using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-290 포장 v3 증거 찍기(QA): 꼬리표 → 집음 → 상자 고름 → 롤 당김 → 물건 넣음 → 테이프 → 흔들기, 7장(1×, 게임 화면 그대로).
/// 그 앞에 작은 상자 시험(물리로 걸리나)도 한 번. 결과 숫자는 JSON(steps).
/// 실행: exec 'return PackV3Shots.Begin("D:/.../packing-v3");' → PackV3Shots.Status 가 "done" 이면 끝.
/// </summary>
public class PackV3Shots : MonoBehaviour
{
    public static string Status = "idle";
    static string dir;
    OnFootDirector d;
    readonly List<string> steps = new List<string>();

    public static string Begin(string outDir)
    {
        Application.runInBackground = true;
        var dd = FindFirstObjectByType<OnFootDirector>();
        if (dd == null) return "no dir";
        if (dd.PackSpot == null || dd.PackSpot.V3Table == null) return "no v3";
        dir = outDir; System.IO.Directory.CreateDirectory(dir); Status = "run";
        new GameObject("PackV3Shots").AddComponent<PackV3Shots>().d = dd;
        return "OK";
    }

    void Step(PackTable2 t, string s) => steps.Add($"{s} | 손={t.Holding} 상자={(t.WorkBox != null ? t.WorkBox.Box.sizeName : "-")} InBox={(t.InBox != null)} 바닥={t.Bottom} 덮기={t.Top} fit={t.Fit} | {t.LastAct}");

    IEnumerator Look(Vector3 at) { d.Player.Cam.LookAt(at); yield return null; yield return null; yield return null; }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, name + ".png"));
        yield return null; yield return null;
    }

    IEnumerator Start()
    {
        float w = 0f;
        while (d.State == OnFootDirector.Mode.Starting && w < 10f) { w += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSecondsRealtime(1f);
        var t = d.PackSpot.V3Table;
        var parcel = t.receive.today[0];
        var pp = parcel.transform.position;

        // 1 꼬리표
        d.DebugTeleport(new Vector3(pp.x, 31.1f, pp.z - 1.0f), 0f, null, 0f); yield return null;
        yield return Look(pp + Vector3.up * 0.08f);
        yield return new WaitForSecondsRealtime(0.6f);
        Step(t, $"꼬리표 보임={(parcel.tag3d != null && parcel.tag3d.activeSelf)} 시선={d.GazeTarget} 칩='{d.Prompt}'");
        yield return Shot("1_tag");

        // 2 집음(시선 + E)
        d.SimInteract(); yield return null; yield return null;
        if (!t.Picked) { Step(t, "시선 E 안 잡힘 → SimUse"); t.SimUse(parcel); }
        yield return new WaitForSecondsRealtime(0.5f);
        Step(t, $"집음 OrderPicked={d.OrderPicked} 꼬리표 보임={(parcel.tag3d != null && parcel.tag3d.activeSelf)}");
        yield return Shot("2_picked");

        // 포장대 앞
        d.DebugTeleport(new Vector3(273.35f, 31.1f, -29.5f), 90f, null, 0f); yield return null;
        yield return Look(t.workSpot.position + Vector3.up * 0.1f);
        t.SimUse(t); yield return null;   // 물건을 판 위에
        Step(t, "물건 판 위");

        // 작은 상자 시험: 물리로 걸리나
        t.SimUse(t.boxes[0]); yield return null; t.SimUse(t); yield return null;
        t.SimUse(parcel); yield return null; t.SimUse(t.WorkBox);
        yield return new WaitForSecondsRealtime(2.2f);
        var bx0 = t.WorkBox.Box;
        Step(t, $"작은 상자 시험: 물건 바닥 y(상자 기준)={bx0.transform.InverseTransformPoint(parcel.transform.position).y - parcel.transform.lossyScale.y * 0.5f:F3} 테두리 y={bx0.RimY:F3}");
        t.SimUse(parcel); yield return null; t.SimUse(t); yield return null;   // 다시 판 위로
        // 3 상자 고름(중) — 손에 든 채
        t.SimUse(t.boxes[1]); yield return new WaitForSecondsRealtime(0.3f);
        Step(t, "상자 중 집음");
        yield return Shot("3_box");
        t.SimUse(t); yield return null;
        yield return Look(t.workSpot.position + Vector3.up * 0.1f);

        // 4 롤 당김
        t.SimUse(t.rolls[0]); yield return new WaitForSecondsRealtime(0.3f);
        Step(t, "뽁뽁이 한 장");
        yield return Shot("4_roll");
        t.SimUse(t); yield return null;   // 바닥에

        // 5 물건 넣음
        t.SimUse(parcel); yield return null; t.SimUse(t.WorkBox);
        yield return new WaitForSecondsRealtime(2.2f);
        Step(t, "물건 넣음");
        yield return Shot("5_in");
        t.SimUse(t.rolls[0]); yield return null; t.SimUse(t); yield return null;   // 덮기

        // 6 닫고 테이프
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(0.6f);
        t.SimTape(); yield return null; yield return null;
        Step(t, $"닫음={t.WorkBox.Box.IsClosed} 테이프={t.WorkBox.Box.TapeCover:F2}");
        yield return Shot("6_tape");

        // 7 들어 흔들기
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(0.3f);
        t.SimShake(2f); yield return new WaitForSecondsRealtime(0.7f);
        yield return Shot("7_shake");
        yield return new WaitForSecondsRealtime(1.6f);
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(0.5f);
        Step(t, $"내려놓음 Ready={t.Ready} spot.Ready={d.PackSpot.Ready} 시작={t.StartIntegrity01 * 100f:F0}% rattle={t.Result.rattle:F2} hits={t.Result.hits}");

        var sb = new System.Text.StringBuilder("{\"steps\":[\n");
        for (int i = 0; i < steps.Count; i++) sb.Append("\"").Append(steps[i].Replace("\"", "'")).Append(i < steps.Count - 1 ? "\",\n" : "\"\n");
        sb.Append("]}\n");
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "steps.json"), sb.ToString());
        Status = "done";
        Destroy(gameObject);
    }
}
