using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-287 분리 전·후 비교용: 하루 한 바퀴(사장 → 쪽지 → 작업대 → 실음 → 도착 → 건넴 → 노을 → 침대 → 일기 → 아침)를
/// 순간이동 + Sim 입력으로 돌리고 감독 로그를 JSON 으로 남긴다. 걷기·운전은 하지 않는다(시각이 흔들리지 않게).
/// 포장 손동작은 따로 크다 — 작업대 들어가기·물러나기만 Sim 으로, 상자 들기는 감독의 TakePacked 를 바로 부른다.
/// 실행: exec 'return DayLoopCheck.Begin("D:/.../before.json");' → DayLoopCheck.Status 가 "done …" 이면 끝.
/// </summary>
public class DayLoopCheck : MonoBehaviour
{
    public static string Status = "idle";
    /// <summary>D-293 걷기 검사 결과(집은 뒤 걸어도 물건-카메라 거리가 안 변하나). 한 줄 JSON-ish.</summary>
    public static string WalkResult = "";
    public const float WalkHoldTol = 0.05f;
    static string outPath;
    bool walkOnly;
    OnFootDirector d;
    float t0;
    readonly List<string> steps = new List<string>();

    public static string Begin(string path)
    {
        var dir = FindFirstObjectByType<OnFootDirector>();
        if (dir == null) return "no dir";
        outPath = path; Status = "run";
        var go = new GameObject("DayLoopCheck");
        go.AddComponent<DayLoopCheck>().d = dir;
        return "OK";
    }

    /// <summary>D-293: 선반 앞에서 물건만 집고 걷기 검사 한 번(포장 손동작 없이). exec 'return DayLoopCheck.BeginWalk();' → WalkResult.</summary>
    public static string BeginWalk()
    {
        var dir = FindFirstObjectByType<OnFootDirector>();
        if (dir == null) return "no dir";
        outPath = null; Status = "run"; WalkResult = "";
        var go = new GameObject("DayLoopCheck");
        var c = go.AddComponent<DayLoopCheck>(); c.d = dir; c.walkOnly = true;
        return "OK";
    }

    float T => Time.unscaledTime;
    void Step(string s) { steps.Add($"[{T - t0:F1}] {s} | 상태={d.State} 단계={d.DeliveryStep} 안내='{d.Prompt}' 지나온={d.StepTrail}"); }

    IEnumerator Wait(System.Func<bool> ok, float sec)
    {
        float w = 0f;
        while (!ok() && w < sec) { w += Time.unscaledDeltaTime; yield return null; }
    }

    IEnumerator Look(Vector3 at)
    {
        d.Player.Cam.LookAt(at);
        yield return null; yield return null; yield return null;
    }

    void Call(string method) =>
        typeof(OnFootDirector).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(d, null);

    IEnumerator Press()
    {
        d.SimInteract(); yield return null; yield return null;
    }

    IEnumerator Start()
    {
        yield return Wait(() => d.State != OnFootDirector.Mode.Starting, 10f);
        yield return new WaitForSecondsRealtime(1f);
        t0 = T;
        Step("시작");

        if (walkOnly)
        {
            var t2 = d.PackSpot != null ? d.PackSpot.V3Table : null;
            if (t2 == null) { WalkResult = "FAIL no V3Table"; Status = "done"; Destroy(gameObject); yield break; }
            yield return PickFromShelf(t2);
            yield return WalkWhileHolding(t2);
            Status = "done"; Destroy(gameObject); yield break;
        }

        // 사장
        DebugWindow.GoPlace(2); yield return null;
        yield return Look(d.Boss.position + Vector3.up * 1.5f);
        Step("사장 앞"); yield return Press();
        yield return Wait(() => d.InTalk, 2f);
        while (d.InTalk) { yield return new WaitForSecondsRealtime(0.4f); d.SimInteract(); }
        yield return new WaitForSecondsRealtime(1.5f);
        Step("사장 끝 Talked=" + d.Talked);

        if (d.PackSpot != null && d.PackSpot.V3Table != null) yield return PackV3(d.PackSpot.V3Table);
        else yield return OldPack();

        // 싣기
        DebugWindow.GoPlace(3); yield return null;
        yield return Look(d.Car.transform.position);
        Step("카트 옆"); yield return Press();
        yield return Wait(() => d.State == OnFootDirector.Mode.InCart, 2f);
        yield return new WaitForSecondsRealtime(1f);
        Step("카트 탐");

        // 도착 → 건넴
        d.DebugArriveForHandover(1f, new Vector3(97.5f, 4.4f, -281.0f), 270f, new Vector3(94.0f, 4.0f, -283.65f), 180f);
        yield return new WaitForSecondsRealtime(1f);
        Step("도착 Carrying=" + d.CarryingBox);
        yield return Look(d.Handover.DoorCenter);
        yield return Wait(() => d.Prompt == "E 두드리기", 1f);
        Step("문 앞"); yield return Press();
        yield return Wait(() => d.InTalk, 4f);
        while (d.InTalk) { yield return new WaitForSecondsRealtime(0.6f); d.SimInteract(); }
        yield return new WaitForSecondsRealtime(2f);
        Step("건넴 HandedOver=" + d.HandedOver);

        // 침대 → 일기 → 아침
        d.DebugTeleport(new Vector3(187.3f, 28.5f, -62.6f), 270f, null, 0f); yield return null;
        yield return Look(d.Bed.Anchor);
        Step("침대 앞"); yield return Press();
        yield return Wait(() => d.State == OnFootDirector.Mode.Diary && d.DiaryCanClose, 4f);
        yield return new WaitForSecondsRealtime(1f);
        Step("일기");
        d.SimDiaryClose();
        yield return Wait(() => d.State == OnFootDirector.Mode.OnFoot, 6f);
        yield return new WaitForSecondsRealtime(1f);
        Step("아침");

        var sb = new System.Text.StringBuilder("{\"steps\":[\n");
        for (int i = 0; i < steps.Count; i++) sb.Append("\"").Append(steps[i].Replace("\"", "'")).Append(i < steps.Count - 1 ? "\",\n" : "\"\n");
        sb.Append("],\n\"log\":[\n");
        for (int i = 0; i < d.Log.Count; i++) sb.Append("\"").Append(d.Log[i].Replace("\"", "'")).Append(i < d.Log.Count - 1 ? "\",\n" : "\"\n");
        sb.Append("]}\n");
        System.IO.File.WriteAllText(outPath, sb.ToString());
        Status = "done";
        Destroy(gameObject);
    }

    /// <summary>D-290 전: 칠판 쪽지 → 옛 작업대(PackBench 봇) → 상자 들기.</summary>
    IEnumerator OldPack()
    {
        // 쪽지
        DebugWindow.GoPlace(12); yield return null;
        yield return Look(d.OrderBoard.Center);
        yield return Wait(() => d.Prompt == "E 쪽지 집기", 1f);
        Step("칠판 앞"); yield return Press();
        yield return new WaitForSecondsRealtime(2f);
        Step("쪽지 OrderPicked=" + d.OrderPicked);

        // 작업대
        yield return new WaitForSecondsRealtime(1.5f);   // 쪽지 뒤 작업대 쪽 시선(0.5+0.8 s)이 끝나길
        d.DebugTeleport(new Vector3(273.2f, 31.1f, -29.5f), 90f, null, 0f); yield return null;
        yield return Look(d.PackSpot.Top);
        yield return Wait(() => d.Prompt.StartsWith("E "), 1f);
        Step("작업대 앞");
        Call("EnterPack");   // 시선 레이가 작업대 판을 못 잡을 때가 있다 — E 대신 감독의 작업대 들어가기를 바로
        yield return Wait(() => d.State == OnFootDirector.Mode.Pack && !d.PackMoving, 4f);
        Step("작업대 안");
        PitCrew.PackBench.PackBench.BotRun("full");
        yield return Wait(() => PitCrew.PackBench.PackBench.BotStatus.Contains(" done "), 40f);
        yield return new WaitForSecondsRealtime(1f);
        Step("포장 봇 끝 Ready=" + d.PackSpot.Ready);
        d.SimPackLeave();
        yield return Wait(() => d.State == OnFootDirector.Mode.OnFoot && !d.PackMoving, 6f);
        Step("작업대 물러남");
        Call("TakePacked");
        yield return new WaitForSecondsRealtime(2f);
        Step("상자 들기 Carrying=" + d.CarryingBox);
    }

    /// <summary>
    /// D-290 포장 v3: 받는 곳 선반에서 물건 E(시선) → 포장대 앞 → 사람과 같은 Use 순서(작업면 · 상자 중 · 뽁뽁이 바닥 · 물건 · 뽁뽁이 덮기 · 닫기 · 테이프 · 들어 흔들기 · 내려놓기 · 상자 들기).
    /// 첫 E 는 시선 레이로(안 맞으면 SimUse), 포장대 위 물건들은 SimUse 로 바로 — 시선 각도 흔들림 없이.
    /// </summary>
    IEnumerator PackV3(PackTable2 t)
    {
        yield return PickFromShelf(t);
        yield return WalkWhileHolding(t);
        var parcel = t.receive.today[0];
        d.DebugTeleport(new Vector3(273.2f, 31.1f, -29.5f), 90f, null, 0f); yield return null;
        yield return Look(t.workSpot.position);
        Step("포장대 앞");
        t.SimUse(t); yield return null;                                   // 물건을 작업면에
        t.SimUse(t.boxes[1]); yield return null; t.SimUse(t); yield return null;   // 상자 중
        t.SimUse(t.rolls[0]); yield return null; t.SimUse(t); yield return null;   // 뽁뽁이 바닥
        t.SimUse(parcel); yield return null; t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(2f);   // 물건 넣기(떨어짐)
        t.SimUse(t.rolls[0]); yield return null; t.SimUse(t); yield return null;   // 뽁뽁이 덮기
        Step($"넣음 InBox={(t.InBox != null)} 바닥={t.Bottom} 덮기={t.Top} fit={t.Fit}");
        t.SimUse(t.WorkBox); yield return null;                            // 닫기
        t.SimTape(); yield return null; yield return null;
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(0.3f);   // 들기
        t.SimShake(2f); yield return new WaitForSecondsRealtime(2.5f);
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(0.5f);   // 내려놓기
        Step($"포장 v3 끝 Ready={d.PackSpot.Ready} 시작={t.StartIntegrity01 * 100f:F0}% rattle={t.Result.rattle:F2} | {t.LastAct}");
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(2f);   // 상자 들기(BoxTaken)
        Step("상자 들기 Carrying=" + d.CarryingBox);
    }

    /// <summary>받는 곳 선반 앞에서 시선 E 로 물건을 집는다(안 잡히면 SimUse).</summary>
    IEnumerator PickFromShelf(PackTable2 t)
    {
        var parcel = t.receive.today[0];
        var pp = parcel.transform.position;
        d.DebugTeleport(new Vector3(pp.x, 31.1f, pp.z - 1.1f), 0f, null, 0f); yield return null;
        yield return Look(pp);
        Step("받는 곳 선반 앞 시선=" + d.GazeTarget);
        yield return Press();
        if (!t.Picked) { Step("시선 E 안 잡힘 → SimUse"); t.SimUse(parcel); yield return null; }
        yield return new WaitForSecondsRealtime(0.5f);
        Step("물건 집음 OrderPicked=" + d.OrderPicked + " 손=" + t.Holding);
    }

    /// <summary>
    /// D-293: 집은 뒤 순간이동 없이 Sim 입력 W 2초로 걷는다. 든 물건-카메라 거리가 걷기 전과 0.05 m 이상 다르면 실패(WalkResult "FAIL …").
    /// 들고 있지 않거나 실제로 1 m 도 못 걸었어도 실패 — 검사가 빈 채로 통과하지 못하게.
    /// </summary>
    IEnumerator WalkWhileHolding(PackTable2 t)
    {
        var held = PackTable2.HeldNow;
        var cam = Camera.main;
        if (held == null || cam == null || t.Holding != PackTable2.Hold.Parcel) { WalkResult = $"FAIL 손에 안 듦 held={(held != null)} cam={(cam != null)} Holding={t.Holding}"; Step(WalkResult); yield break; }
        var pl = d.Player;
        d.Player.Cam.AddLook(180f - d.Player.Cam.Yaw, 0f);   // 선반을 등지고(뒤는 열린 보관소 바닥)
        yield return null; yield return null;
        float d0 = Vector3.Distance(held.position, cam.transform.position);
        Vector3 p0 = pl.transform.position, rel0 = cam.transform.InverseTransformPoint(held.position);
        bool oldSim = pl.simActive, oldCamSim = pl.Cam.simActive;
        pl.simActive = true; pl.Cam.simActive = true; pl.simMove = new Vector2(0f, 1f);
        float maxDev = 0f; float w = 0f;
        while (w < 2f)
        {
            w += Time.unscaledDeltaTime; yield return new WaitForEndOfFrame();
            float dev = Vector3.Distance(cam.transform.InverseTransformPoint(held.position), rel0);   // 카메라 기준 자리 변화(거리+방향)
            if (dev > maxDev) maxDev = dev;
        }
        pl.simMove = Vector2.zero; pl.simActive = oldSim; pl.Cam.simActive = oldCamSim;
        float d1 = Vector3.Distance(held.position, cam.transform.position);
        float walked = Vector3.Distance(pl.transform.position, p0);
        bool ok = held == PackTable2.HeldNow && Mathf.Abs(d1 - d0) < WalkHoldTol && maxDev < WalkHoldTol && walked > 1f;
        WalkResult = $"{(ok ? "PASS" : "FAIL")} 걸음={walked:F2}m 거리 {d0:F3}→{d1:F3} (Δ{Mathf.Abs(d1 - d0):F3}) 카메라기준 최대이탈={maxDev:F3} 부모={(held.parent != null ? held.parent.name : "null")} 부모의부모={(held.parent != null && held.parent.parent != null ? held.parent.parent.name : "null")} 카메라={cam.name}";
        Step("걷기 검사 " + WalkResult);
    }
}
