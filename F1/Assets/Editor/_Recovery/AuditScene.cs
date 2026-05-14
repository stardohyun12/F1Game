using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 종합 진단. 수정 X. 리포트 only.
public static class AuditScene
{
    public static void Execute()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");
        Debug.Log("===== AUDIT START =====");

        AuditNullRefs();
        AuditCarColliderConflicts();
        AuditFloatingTilesV2();
        AuditZFightCandidates();
        AuditTrackConnectivity();
        AuditServices();
        AuditDuplicates();

        Debug.Log("===== AUDIT END =====");
    }

    // 1. MonoBehaviour 필드에 missing reference 있는 컴포넌트
    static void AuditNullRefs()
    {
        Debug.Log("--- [Audit] Missing references ---");
        int missing = 0;
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb == null) { missing++; continue; }
            var so = new SerializedObject(mb);
            var sp = so.GetIterator();
            while (sp.NextVisible(true))
            {
                if (sp.propertyType == SerializedPropertyType.ObjectReference &&
                    sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                {
                    Debug.LogWarning($"[Audit] MISSING ref: '{GetPath(mb.transform)}' ({mb.GetType().Name}).{sp.name}");
                    missing++;
                }
            }
        }
        Debug.Log($"[Audit] Missing refs total: {missing}");
    }

    // 2. 차에 자기 self-collider 충돌 가능성 + groundLayer 호환
    static void AuditCarColliderConflicts()
    {
        Debug.Log("--- [Audit] Car colliders ---");
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogWarning("[Audit] no car"); return; }
        var cols = car.GetComponentsInChildren<Collider>(true);
        Debug.Log($"[Audit] Car has {cols.Length} colliders");
        foreach (var c in cols)
            Debug.Log($"  - '{c.name}' {c.GetType().Name} layer={c.gameObject.layer}({LayerMask.LayerToName(c.gameObject.layer)}) trigger={c.isTrigger}");
        var so = new SerializedObject(car);
        var p = so.FindProperty("groundLayer");
        Debug.Log($"[Audit] groundLayer mask = 0x{p.intValue:X8}");
        if (p.intValue == -1)
            Debug.LogWarning($"[Audit] groundLayer = Everything (~0). 차 자체 layer 포함 — 자기 raycast self-hit 위험 (현재 origin+up 0.5로 회피 중).");
    }

    // 3. 떠 있는 평지 도로 타일 재검사 (더 엄격: 0.05 미만)
    static void AuditFloatingTilesV2()
    {
        Debug.Log("--- [Audit] Floating tiles ---");
        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogWarning("[Audit] no Track"); return; }
        var renderers = track.GetComponentsInChildren<MeshRenderer>(false);
        var ys = renderers.Select(r => r.bounds.max.y).OrderBy(y => y).ToList();
        float median = ys[ys.Count / 2];
        var floaters = renderers.Where(r =>
            r.bounds.max.y > median + 0.2f && r.bounds.max.y < median + 1.0f).ToList(); // slope 제외
        Debug.Log($"[Audit] Tiles={renderers.Length}, median.top={median:F3}, mid-elevation suspects (median+0.2~+1.0)={floaters.Count}");
        foreach (var r in floaters.Take(10))
            Debug.Log($"  - {r.name}: top.y={r.bounds.max.y:F3}");
    }

    // 4. z-fighting — 큰 평면이 거의 같은 y에 있는 후보
    static void AuditZFightCandidates()
    {
        Debug.Log("--- [Audit] Z-fight candidates ---");
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var bigFlat = renderers.Where(r =>
        {
            var b = r.bounds;
            return b.size.x > 5f && b.size.z > 5f && b.size.y < 0.5f;
        }).Select(r => new { r, topY = r.bounds.max.y }).ToList();
        // 0.02 이내로 겹치는 페어
        int pairs = 0;
        for (int i = 0; i < bigFlat.Count; i++)
        for (int j = i + 1; j < bigFlat.Count; j++)
        {
            if (Mathf.Abs(bigFlat[i].topY - bigFlat[j].topY) < 0.02f)
            {
                if (bigFlat[i].r.bounds.Intersects(bigFlat[j].r.bounds))
                {
                    pairs++;
                    if (pairs <= 5)
                        Debug.LogWarning($"[Audit] z-fight: '{bigFlat[i].r.name}' (y={bigFlat[i].topY:F3}) ↔ '{bigFlat[j].r.name}' (y={bigFlat[j].topY:F3})");
                }
            }
        }
        Debug.Log($"[Audit] z-fight candidate pairs: {pairs}");
    }

    // 5. 도로 연결성 — End cells
    static void AuditTrackConnectivity()
    {
        Debug.Log("--- [Audit] Track connectivity ---");
        var track = GameObject.Find("Track");
        if (track == null) return;
        int ends = 0, straights = 0, curves = 0, intersections = 0, crossroads = 0, slopes = 0;
        foreach (Transform t in track.transform)
        {
            string n = t.name;
            if (n.Contains("End") || n.Contains("Driveway")) ends++;
            else if (n.Contains("Straight") || n.Contains("Crossing") || n.Contains("Bridge")) straights++;
            else if (n.Contains("Curve")) curves++;
            else if (n.Contains("Intersection")) intersections++;
            else if (n.Contains("Crossroad")) crossroads++;
            else if (n.Contains("Slant")) slopes++;
        }
        Debug.Log($"[Audit] Tiles: straight={straights}, curve={curves}, intersection={intersections}, crossroad={crossroads}, end={ends}, slope={slopes}");
    }

    // 6. Services 등록 상태 — GameContext가 다 채워주는지
    static void AuditServices()
    {
        Debug.Log("--- [Audit] Services (compile-only check) ---");
        // 런타임 Awake 안 도니까 GameContext 컴포넌트 존재만 확인.
        var ctx = GameObject.Find("GameContext");
        Debug.Log($"[Audit] GameContext GameObject present: {ctx != null}");
    }

    // 7. 같은 GameObject 이름 중복
    static void AuditDuplicates()
    {
        Debug.Log("--- [Audit] Duplicate GameObjects (top-level) ---");
        var rootObjs = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects();
        var nameCounts = new Dictionary<string, int>();
        foreach (var go in rootObjs)
        {
            nameCounts.TryGetValue(go.name, out int n);
            nameCounts[go.name] = n + 1;
        }
        foreach (var kv in nameCounts)
            if (kv.Value > 1)
                Debug.LogWarning($"[Audit] Duplicate root: '{kv.Key}' x{kv.Value}");
    }

    static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
