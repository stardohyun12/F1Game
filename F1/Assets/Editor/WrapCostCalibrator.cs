using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitCrew.Packing;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PitCrew/Calibrate Wrap Cost — 화물 종류마다 WrapPainter 로 '사람처럼 다 감기'를 자동 실행해
/// 다 감기 픽셀 면적을 실측하고 Resources/WrapCostCalibration.asset 에 저장한다.
/// 그러면 '다 감으면 보상 × 재료 costPerPixel'(버블 0.15 / 폼 0.25 / 하드 0.35) 이 된다.
/// 스트로크 패턴 = QA 1536 wrapall.cs: 6방향으로 돌려 화면 세로 띠 0.2m 간격, 기본 랩 폭 24cm(겹침 포함).
/// Play 중에만 동작(PackingScreen.Show 가 런타임 서비스를 씀). 저장한 뒤 전 주문 × 전 재료 검증 표를 콘솔에 찍는다.
/// </summary>
public static class WrapCostCalibrator
{
    const string AssetPath = "Assets/Resources/" + WrapCostCalibration.ResourcePath + ".asset";
    const float StripSpacing = 0.2f;     // 띠 간격(m) — 사람이 살짝 겹쳐 감는 정도
    const float DefaultWidthUV = 0.06f;  // WrapPainter.stripWidthUV 기본(리본 24cm)

    static readonly Vector3[] Euler =
    {
        new Vector3(0, 180, 0), new Vector3(0, 90, 0), new Vector3(0, 0, 0),
        new Vector3(0, 270, 0), new Vector3(90, 180, 0), new Vector3(-90, 180, 0),
    };

    [MenuItem("PitCrew/Calibrate Wrap Cost")]
    static void Menu()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Calibrate Wrap Cost", "Play 모드에서 실행하세요 (포장 화면이 런타임에서만 만들어짐).", "확인");
            return;
        }
        Debug.Log(Run());
    }

    /// <summary>보정 + 검증. 결과 표(텍스트)를 돌려준다.</summary>
    public static string Run()
    {
        var ps = Object.FindFirstObjectByType<PackingScreen>(FindObjectsInactive.Include);
        if (ps == null) return "[WrapCost] PackingScreen 없음";
        var mats = Resources.LoadAll<WrapMaterialDef>("WrapMaterials").OrderBy(m => m.channelIndex).ToArray();
        if (mats.Length == 0) return "[WrapCost] 재료 없음";
        var jobs = AssetDatabase.FindAssets("t:OrderJob")
            .Select(g => AssetDatabase.LoadAssetAtPath<OrderJob>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(j => j != null).OrderBy(j => j.name).ToList();

        var calib = AssetDatabase.LoadAssetAtPath<WrapCostCalibration>(AssetPath);
        if (calib == null)
        {
            calib = ScriptableObject.CreateInstance<WrapCostCalibration>();
            AssetDatabase.CreateAsset(calib, AssetPath);
        }

        var sb = new StringBuilder("[WrapCost] 보정 (재료와 무관한 다 감기 픽셀 면적)\n");
        // 1) 종류별 실측 — 단가 배율 1 로 감고 픽셀 면적 = 비용 ÷ costPerPixel.
        foreach (var type in jobs.Select(j => j.cargoType).Distinct())
        {
            var job = jobs.First(j => j.cargoType == type);
            var ws = WrapFully(ps, job, mats[0], scale: 1f, out int strokes);
            if (ws == null) { sb.Append($"  {type}: 실패\n"); continue; }
            float pixels = ws.TotalCost / mats[0].costPerPixel;
            calib.Set(type, pixels, strokes);
            sb.Append($"  {type} ({job.name}): pixels={pixels:F0} strokes={strokes}\n");
        }
        EditorUtility.SetDirty(calib);
        AssetDatabase.SaveAssets();

        // 2) 검증 — 보정된 배율로 전 주문 × 전 재료를 다시 감아 보상 대비 % 를 잰다.
        sb.Append("[WrapCost] 검증 (다 감기 비용 ÷ 기본 보상, 목표 = costPerPixel)\n");
        float worst = 0f;
        foreach (var job in jobs)
        {
            sb.Append($"  {job.name} ({job.cargoType}, ₩{job.baseReward}):");
            foreach (var m in mats)
            {
                var ws = WrapFully(ps, job, m, scale: -1f, out _);
                if (ws == null) { sb.Append($" {m.name}=실패"); continue; }
                float pct = ws.TotalCost / job.baseReward * 100f;
                worst = Mathf.Max(worst, Mathf.Abs(pct - m.costPerPixel * 100f));
                sb.Append($" {m.name} {pct:F1}%(₩{ws.TotalCost:F0})");
            }
            sb.Append('\n');
        }
        sb.Append($"[WrapCost] 최대 오차 {worst:F2}%p (목표 ±3%p)\n");

        // 포장 화면을 현재 판 주문으로 되돌린다.
        var run = Services.Run as RunController;
        if (run != null && run.CurrentJob != null) ps.Show(run.CurrentJob);
        return sb.ToString();
    }

    /// <summary>job 을 띄우고 재료 m 으로 다 감는다. scale ≥ 0 이면 단가 배율을 그 값으로 덮어쓴다. 상한은 끈다.</summary>
    static WrapState WrapFully(PackingScreen ps, OrderJob job, WrapMaterialDef m, float scale, out int strokes)
    {
        strokes = 0;
        ps.Show(job);
        var item = ps.ActiveItem;
        var cam = ps.PackingCamera;
        if (item == null || cam == null) return null;
        var painter = item.GetComponent<WrapPainter>();
        var ws = item.GetComponent<WrapState>();
        var col = painter != null ? painter.TargetCollider : null;
        if (ws == null || col == null) return null;

        float cap = ws.CostCap;
        ws.CostCap = float.PositiveInfinity;
        if (scale >= 0f) ws.CostScale = scale;
        painter.StripWidthUV = DefaultWidthUV;
        painter.SetMaterial(m);

        Vector3 R = cam.transform.right, U = cam.transform.up;
        Vector3 absR = new Vector3(Mathf.Abs(R.x), Mathf.Abs(R.y), Mathf.Abs(R.z));
        Vector3 absU = new Vector3(Mathf.Abs(U.x), Mathf.Abs(U.y), Mathf.Abs(U.z));
        foreach (var e in Euler)
        {
            item.transform.rotation = Quaternion.Euler(e);
            Physics.SyncTransforms();
            var b = col.bounds;
            float halfW = Vector3.Dot(b.extents, absR), halfH = Vector3.Dot(b.extents, absU);
            int cols = Mathf.Max(1, Mathf.CeilToInt(2f * halfW / StripSpacing));
            for (int s = 0; s < cols; s++)
            {
                float x = -halfW + 2f * halfW * (s + 0.5f) / cols;
                Vector3 top = b.center + R * x + U * halfH * 1.05f, bot = b.center + R * x - U * halfH * 1.05f;
                // 띠 위에서 물체에 처음 닿는 점에서 시작해 위·아래로 한 번씩 끈다.
                Vector2 st = Vector2.zero; bool hit = false;
                for (int k = 0; k <= 20 && !hit; k++)
                {
                    Vector2 sp = cam.WorldToScreenPoint(Vector3.Lerp(top, bot, k / 20f));
                    if (col.Raycast(cam.ScreenPointToRay(sp), out _, 100f)) { st = sp; hit = true; }
                }
                if (!hit) continue;
                Vector2 ts = cam.WorldToScreenPoint(top), bs = cam.WorldToScreenPoint(bot);
                painter.BeginStroke(st); for (int i = 1; i <= 16; i++) painter.UpdateStroke(Vector2.Lerp(st, ts, i / 16f)); painter.EndStroke();
                painter.BeginStroke(st); for (int i = 1; i <= 16; i++) painter.UpdateStroke(Vector2.Lerp(st, bs, i / 16f)); painter.EndStroke();
                strokes++;
            }
        }
        item.transform.rotation = PackingScreen.FrontFacingRotation;
        Physics.SyncTransforms();
        ws.CostCap = cap;
        return ws;
    }
}
