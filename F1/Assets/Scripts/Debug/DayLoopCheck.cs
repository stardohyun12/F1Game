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
    static string outPath;
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

        // 사장
        DebugWindow.GoPlace(2); yield return null;
        yield return Look(d.Boss.position + Vector3.up * 1.5f);
        Step("사장 앞"); yield return Press();
        yield return Wait(() => d.InTalk, 2f);
        while (d.InTalk) { yield return new WaitForSecondsRealtime(0.4f); d.SimInteract(); }
        yield return new WaitForSecondsRealtime(1.5f);
        Step("사장 끝 Talked=" + d.Talked);

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
}
