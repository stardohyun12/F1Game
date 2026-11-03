using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PitCrew.PackBench;
using UnityEngine;

/// <summary>
/// D-283 첫 한 판 확인용 짧은 손 입력 시뮬(봇 전체 경로 아님): 집 앞 → 걸어서 보관소 → 쪽지 → 작업대 포장 → 상자 들기 → 카트 「E 싣고 타기」.
/// 칸 사이 순간이동 없음 — 걸음이 막히면 기록하고 멈춘다. 입력은 OnFootPlayer.simMove · OnFootCamera.Yaw · OnFootDirector.SimInteract · PackBench.BotRun 뿐.
/// </summary>
public class MemoSim : MonoBehaviour
{
    public static string Status = "idle";
    public static string GrabDir = "";
    OnFootDirector dir;
    string logPath;
    readonly StringBuilder log = new StringBuilder();
    readonly List<string> stuck = new List<string>();

    public static string Begin(string logFile, bool fromHouseFront)
    {
        var d = FindFirstObjectByType<OnFootDirector>();
        if (d == null) return "FAIL: OnFootDirector 없음";
        var old = FindFirstObjectByType<MemoSim>();
        if (old != null) Destroy(old.gameObject);
        var s = new GameObject("_MemoSim").AddComponent<MemoSim>();
        s.dir = d; s.logPath = logFile; s.fromHouseFront = fromHouseFront;
        Status = "starting";
        return "OK";
    }

    bool fromHouseFront;
    void Line(string s) { log.AppendLine(s); File.WriteAllText(logPath, log.ToString(), new UTF8Encoding(false)); }
    static string P(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:F1},{1:F1},{2:F1})", v.x, v.y, v.z);
    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    void Grab(string prefix) { if (!string.IsNullOrEmpty(GrabDir)) PresentGrab.Begin(GrabDir, prefix, 1, 0f, 0.1f); }
    bool Fail(string why) { stuck.Add(why); Line("막힘: " + why); Status = "FAIL " + why; return false; }

    // 집 앞 인도 → 큰길 → 보관소 앞 길 → 앞마당 경사로 → 셔터 → 칠판 앞(OnFootRouteBot 의 보관소 걸음과 같은 점들).
    static readonly Vector2[] ToDepot =
    {
        // 큰길 중심선(RoadSurface 최단길) 점들 — 보관소 앞은 헤어핀(273.8,-44.1 에서 되돌아 267.9,-40.5)이라 점을 촘촘히.
        new Vector2(197.3f, -77.9f), new Vector2(205.0f, -82.1f), new Vector2(210.8f, -81.0f), new Vector2(223.5f, -78.7f), new Vector2(235.6f, -74.1f),
        new Vector2(243.0f, -69.1f), new Vector2(245.2f, -67.3f), new Vector2(245.2f, -63.1f), new Vector2(246.5f, -57.3f), new Vector2(246.4f, -56.3f),
        new Vector2(252.6f, -52.5f), new Vector2(256.0f, -49.6f), new Vector2(260.3f, -46.8f), new Vector2(263.6f, -46.5f), new Vector2(268.7f, -45.3f),
        new Vector2(273.5f, -44.4f), new Vector2(273.8f, -44.1f), new Vector2(272.2f, -43.1f), new Vector2(267.9f, -40.5f), new Vector2(267.8f, -39.5f),
        new Vector2(270.3f, -38.1f), new Vector2(273.1f, -37.1f), new Vector2(277.0f, -37.0f),
        new Vector2(277.0f, -33.6f), new Vector2(272.5f, -33.6f), new Vector2(270.3f, -32.6f), new Vector2(269.6f, -31.6f), new Vector2(268.9f, -29.4f),
    };
    static readonly Vector2[] OutOfDepot =
    {
        new Vector2(271.9f, -30.4f), new Vector2(269.8f, -30.5f), new Vector2(269.6f, -31.6f), new Vector2(270.3f, -32.6f),
        new Vector2(272.5f, -33.6f), new Vector2(277.0f, -33.6f), new Vector2(277.0f, -37.0f),
    };

    IEnumerator Start()
    {
        float wait = 0f;
        while (dir.State == OnFootDirector.Mode.Starting && wait < 10f) { wait += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSeconds(0.5f);
        var pl = dir.Player; var cam = pl.Cam;
        float t0 = Time.time;
        Line($"카트 처음 자리={P(dir.Car.transform.position)}");
        if (fromHouseFront)
        {
            DebugWindow.GoPlace(1);
            yield return null; yield return null;
        }
        else DebugWindow.GoPlace(11);
        pl.simActive = true; cam.simActive = true; pl.simMove = Vector2.zero;
        Line($"시작 {P(pl.transform.position)}");
        if (!fromHouseFront) { yield return Walks(new[] { new Vector2(277.0f, -37.0f), new Vector2(277.0f, -33.6f), new Vector2(272.5f, -33.6f), new Vector2(270.3f, -32.6f), new Vector2(269.6f, -31.6f), new Vector2(268.9f, -29.4f) }); if (Status.StartsWith("FAIL")) { Done(t0); yield break; } }
        else { yield return Walks(ToDepot); if (Status.StartsWith("FAIL")) { Done(t0); yield break; } }
        Line($"칠판 앞 도착 t={Time.time - t0:F0}s {P(pl.transform.position)} (순간이동 없이 걸어옴)");

        // 쪽지
        var board = dir.OrderBoard;
        var note = board.NearestNote(board.Center);
        if (note == null) { Fail("쪽지 없음"); Done(t0); yield break; }
        cam.LookAt(note.transform.position); yield return null; yield return null;
        yield return WaitPrompt(p => p == "E 쪽지 집기", 2f);
        if (dir.Prompt != "E 쪽지 집기") { Fail($"쪽지 안내 '{dir.Prompt}'"); Done(t0); yield break; }
        dir.SimInteract(); yield return new WaitForSecondsRealtime(0.6f);
        Line($"쪽지 집음: 맡음={dir.OrderPicked}");
        if (!dir.OrderPicked) { Fail("쪽지 집기 안 됨"); Done(t0); yield break; }
        yield return new WaitForSecondsRealtime(1.2f);

        // 작업대
        yield return Walks(new[] { new Vector2(273.4f, -29.5f) }); if (Status.StartsWith("FAIL")) { Done(t0); yield break; }
        var ps = dir.PackSpot;
        cam.LookAt(ps.Top - Vector3.up * 0.15f); yield return null; yield return null;
        yield return WaitPrompt(p => p.StartsWith("E 포장"), 2f);
        Line($"작업대 앞 안내='{dir.Prompt}'");
        if (!dir.Prompt.StartsWith("E 포장")) { Fail($"작업대 안내 '{dir.Prompt}'"); Done(t0); yield break; }
        dir.SimInteract();
        float w = 0f;
        while ((dir.State != OnFootDirector.Mode.Pack || dir.PackMoving) && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }
        yield return null;
        var bench = ps.Bench;
        StartCoroutine(ChipLog(bench));
        yield return new WaitForSecondsRealtime(0.6f);
        Grab("1-workbench-first-chip");
        yield return new WaitForSecondsRealtime(0.8f);
        PackBench.BotRun("full");
        w = 0f;
        while (!PackBench.BotStatus.Contains(" done ") && w < 60f) { w += Time.deltaTime; yield return null; }
        Line($"작업대 봇: {PackBench.BotStatus} 칩 지금='{bench.Prompt}'");
        if (!ps.Ready) { Fail("흔들어 본 상자 없음"); Done(t0); yield break; }
        dir.SimPackLeave();
        w = 0f;
        while (dir.State != OnFootDirector.Mode.OnFoot && w < 5f) { w += Time.unscaledDeltaTime; yield return null; }
        yield return null;
        cam.LookAt(ps.Top - Vector3.up * 0.1f); yield return null; yield return null;
        yield return WaitPrompt(p => p == "E 상자 들기", 2f);
        Line($"물러난 뒤 안내='{dir.Prompt}'");
        if (dir.Prompt != "E 상자 들기") { Fail($"상자 들기 안내 '{dir.Prompt}'"); Done(t0); yield break; }
        dir.SimInteract(); yield return null; yield return null;
        Line($"상자 들기: 들기={dir.CarryingBox} Packed={dir.Packed} 카트 느낌표={dir.CartMark}");
        if (!dir.CarryingBox) { Fail("상자 안 들림"); Done(t0); yield break; }

        // 카트로
        yield return Walks(OutOfDepot); if (Status.StartsWith("FAIL")) { Done(t0); yield break; }
        Vector3 cp = dir.Car.transform.position, side = dir.Car.transform.right;
        Vector3 beside = cp - side * 1.7f;
        if (Flat(beside - pl.transform.position).magnitude > Flat(cp + side * 1.7f - pl.transform.position).magnitude) beside = cp + side * 1.7f;
        yield return Walks(new[] { new Vector2(beside.x, beside.z) }); if (Status.StartsWith("FAIL")) { Done(t0); yield break; }
        // 카트를 안 보고(70° 돌린 채) 서 있어도 상자를 들고 2 m 안이면 칩이 떠야 한다.
        cam.Yaw += 70f; yield return null; yield return null; yield return null;
        Line($"카트 옆 {Flat(cp - pl.transform.position).magnitude:F1}m, 시선 70° 돌림: 안내='{dir.Prompt}' 시선={dir.GazeTarget ?? "-"}");
        cam.LookAt(CarAim());
        yield return WaitPrompt(p => p == "E 싣고 타기", 2f);
        Line($"카트 앞(바라봄): 안내='{dir.Prompt}' 시선={dir.GazeTarget ?? "-"} 카트 느낌표={dir.CartMark}");
        Grab("2-cart-chip");
        yield return new WaitForSecondsRealtime(0.8f);
        if (dir.Prompt != "E 싣고 타기") { Fail($"싣고 타기 안내 '{dir.Prompt}'"); Done(t0); yield break; }
        dir.SimInteract();
        yield return new WaitForSeconds(1.0f);
        Line($"싣고 탄 뒤: 상태={dir.State} 주문시작={dir.OrderStarted} 들기={dir.CarryingBox}");
        if (dir.State != OnFootDirector.Mode.InCart || !dir.OrderStarted) { Fail("싣고 타기 뒤 상태"); Done(t0); yield break; }
        Status = "memo ok";
        Done(t0);
    }

    void Done(float t0)
    {
        dir.Player.simMove = Vector2.zero;
        Line($"총 {Time.time - t0:F0}s 막힘 {stuck.Count}곳: {string.Join(", ", stuck)} 결과={Status}");
        if (!Status.StartsWith("FAIL") && Status != "memo ok") Status = "done";
    }

    IEnumerator ChipLog(PackBench bench)
    {
        string last = null; float t0 = Time.time;
        while (dir.State == OnFootDirector.Mode.Pack)
        {
            if (bench.Prompt != last) { last = bench.Prompt; Line($"  작업대 칩 +{Time.time - t0:F1}s [{bench.CurrentStep}] '{last}'"); }
            yield return null;
        }
    }

    IEnumerator WaitPrompt(System.Func<string, bool> ok, float max)
    {
        float w = 0f;
        while (!ok(dir.Prompt) && w < max) { w += Time.deltaTime; yield return null; }
    }

    IEnumerator Walks(Vector2[] pts)
    {
        foreach (var q in pts)
        {
            yield return Walk(q);
            if (Status.StartsWith("FAIL")) yield break;
        }
    }

    // 걷기: 막히면(3초 동안 0.3 m 도 못 줄임) 순간이동하지 않고 멈춘다.
    IEnumerator Walk(Vector2 target)
    {
        var pl = dir.Player; var cam = pl.Cam;
        cam.Pitch = 0f;
        float best = float.MaxValue, lastImprove = Time.time, t0 = Time.time;
        while (true)
        {
            Vector3 p = pl.transform.position;
            Vector2 d = target - new Vector2(p.x, p.z);
            float dist = d.magnitude;
            if (dist < 0.3f) break;
            if (dist < best - 0.3f) { best = dist; lastImprove = Time.time; }
            if (Time.time - lastImprove > 3f || Time.time - t0 > 40f)
            {
                pl.simMove = Vector2.zero;
                Fail($"걸음 {target} 남은 {dist:F2}m 사람={P(p)}");
                yield break;
            }
            float want = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            cam.Yaw = Mathf.MoveTowardsAngle(cam.Yaw, want, 360f * Time.deltaTime);
            float off = Mathf.Abs(Mathf.DeltaAngle(cam.Yaw, want));
            pl.simMove = new Vector2(0f, off > 60f ? 0.2f : (dist < 0.8f ? 0.6f : 1f));
            yield return null;
        }
        pl.simMove = Vector2.zero;
    }

    Vector3 CarAim()
    {
        var b = new Bounds(dir.Car.transform.position, Vector3.zero);
        foreach (var c in dir.Car.GetComponentsInChildren<Collider>()) if (!c.isTrigger) b.Encapsulate(c.bounds);
        Vector3 p = dir.Player.transform.position;
        Vector3 a = b.ClosestPoint(new Vector3(p.x, b.center.y, p.z));
        Vector3 inward = b.center - a; inward.y = 0f;
        a += inward.normalized * Mathf.Min(0.3f, inward.magnitude);
        a.y = b.max.y - 0.1f;
        return a;
    }
}
