#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 두 번째 한 판 메모(D-284) 확인용 짧은 구간 시험 — 전체 경로 봇이 아니다.
/// ① 집 앞(184.8, -66.2)에서 정비소 쪽을 본다(표지판 없음·시선 트임 여부) → ② 사장 앞에서 대화 → 카트를 받아 탐 →
/// ③ 후진 풀 입력 → ④ 정비소에서 보관소 앞까지 실제 입력(SetMoveInput)으로 운전 → 내려서 걷기까지.
/// Play 중 exec: GarageDriveCheck.Begin("D:/.../evidence-dir"). 결과 = 증거폴더/garage_drive.txt, 진행 = GarageDriveCheck.Status.
/// 입력은 키보드 대신 OnFootPlayer.sim* · SimInteract · ArcadeCarController.SetMoveInput. 사람이 못 끼어든다.
/// </summary>
public class GarageDriveCheck : MonoBehaviour
{
    public static string Status = "idle";
    string ev;
    OnFootDirector dir;
    readonly StringBuilder log = new StringBuilder();
    readonly List<string> bad = new List<string>();
    // 마당 동쪽(x≈235, z -54~-52)은 1.8m 낭떠러지+옹벽. 출구는 북쪽(z≥-49)이라 첫 점을 마당 안 (231.5,-50)에 둬 모서리를 못 자르게 한다.
    static readonly Vector2[] Driveway = { new Vector2(231.5f, -50.0f), new Vector2(233.5f, -48.0f), new Vector2(238.5f, -46.8f), new Vector2(243.0f, -51.0f), new Vector2(245.5f, -57.0f), new Vector2(244.5f, -64.0f) };

    public static string Begin(string evidenceDir)
    {
        var d = FindFirstObjectByType<OnFootDirector>();
        if (d == null) return "FAIL: OnFootDirector 없음";
        var old = FindFirstObjectByType<GarageDriveCheck>();
        if (old != null) Destroy(old.gameObject);
        var g = new GameObject("_GarageDriveCheck").AddComponent<GarageDriveCheck>();
        g.ev = evidenceDir; g.dir = d; Status = "starting";
        return "OK";
    }

    static string P(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:F1},{1:F1},{2:F1})", v.x, v.y, v.z);
    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    void Line(string s) { log.AppendLine(s); }

    Vector3 Ground(Vector3 p)
    {
        if (Physics.Raycast(new Vector3(p.x, p.y + 4f, p.z), Vector3.down, out var h, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) p.y = h.point.y + 0.05f;
        return p;
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(ev);
        float wait = 0f;
        while (dir.State == OnFootDirector.Mode.Starting && wait < 10f) { wait += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSeconds(0.5f);
        var pl = dir.Player; var cam = pl.Cam;
        pl.simActive = true; cam.simActive = true;
        float t0 = Time.time;
        var cartAt = dir.MorningCartAt;
        Line($"시작 사람={P(pl.transform.position)} 아침 카트={(cartAt.HasValue ? P(cartAt.Value) : "없음")} 차 실제={P(dir.Car.transform.position)} 사장={P(dir.Boss.position)}");
        if (!cartAt.HasValue || Flat(dir.Car.transform.position - cartAt.Value).magnitude > 1.5f) bad.Add("아침 카트가 정비소 자리에 없다");

        // ── ① 집 앞에서 정비소가 보이나 (순간이동: 집 안 걷기는 이 시험 밖) ──
        Status = "home front";
        Vector3 front = Ground(new Vector3(184.8f, 27.6f, -66.2f));
        pl.Teleport(front, 90f); yield return null; yield return null;
        Vector3 garage = (cartAt ?? dir.Boss.position) + Vector3.up * 1.2f;
        Vector3 eye = Camera.main.transform.position;
        bool blocked = Physics.Linecast(eye, garage, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Line($"집 앞 시야: 눈={P(eye)} 정비소 카트 위={P(garage)} 거리={Vector3.Distance(eye, garage):F0}m 시선={(blocked ? "가림 → " + hit.collider.name + " " + P(hit.point) : "트임")}");
        var sign = GameObject.Find("DepotRoadSign");
        Line($"  표지판 켜짐={(sign != null && sign.activeInHierarchy)}(false 여야) 안내='{dir.Prompt}' Opening={Opening.Step}");
        if (sign != null && sign.activeInHierarchy) bad.Add("표지판이 켜져 있다");
        cam.Pitch = 0f; cam.LookAt(garage); yield return null; yield return null; yield return null;
        PresentGrab.Begin(ev, "02_home_front", 1, 0f);
        yield return new WaitForSecondsRealtime(0.5f);
        // 집 앞에서 정비소 방향으로 눈높이 몇 개 더 — 안 보이는 이유(건물·지형)를 찾는다
        var probes = new StringBuilder();
        foreach (var dy in new[] { 0.5f, 2.0f, 4.0f, 8.0f })
        {
            bool b = Physics.Linecast(eye, (cartAt ?? dir.Boss.position) + Vector3.up * dy, out var h2, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            probes.Append($"[+{dy:F1}m {(b ? "가림 " + h2.collider.name : "트임")}] ");
        }
        Line("  높이별 시선: " + probes);

        // ── ② 사장 앞 → 대화 → 카트 ──
        Status = "boss talk";
        Vector3 bp = dir.Boss.position;
        Vector3 toCart = Flat((cartAt ?? bp) - bp);
        Vector3 stand = Ground(bp + (toCart.sqrMagnitude > 0.01f ? -toCart.normalized : Vector3.back) * 1.6f);
        pl.Teleport(stand, 90f); yield return null; yield return null;
        cam.LookAt(bp + Vector3.up * 1.3f);
        float w = 0f;
        while (dir.Prompt != "E 말하기" && w < 2f) { w += Time.deltaTime; yield return null; }
        Line($"사장 앞 {P(pl.transform.position)} 안내='{dir.Prompt}'");
        if (dir.Prompt != "E 말하기") { bad.Add("사장 말하기 안내 안 뜸: " + dir.Prompt); Done(t0); yield break; }
        dir.SimInteract(); yield return null; yield return null;
        int shown = 0;
        while (dir.InTalk && shown < 6)
        {
            shown++;
            Line($"  자막 {shown}: {dir.Subtitle}");
            yield return new WaitForSecondsRealtime(0.9f);
            if (dir.Subtitle.Contains("열쇠")) { PresentGrab.Begin(ev, "01_garage_key", 1, 0f); yield return new WaitForSecondsRealtime(0.4f); }
            dir.SimInteract(); yield return null; yield return null;
        }
        Line($"대화 끝: 줄 {shown} Talked={dir.Talked} 주문얘기={dir.Subtitle.Contains("주문")}");
        yield return new WaitForSecondsRealtime(1.0f);

        // 카트 옆 → 타기
        Status = "board";
        Vector3 cp = dir.Car.transform.position, right = dir.Car.transform.right;
        Vector3 beside = Ground(cp - right * 1.7f);
        if (Flat(beside - pl.transform.position).magnitude > Flat(cp + right * 1.7f - pl.transform.position).magnitude) beside = Ground(cp + right * 1.7f);
        pl.Teleport(beside, 90f); yield return null; yield return null;
        cam.LookAt(cp + Vector3.up * 0.6f);
        w = 0f;
        while (dir.Prompt != "E 타기" && w < 2f) { w += Time.deltaTime; yield return null; }
        Line($"카트 옆 {P(pl.transform.position)} 안내='{dir.Prompt}'");
        if (dir.Prompt != "E 타기") { bad.Add("E 타기 안 뜸: " + dir.Prompt); Done(t0); yield break; }
        PresentGrab.Begin(ev, "01b_beside_cart", 1, 0f);
        yield return new WaitForSecondsRealtime(0.4f);
        dir.SimInteract();
        w = 0f;
        while (dir.State != OnFootDirector.Mode.InCart && w < 3f) { w += Time.deltaTime; yield return null; }
        if (dir.State != OnFootDirector.Mode.InCart) { bad.Add("카트에 못 탐"); Done(t0); yield break; }
        yield return new WaitForSeconds(0.8f);
        Line($"탐: 상태={dir.State} 주문시작={dir.OrderStarted}(false 여야) Opening={Opening.Step} 걸어갈 곳={(dir.WalkDest != null ? dir.WalkDest.Label : "없음")}");
        if (dir.OrderStarted) bad.Add("정비소에서 탄 게 주문 출발이 됨");

        // ── ③ 정비소 → 보관소 앞 운전 ──
        Status = "drive to depot";
        var car = dir.Car;
        Vector3 park = DebugPlaces.DepotCart;
        var pts = new List<Vector3>();
        foreach (var q in Driveway) pts.Add(new Vector3(q.x, 0f, q.y));
        var path = OnFootRouteBot.RoadPath(new Vector3(Driveway[Driveway.Length - 1].x, 0f, Driveway[Driveway.Length - 1].y), park, out string pinfo);
        Line("보관소 길찾기 " + pinfo);
        if (path == null) { bad.Add("보관소 길찾기 실패: " + pinfo); Done(t0); yield break; }
        pts.AddRange(path);
        float tDrive = Time.time; float distTotal = 0f; for (int i = 1; i < pts.Count; i++) distTotal += Flat(pts[i] - pts[i - 1]).magnitude;
        yield return Follow(pts, () => Flat(car.transform.position - park).magnitude < 3f);
        float driveS = Time.time - tDrive;
        w = 0f;
        while (Mathf.Abs(car.CurrentSpeedMps) > 0.3f && w < 6f) { car.SetMoveInput(new Vector2(0f, -0.5f * Mathf.Sign(car.CurrentSpeedMps))); w += Time.deltaTime; yield return null; }
        car.SetMoveInput(Vector2.zero);
        yield return new WaitForSeconds(0.4f);
        float left = Flat(car.transform.position - park).magnitude;
        Line($"정비소 → 보관소 앞: {driveS:F1}s 경로 {distTotal:F0}m 차={P(car.transform.position)} 보관소 자리까지 {left:F1}m 막힘 {stuckCount}번");
        if (left > 4f) bad.Add($"보관소 앞에 못 닿음({left:F1}m)");

        // ── ④ 후진 풀 입력(보관소 앞 길에서 — 달려온 길로 되돌아가므로 뒤가 트여 있다) ──
        Status = "reverse";
        var rb = car.GetComponent<Rigidbody>();
        var spd = FindFirstObjectByType<SpeedometerHUD>(FindObjectsInactive.Include);
        bool spdWas = spd != null && spd.gameObject.activeSelf;
        if (spd != null) spd.gameObject.SetActive(true);
        yield return null;
        Vector3 from = car.transform.position;
        var rs = new StringBuilder(); float vmax = 0f; bool shot = false;
        car.SetMoveInput(new Vector2(0f, -1f));
        for (float t = 0f; t < 3.0f; t += Time.deltaTime)
        {
            float kmh = -Vector3.Dot(rb.linearVelocity, car.transform.forward) * 3.6f; vmax = Mathf.Max(vmax, kmh);
            if (!shot && t > 2.6f) { shot = true; yield return Capture("03_reverse_speedometer"); }
            if ((int)(t * 2f) != (int)((t - Time.deltaTime) * 2f)) rs.Append($"{t:F1}s={kmh:F1} ");
            yield return null;
        }
        car.SetMoveInput(Vector2.zero);
        Line($"후진 3.0초: km/h {rs}| 최고 {vmax:F1} 뒤로 {Flat(car.transform.position - from).magnitude:F1}m 한도 {car.ReverseMaxMps * 3.6f:F0}km/h");
        if (spd != null) spd.gameObject.SetActive(spdWas);
        float bw = 0f;
        while (Mathf.Abs(car.CurrentSpeedMps) > 0.3f && bw < 8f) { car.SetMoveInput(new Vector2(0f, -0.5f * Mathf.Sign(car.CurrentSpeedMps))); bw += Time.deltaTime; yield return null; }
        car.SetMoveInput(Vector2.zero);
        yield return new WaitForSeconds(0.3f);

        w = 0f;
        while (dir.Prompt != "E 내리기" && w < 3f) { w += Time.deltaTime; yield return null; }
        PresentGrab.Begin(ev, "04_depot_arrive", 1, 0f);
        yield return new WaitForSecondsRealtime(0.4f);
        dir.SimInteract();
        yield return new WaitForSeconds(1.0f);
        Line($"내림: 상태={dir.State} 사람={P(pl.transform.position)} 안내='{dir.Prompt}'");
        if (dir.State != OnFootDirector.Mode.OnFoot) bad.Add("보관소 앞에서 못 내림");
        Done(t0);
    }

    int stuckCount;

    /// <summary>순수 추종(OnFootRouteBot.Follow 와 같은 방식, 순간이동 보정 없음): 앞 4 + v·0.5 m 점으로 조향, 곧은 길 8 m/s·굽은 길 4.5 m/s. 막히면 후진 1.5초, 4번이면 포기.</summary>
    IEnumerator Follow(List<Vector3> path, System.Func<bool> done)
    {
        var car = dir.Car; int idx = 0; float t0 = Time.time, lastImprove = Time.time, bestLeft = float.MaxValue; int recover = 0;
        while (!done())
        {
            Transform c = car.transform; Vector3 cp = c.position;
            float bd = float.MaxValue;
            for (int i = idx; i < Mathf.Min(path.Count, idx + 40); i++) { float d = Flat(path[i] - cp).sqrMagnitude; if (d < bd) { bd = d; idx = i; } }
            float v = car.CurrentSpeedMps;
            float look = 4f + Mathf.Abs(v) * 0.5f;
            int li = idx; float acc = 0f;
            while (li + 1 < path.Count && acc < look) { acc += Flat(path[li + 1] - path[li]).magnitude; li++; }
            int ci = idx; float a2 = 0f; while (ci + 1 < path.Count && a2 < 15f) { a2 += Flat(path[ci + 1] - path[ci]).magnitude; ci++; }
            float bend = ci > idx + 2 ? Vector3.Angle(Flat(path[idx + 1] - path[idx]), Flat(path[ci] - path[ci - 1])) : 0f;
            Vector3 d3 = path[li] - cp;
            float err = Mathf.DeltaAngle(c.eulerAngles.y, Mathf.Atan2(d3.x, d3.z) * Mathf.Rad2Deg);
            float target = (bend > 45f || Mathf.Abs(err) > 35f) ? 4.5f : 8f;
            float thr = v < target ? 0.75f : (v > target + 1.5f ? -0.2f : 0.08f);
            car.SetMoveInput(new Vector2(Mathf.Clamp(err / 35f, -1f, 1f), thr));
            float left = 0f; for (int i = idx; i + 1 < path.Count; i++) left += Flat(path[i + 1] - path[i]).magnitude;
            if (left < bestLeft - 0.5f) { bestLeft = left; lastImprove = Time.time; }
            if (Time.time - lastImprove > 5f)
            {
                recover++; stuckCount++;
                Line($"  막힘 {recover}번째: 남은 {left:F0}m 차={P(cp)} 속도={v:F1}");
                float rb = 0f; while (rb < 1.5f) { car.SetMoveInput(new Vector2(-Mathf.Clamp(err / 35f, -1f, 1f), -0.6f)); rb += Time.deltaTime; yield return null; }
                lastImprove = Time.time;
                if (recover >= 8) { bad.Add("운전 8번 막혀 포기"); yield break; }
            }
            if (Time.time - t0 > 200f) { bad.Add("운전 200초 초과"); yield break; }
            yield return null;
        }
    }

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var tx = ScreenCapture.CaptureScreenshotAsTexture();
        if (tx != null) { File.WriteAllBytes(Path.Combine(ev, name + ".png"), tx.EncodeToPNG()); Line($"캡처 {name} {tx.width}x{tx.height}(속도계 포함 전체 화면)"); Destroy(tx); }
    }

    void Done(float t0)
    {
        Line($"총 {Time.time - t0:F1}s 문제 {bad.Count}: {string.Join(" / ", bad)}");
        File.WriteAllText(Path.Combine(ev, "garage_drive.txt"), log.ToString(), new UTF8Encoding(false));
        Status = bad.Count == 0 ? "done ok" : "done bad=" + bad.Count;
    }
}
#endif
