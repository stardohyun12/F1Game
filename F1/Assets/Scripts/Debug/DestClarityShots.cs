using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// D-252 검증 캡처(과제 20261102-1409): 첫 배달(차고 → 찻집) 길 위 고정 지점에서 같은 카메라로
/// 이전(SimDestOff) / 점선 / 빛기둥 을 오후·노을 두 번 찍고, 표식 상태(DestMode·픽셀·거리)를 JSON 줄로 남긴다.
/// + 사장 대화 끝 시선 이동 8장, 깜빡임 검사(8초 주행 동안 0.1초마다 DestMode·픽셀).
/// Play 중 exec 로 DestClarityShots.Begin(증거폴더, 다른목적지이름 또는 ""). 진행 = Status.
/// </summary>
public class DestClarityShots : MonoBehaviour
{
    public static string Status = "idle";
    string dir, altName;
    bool flagOnly;   // 깃발 크기 전/후만(4cccf317 후속): 사장 대화·전표·깜빡임·빛기둥 단계를 건너뛴다
    OnFootDirector d;
    DotScreenUI ui;
    readonly StringBuilder log = new StringBuilder();

    public static string Begin(string evidenceDir, string altStop)
    {
        Application.runInBackground = true;
        var g = new GameObject("DestClarityShots").AddComponent<DestClarityShots>();
        g.dir = evidenceDir; g.altName = altStop;
        Directory.CreateDirectory(evidenceDir);
        Status = "running";
        g.StartCoroutine(g.Run());
        return "OK";
    }

    /// <summary>깃발 크기 2단 검증: 같은 카메라로 원래(SimFlagNear) / 새 크기를 오후·노을에 찍고 JSON 줄로 상태를 남긴다.</summary>
    public static string BeginFlag(string evidenceDir, string altStop)
    {
        Application.runInBackground = true;
        var g = new GameObject("DestClarityShots").AddComponent<DestClarityShots>();
        g.dir = evidenceDir; g.altName = altStop; g.flagOnly = true;
        Directory.CreateDirectory(evidenceDir);
        Status = "running";
        g.StartCoroutine(g.Run());
        return "OK";
    }

    static string F(float v) => v.ToString("F1", CultureInfo.InvariantCulture);
    void J(string s) { log.Append(s).Append('\n'); File.WriteAllText(Path.Combine(dir, "dest_log.jsonl"), log.ToString(), new UTF8Encoding(false)); }

    IEnumerator Grab(string name)
    {
        while (PresentGrab.Pending > 0) yield return null;
        yield return null; yield return null;
        PresentGrab.Begin(dir, name, 1, 0f);
        while (PresentGrab.Pending > 0) yield return null;
    }

    string State(string tag) =>
        $"{{\"shot\":\"{tag}\",\"mode\":\"{ui.DestMode}\",\"px\":[{F(ui.DestPx.x)},{F(ui.DestPx.y)}],\"distM\":{F(ui.DestDistM)},\"areaPct\":{ui.DestAreaPct.ToString("F2", CultureInfo.InvariantCulture)},\"ui\":\"{ui.Visible}\",\"flag\":\"{ui.FlagTier}\",\"flagWH\":[{F(ui.FlagSize.x)},{F(ui.FlagSize.y)}],\"dusk\":{F(SunsetLook.Find() != null ? SunsetLook.Find().dusk : -1f)}}}";

    IEnumerator Run()
    {
        d = FindFirstObjectByType<OnFootDirector>();
        ui = FindFirstObjectByType<DotScreenUI>();
        float w = 0f;
        while ((d == null || !d.DebugReady) && w < 20f) { w += Time.unscaledDeltaTime; yield return null; d = d != null ? d : FindFirstObjectByType<OnFootDirector>(); }
        if (d == null || ui == null) { Status = "FAIL no director/ui"; yield break; }
        var bf = BindingFlags.NonPublic | BindingFlags.Instance;

        // 1) 사장 대화 끝 → 목적지 쪽 시선 0.5초 (+0.8초 멈춤). 찻집을 등진 방향으로 서서 시작.
        DebugWindow.SetDusk(0f);
        if (!flagOnly) {
        Vector3 cart = d.DebugCartSpot, tea = d.FirstDrop.transform.position;
        Vector3 away = cart - tea; away.y = 0f;
        d.DebugTeleport(cart + away.normalized * 3f + Vector3.Cross(Vector3.up, away.normalized) * 2.5f, Quaternion.LookRotation(away).eulerAngles.y, null, 0f);
        yield return new WaitForSecondsRealtime(0.5f);
        d.ForceTalk();
        int guard = 0;
        while (d.InTalk && guard++ < 10) { yield return new WaitForSecondsRealtime(0.4f); if (d.InTalk) d.SimInteract(); yield return null; yield return null; }
        // 마지막 E 를 누른 직후부터 8장(0.18초 간격 = 1.4초)
        PresentGrab.Begin(dir, "gaze", 8, 0.18f);
        var gaze = new StringBuilder();
        for (int i = 0; i < 16; i++) { gaze.Append($"[{F(Time.unscaledTime)},{F(d.Player.Cam.Yaw)},\"{ui.DestMode}\"],"); yield return new WaitForSecondsRealtime(0.09f); }
        while (PresentGrab.Pending > 0) yield return null;
        J($"{{\"gaze\":[{gaze.ToString().TrimEnd(',')}],\"talked\":{d.Talked.ToString().ToLower()}}}");
        J(State("onfoot_after_talk"));
        yield return Grab("onfoot_after_talk");

        }

        // 2) 탑승(= 첫 주문 출발)
        typeof(OnFootDirector).GetMethod("EnterCart", bf).Invoke(d, null);
        yield return new WaitForSecondsRealtime(1.0f);
        var mgr = FindFirstObjectByType<DeliveryManager>();
        if (mgr == null || mgr.CurrentTarget == null) { Status = "FAIL no target"; J("{\"fail\":\"no target\"}"); yield break; }

        var rp = typeof(OnFootRouteBot).GetMethod("RoadPath", BindingFlags.NonPublic | BindingFlags.Static);
        var args = new object[] { d.Car.transform.position, mgr.CurrentTarget.transform.position, null };
        var path = rp.Invoke(null, args) as List<Vector3>;
        J($"{{\"path\":\"{args[2]}\",\"n\":{(path != null ? path.Count : 0)}}}");
        if (path == null || path.Count < 4) { Status = "FAIL path"; yield break; }
        // 길이 누적 → 지점 고르기
        var acc = new List<float> { 0f };
        for (int i = 1; i < path.Count; i++) acc.Add(acc[i - 1] + Vector3.Distance(path[i - 1], path[i]));
        float L = acc[acc.Count - 1];
        int At(float m) { for (int i = 0; i < acc.Count; i++) if (acc[i] >= m) return Mathf.Clamp(i, 0, path.Count - 2); return path.Count - 2; }
        var spots = new List<(string tag, int i)>
        {
            ("p1_start", At(8f)), ("p2_fork1", At(L * 0.35f)), ("p3_fork2", At(L * 0.65f)), ("p4_before", At(L - 30f)), ("p5_near", At(L - 11f)),
        };
        if (flagOnly)   // 깃발 크기 검증: 처음 보는 길목 3곳 + 근거리 20 m(큰/작은 경계 아래) 더
        {
            spots.Add(("p6_path15", At(L * 0.15f))); spots.Add(("p7_path50", At(L * 0.5f))); spots.Add(("p8_path80", At(L * 0.8f))); spots.Add(("p9_before20", At(L - 20f)));
        }
        // 3) 지점마다 오후·노을 × 이전/점선/빛기둥
        foreach (var dusk in new[] { 0f, 1f })
        {
            DebugWindow.SetDusk(dusk);
            foreach (var s in spots)
            {
                Vector3 a = path[s.i], b = path[Mathf.Min(s.i + 2, path.Count - 1)];
                Vector3 fw = b - a; fw.y = 0f;
                typeof(OnFootDirector).GetMethod("ParkCar", bf).Invoke(d, new object[] { a, Quaternion.LookRotation(fw).eulerAngles.y });
                d.Car.SetMoveInput(Vector2.zero);
                var follow = FindFirstObjectByType<FollowCamera>();
                if (follow != null) follow.SnapToTarget();
                yield return new WaitForSecondsRealtime(1.2f);
                string tag = s.tag + (dusk > 0.5f ? "_dusk" : "_noon");
                if (flagOnly)
                {
                    ui.SimFlagNear = true; yield return null; yield return null;
                    J(State(tag + "_old")); yield return Grab(tag + "_old");
                    ui.SimFlagNear = false; yield return null; yield return null;
                    J(State(tag + "_new")); yield return Grab(tag + "_new");
                    continue;
                }
                ui.SimDestOff = true; yield return null; yield return null;
                J(State(tag + "_before")); yield return Grab(tag + "_before");
                ui.SimDestOff = false; ui.Style = DotScreenUI.DestStyle.Dots; yield return null; yield return null;
                J(State(tag + "_dots")); yield return Grab(tag + "_dots");
                ui.Style = DotScreenUI.DestStyle.Beam; yield return null; yield return null;
                J(State(tag + "_beam")); yield return Grab(tag + "_beam");
                ui.Style = DotScreenUI.DestStyle.Dots;
            }
        }
        DebugWindow.SetDusk(0f);
        if (flagOnly)   // 경계 왕복: 길 끝에서 거리를 앞뒤로 옮기며 단계가 FlagFarM 위에서만 far, FlagFarM-FlagHysteresisM 아래에서만 near 로 바뀌는지
        {
            Vector3 PointAt(float m)
            {
                for (int i = 1; i < acc.Count; i++)
                    if (acc[i] >= m) return Vector3.Lerp(path[i - 1], path[i], Mathf.InverseLerp(acc[i - 1], acc[i], m));
                return path[path.Count - 1];
            }
            var back = new[] { 40f, 30f, 26f, 24f, 23f, 22.5f, 21f, 20f, 18f, 20f, 21.5f, 23f, 24f, 24.5f, 26f, 27f, 24f, 22.5f, 21.5f, 18f };
            var sweep = new StringBuilder();
            int changes = 0, bad = 0; string last = ui.FlagTier;
            foreach (float m in back)
            {
                Vector3 a = PointAt(L - m), b = PointAt(L - m + 3f);
                Vector3 fw = b - a; fw.y = 0f;
                typeof(OnFootDirector).GetMethod("ParkCar", bf).Invoke(d, new object[] { a, Quaternion.LookRotation(fw).eulerAngles.y });
                d.Car.SetMoveInput(Vector2.zero);
                for (int f = 0; f < 6; f++) yield return null;
                string tier = ui.FlagTier; float dm = ui.DestDistM;
                if (tier != last)
                {
                    changes++;
                    if (tier == "far" && dm <= DotScreenUI.FlagFarM) bad++;
                    if (tier == "near" && dm >= DotScreenUI.FlagFarM - DotScreenUI.FlagHysteresisM) bad++;
                }
                sweep.Append($"[{F(m)},{F(dm)},\"{tier}\",{F(ui.FlagSize.y)}],");
                last = tier;
            }
            J($"{{\"sweep\":{{\"changes\":{changes},\"badSwitch\":{bad},\"farM\":{F(DotScreenUI.FlagFarM)},\"nearBelowM\":{F(DotScreenUI.FlagFarM - DotScreenUI.FlagHysteresisM)},\"seq\":[{sweep.ToString().TrimEnd(',')}]}}}}");
        }
        if (!flagOnly) {
        {   // 전표(Tab) — 첫 지점에서
            Vector3 a = path[spots[0].i], b = path[Mathf.Min(spots[0].i + 2, path.Count - 1)];
            Vector3 fw = b - a; fw.y = 0f;
            typeof(OnFootDirector).GetMethod("ParkCar", bf).Invoke(d, new object[] { a, Quaternion.LookRotation(fw).eulerAngles.y });
            yield return new WaitForSecondsRealtime(1.0f);
            ui.SimSlip = true; ui.SimDestOff = true; yield return null; yield return null;
            J(State("slip_before")); yield return Grab("slip_before");
            ui.SimDestOff = false; yield return null; yield return null;
            J(State("slip_after")); yield return Grab("slip_after");
            ui.SimSlip = false;
        }

        // 4) 깜빡임: fork2(표식이 화면 안·건물 뒤) → 8초 앞으로 굴리며 0.1초마다 상태. mode 가 on↔edge 로 바뀐 횟수와 픽셀 점프(>40px) 횟수.
        {
            Vector3 a = path[spots[2].i], b = path[Mathf.Min(spots[2].i + 2, path.Count - 1)];
            Vector3 fw = b - a; fw.y = 0f;
            typeof(OnFootDirector).GetMethod("ParkCar", bf).Invoke(d, new object[] { a, Quaternion.LookRotation(fw).eulerAngles.y });
            yield return new WaitForSecondsRealtime(1f);
            float d0 = ui.DestDistM;
            int changes = 0, jumps = 0, hidden = 0, n = 0; string last = ui.DestMode; Vector2 lp = ui.DestPx;
            var seq = new StringBuilder();
            int k = spots[2].i;
            for (float t = 0f; t < 8f; t += 0.1f)
            {
                // 길 따라 조향: 다음 점 쪽으로
                while (k < path.Count - 1 && Vector3.Distance(Flat(d.Car.transform.position), Flat(path[k])) < 6f) k++;
                Vector3 to = path[k] - d.Car.transform.position; to.y = 0f;
                float steer = Mathf.Clamp(Vector3.SignedAngle(d.Car.transform.forward, to, Vector3.up) / 30f, -1f, 1f);
                d.Car.SetMoveInput(new Vector2(steer, 0.45f));
                yield return new WaitForSecondsRealtime(0.1f);
                n++;
                if (ui.DestMode != last) changes++;
                if (ui.DestMode == "none") hidden++;
                if (ui.DestMode == last && (ui.DestPx - lp).magnitude > 40f) jumps++;
                if (n % 5 == 0) seq.Append($"[{F(t)},\"{ui.DestMode}\",{F(ui.DestPx.x)},{F(ui.DestPx.y)},{F(ui.DestDistM)}],");
                last = ui.DestMode; lp = ui.DestPx;
            }
            d.Car.SetMoveInput(Vector2.zero);
            J($"{{\"flicker\":{{\"d0\":{F(d0)},\"d1\":{F(ui.DestDistM)},\"samples\":{n},\"modeChanges\":{changes},\"pxJumps40\":{jumps},\"none\":{hidden},\"seq\":[{seq.ToString().TrimEnd(',')}]}}}}");
        }

        }

        // 5) 처음 보는 경로: 다른 배달 정류장 하나로 목적지를 임시로 바꿔 같은 화면.
        if (!string.IsNullOrEmpty(altName))
        {
            var stop = GameObject.Find(altName);
            var dp = stop != null ? stop.GetComponentInChildren<DeliveryPoint>(true) : null;
            if (dp != null)
            {
                dp.gameObject.SetActive(true);
                mgr.SetPoints(new List<DeliveryPoint> { dp }, null);
                yield return new WaitForSecondsRealtime(0.5f);
                if (flagOnly)
                {
                    foreach (var dk in new[] { 0f, 1f })
                    {
                        DebugWindow.SetDusk(dk); yield return new WaitForSecondsRealtime(0.5f);
                        string t = "alt_" + altName + (dk > 0.5f ? "_dusk" : "_noon");
                        ui.SimFlagNear = true; yield return null; yield return null; J(State(t + "_old")); yield return Grab(t + "_old");
                        ui.SimFlagNear = false; yield return null; yield return null; J(State(t + "_new")); yield return Grab(t + "_new");
                    }
                    DebugWindow.SetDusk(0f); Status = "done"; yield break;
                }
                J(State("alt_" + altName + "_noon")); yield return Grab("alt_" + altName + "_noon");
                DebugWindow.SetDusk(1f); yield return new WaitForSecondsRealtime(0.5f);
                J(State("alt_" + altName + "_dusk")); yield return Grab("alt_" + altName + "_dusk");
                ui.SimSlip = true; yield return new WaitForSecondsRealtime(0.2f);
                J(State("alt_slip")); yield return Grab("alt_slip");
                ui.SimSlip = false;
                DebugWindow.SetDusk(0f);
            }
            else J($"{{\"alt\":\"{altName} 없음\"}}");
        }
        Status = "done";
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
}
