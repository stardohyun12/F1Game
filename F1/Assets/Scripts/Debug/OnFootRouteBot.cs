using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 기능 1 검증 봇(과제 20261030-2243): 방 → 방문 → 계단 → 현관 → 돌계단 → 인도 → 차고 → 카트 타기 → 진입로 → 큰길 → 내리기.
/// 키·마우스 대신 OnFootPlayer.sim* · OnFootCamera.Yaw · OnFootDirector.SimInteract · ArcadeCarController.SetMoveInput 에 값을 넣는다.
/// 3 초에 0.3 m 도 못 가면 「막힘」으로 적고 그 점으로 옮겨 계속(막힌 곳을 한 번에 다 모으려고).
/// Play 중 exec 로 OnFootRouteBot.Begin(증거폴더, 로그폴더). 결과 = 로그폴더/route.txt, 진행 = OnFootRouteBot.Status.
/// </summary>
public class OnFootRouteBot : MonoBehaviour
{
    public static string Status = "idle";

    // shot 의 p 가 (0,0) 이 아니면 그 점(발 높이 + aimY)을 바라보고 찍는다(1인칭, 과제 20261031-0044).
    struct Step { public string kind, label; public Vector2 p; public float aimY; public Step(string k, string l, float x, float z, float ay = 1.4f) { kind = k; label = l; p = new Vector2(x, z); aimY = ay; } }

    // walk = 걸어서 그 점까지 · door = 근처 문 열기(E) · shot = 캡처 · board = 카트 타기 · drive = 차로 그 점까지 · exit = 서서 내리기
    static readonly Step[] Route =
    {
        new Step("shot", "01_room", 0, 0),
        // 20261030-2334 house_home 9차: 방문 = 방 뒷벽(집 z -0.5) 동쪽 끝, 계단참 통로 = RAIL_02 끝 ~ 동쪽 벽 0.86 m.
        //   점은 집 로컬 → 월드(집 (185, 25.27, -61) yaw 190). 방문은 복도 쪽으로 열려 통로를 반쯤 막으니 지나서 닫는다.
        //   지나는 점 = 문턱에서 0.65 m 복도 안쪽(로컬 z -1.15) — 문턱(z -0.6)에서 닫으면 문짝이 사람을 방 쪽으로 밀어냄(1회차 막힘).
        new Step("walk", "방문 앞", 187.94f, -61.62f),
        new Step("door", "방문", 0, 0),
        new Step("walk", "방문 지나", 188.06f, -60.37f),
        new Step("close", "방문 닫기", 0, 0),
        new Step("walk", "복도", 188.20f, -60.14f),
        new Step("walk", "계단참 입구", 188.82f, -60.05f),
        new Step("walk", "계단참", 189.14f, -59.39f),
        new Step("walk", "계단 위", 188.66f, -59.26f),
        new Step("shot", "02_stairs", 184.62f, -58.55f, -1.2f),
        new Step("walk", "계단 아래", 184.62f, -58.55f),
        new Step("walk", "1층 계단 앞", 184.42f, -59.68f),
        new Step("walk", "현관 앞", 184.62f, -63.17f),
        new Step("door", "현관문", 0, 0),
        new Step("shot", "03_porch", 184.6f, -66.0f, 1.3f),
        new Step("walk", "현관 밖", 184.6f, -65.0f),
        new Step("close", "현관문 닫기", 0, 0),
        new Step("walk", "마당", 184.8f, -66.2f),
        new Step("shot", "03b_outside_door", 0, 0),
        new Step("walk", "마당 동쪽", 194.6f, -68.3f),
        new Step("walk", "돌계단 위", 196.3f, -68.6f),
        new Step("walk", "돌계단 아래", 197.0f, -76.9f),
        new Step("walk", "집 앞 인도", 197.3f, -77.9f),
        new Step("walk", "인도 200", 200.0f, -77.4f),
        new Step("walk", "인도 205", 205.0f, -76.5f),
        new Step("shot", "04_sidewalk", 0, 0),
        new Step("walk", "인도 210", 210.0f, -75.6f),
        new Step("walk", "인도 214", 214.3f, -74.9f),
        new Step("walk", "차고 계단 앞", 215.6f, -74.6f),
        new Step("walk", "차고 계단 아래", 215.6f, -73.7f),
        new Step("walk", "차고 계단 위", 215.4f, -65.0f),
        new Step("walk", "골목 222", 222.0f, -64.0f),
        new Step("walk", "골목 230", 230.0f, -63.3f),
        new Step("walk", "차고 마당", 229.4f, -57.6f),
        // 1인칭 시선 2 m: 카트 몸통 상자는 낮다(윗면 = 눈 아래 1.2 m) → 몸통 가장자리 0.6 m 까지 붙어서 본다.
        new Step("walk", "카트 옆", 229.45f, -57.2f),
        new Step("aimcar", "카트 보기", 0, 0),
        new Step("shot", "05_beside_cart", 0, 0),
        new Step("board", "카트 타기", 0, 0),
        new Step("drive", "진입로 시작", 233.5f, -48.0f),
        new Step("drive", "진입로 굽이", 238.5f, -46.8f),
        new Step("drive", "진입로 중간", 243.0f, -51.0f),
        new Step("shot", "06_cart_driving", 0, 0),
        new Step("drive", "진입로 아래", 245.5f, -57.0f),
        new Step("drive", "큰길 합류", 244.5f, -64.0f),
        new Step("drive", "큰길", 240.0f, -70.5f),
        new Step("drive", "큰길 서쪽", 230.0f, -72.0f),
        new Step("exit", "내리기", 0, 0),
    };

    string evDir, logDir;
    OnFootDirector dir;
    readonly StringBuilder log = new StringBuilder();
    readonly List<string> stuck = new List<string>();

    public static string Begin(string evidenceDir, string logDirectory)
    {
        var d = FindFirstObjectByType<OnFootDirector>();
        if (d == null) return "FAIL: OnFootDirector 없음";
        var old = FindFirstObjectByType<OnFootRouteBot>();
        if (old != null) Destroy(old.gameObject);
        var bot = new GameObject("_OnFootRouteBot").AddComponent<OnFootRouteBot>();
        bot.evDir = evidenceDir; bot.logDir = logDirectory; bot.dir = d;
        Status = "starting";
        return "OK";
    }

    IEnumerator Start()
    {
        if (doorCheck) { yield return DoorCheck(); yield break; }
        Directory.CreateDirectory(evDir); Directory.CreateDirectory(logDir);
        float wait = 0f;
        while (dir.State == OnFootDirector.Mode.Starting && wait < 10f) { wait += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSeconds(0.5f);
        var pl = dir.Player; var cam = pl.Cam;
        pl.simActive = true; cam.simActive = true;
        float tStart = Time.time, tSeg = Time.time;
        Line($"시작 {P(pl.transform.position)}");
        foreach (var s in Route)
        {
            // 기능 2: 차고 마당까지는 기능 1 길 그대로, 거기서부터 사장 대화 → 카트 → 찻집.
            if (firstOrder && s.label == "카트 옆") { yield return FirstOrderTail(tStart, tSeg); yield break; }
            Status = s.label;
            switch (s.kind)
            {
                case "shot":
                    if (s.p != Vector2.zero) { cam.LookAt(new Vector3(s.p.x, pl.transform.position.y + s.aimY, s.p.y)); yield return null; yield return null; }
                    yield return Shot(s.label); break;
                case "walk": yield return Walk(s); break;
                case "aimcar": cam.LookAt(CarAim()); yield return null; yield return null; break;
                case "door":
                case "close":
                {
                    string want = s.kind == "door" ? "열기" : "닫기";
                    // 1인칭: 문을 바라봐야 안내가 뜬다 — 가장 가까운 문짝을 본다.
                    var nd = NearestDoor(pl.transform.position);
                    if (nd != null) cam.LookAt(DoorAim(nd));
                    float w = 0f;
                    while (!dir.Prompt.Contains(want) && w < 2f) { w += Time.deltaTime; yield return null; }
                    if (!dir.Prompt.Contains(want)) { Stuck(s.label, $"안내 '{dir.Prompt}' ({want} 아님)"); break; }
                    dir.SimInteract();
                    yield return new WaitForSeconds(0.55f);
                    break;
                }
                case "board":
                {
                    pl.simMove = Vector2.zero;
                    float w = 0f;
                    while (dir.Prompt != "E 타기" && w < 2f) { w += Time.deltaTime; yield return null; }
                    if (dir.Prompt != "E 타기") { Stuck(s.label, $"안내 '{dir.Prompt}'"); Status = "FAIL board"; Finish(tStart); yield break; }
                    Seg("걷기 끝(카트 앞)", ref tSeg);
                    dir.SimInteract();
                    yield return CamTrace("타기");
                    if (dir.State != OnFootDirector.Mode.InCart) { Stuck(s.label, "타기 안 됨"); Status = "FAIL board"; Finish(tStart); yield break; }
                    yield return new WaitForSeconds(0.6f);
                    break;
                }
                case "drive": yield return Drive(s); break;
                case "exit":
                {
                    float w = 0f;
                    while (Mathf.Abs(dir.Car.CurrentSpeedMps) > 0.5f && w < 6f) { dir.Car.SetMoveInput(new Vector2(0f, -0.4f * Mathf.Sign(dir.Car.CurrentSpeedMps))); w += Time.deltaTime; yield return null; }
                    dir.Car.SetMoveInput(Vector2.zero);
                    yield return new WaitForSeconds(0.3f);
                    Seg("운전 끝", ref tSeg);
                    w = 0f;
                    while (dir.Prompt != "E 내리기" && w < 3f) { w += Time.deltaTime; yield return null; }
                    dir.SimInteract();
                    yield return CamTrace("내리기");
                    Line(dir.State == OnFootDirector.Mode.OnFoot ? $"내림 OK 사람={P(pl.transform.position)}" : $"내리기 실패 안내='{dir.Prompt}' 속도={dir.Car.CurrentSpeedMps:F2}");
                    if (dir.State != OnFootDirector.Mode.OnFoot) stuck.Add("내리기");
                    break;
                }
            }
            if (s.kind == "walk" || s.kind == "drive") Line($"{s.label} 도착 t={Time.time - tStart:F1}s {P(s.kind == "walk" ? pl.transform.position : dir.Car.transform.position)}{stepNote}");
            stepNote = "";
        }
        pl.simMove = Vector2.zero;
        Finish(tStart);
    }

    IEnumerator Walk(Step s)
    {
        var pl = dir.Player; var cam = pl.Cam;
        cam.Pitch = 0f;
        float best = float.MaxValue, lastImprove = Time.time, t0 = Time.time;
        // 계단 덜컹임: 프레임마다 발 높이 변화와 눈(카메라) 높이 변화 중 가장 큰 것.
        var camT = Camera.main.transform;
        float footY = pl.transform.position.y, eyeY = camT.position.y, maxFoot = 0f, maxEye = 0f;
        while (true)
        {
            Vector3 p = pl.transform.position;
            maxFoot = Mathf.Max(maxFoot, Mathf.Abs(p.y - footY)); footY = p.y;
            maxEye = Mathf.Max(maxEye, Mathf.Abs(camT.position.y - eyeY)); eyeY = camT.position.y;
            if (maxFoot > 0.05f) stepNote = $" 한 프레임 최대 높이변화 발 {maxFoot:F3}m / 눈 {maxEye:F3}m";
            Vector2 d = s.p - new Vector2(p.x, p.z);
            float dist = d.magnitude;
            if (dist < 0.3f) break;
            if (dist < best - 0.3f) { best = dist; lastImprove = Time.time; }
            if (Time.time - lastImprove > 3f || Time.time - t0 > 25f)
            {
                Stuck(s.label, $"남은 {dist:F2}m 사람={P(p)}");
                Vector3 to = new Vector3(s.p.x, p.y + 0.5f, s.p.y);
                // 천장에 안 걸리게 허리 높이에서 아래로(지붕 위로 올라간 적 있음)
                if (Physics.Raycast(new Vector3(s.p.x, p.y + 0.9f, s.p.y), Vector3.down, out var h, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) to.y = h.point.y + 0.05f;
                pl.Teleport(to, pl.transform.eulerAngles.y);
                yield return null;
                break;
            }
            float want = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            cam.Yaw = Mathf.MoveTowardsAngle(cam.Yaw, want, 360f * Time.deltaTime);
            float off = Mathf.Abs(Mathf.DeltaAngle(cam.Yaw, want));
            pl.simMove = new Vector2(0f, off > 60f ? 0.2f : (dist < 0.8f ? 0.6f : 1f));
            yield return null;
        }
        pl.simMove = Vector2.zero;
    }

    IEnumerator Drive(Step s)
    {
        var car = dir.Car;
        float best = float.MaxValue, lastImprove = Time.time, t0 = Time.time;
        while (true)
        {
            Transform c = car.transform;
            Vector2 d = s.p - new Vector2(c.position.x, c.position.z);
            float dist = d.magnitude;
            if (dist < 2.5f) break;
            if (dist < best - 0.3f) { best = dist; lastImprove = Time.time; }
            if (Time.time - lastImprove > 4f || Time.time - t0 > 30f)
            {
                Stuck(s.label, $"남은 {dist:F1}m 차={P(c.position)} 속도={car.CurrentSpeedMps:F1}");
                break;
            }
            float want = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            float err = Mathf.DeltaAngle(c.eulerAngles.y, want);
            float steer = Mathf.Clamp(err / 35f, -1f, 1f);
            float v = car.CurrentSpeedMps;
            float target = Mathf.Abs(err) > 40f ? 4f : 8f;
            float thr = v < target ? 0.75f : (v > target + 2f ? -0.3f : 0.1f);
            car.SetMoveInput(new Vector2(steer, thr));
            yield return null;
        }
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        if (lr != null && lr.enabled) { cam.Render(); lr.Present(rt); }
        else { var prevT = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prevT; }
        var prev = RenderTexture.active; RenderTexture.active = rt;
        tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(evDir, name + ".png"), tx.EncodeToPNG());
        rt.Release(); Destroy(rt); Destroy(tx);
        Line($"캡처 {name} 안내='{dir.Prompt}'");
    }

    // ── 1인칭 시선(과제 20261031-0044) ──
    static DoorHinge NearestDoor(Vector3 p)
    {
        DoorHinge pick = null; float best = float.MaxValue;
        foreach (var d in FindObjectsByType<DoorHinge>(FindObjectsSortMode.None))
        {
            if (!d.configured) continue;
            float dist = Vector3.Distance(d.CenterWorld, p + Vector3.up * 1f);
            if (dist < best) { best = dist; pick = d; }
        }
        return pick;
    }

    /// <summary>문짝에서 바라볼 점 — 가운데보다 문 끝 쪽(경첩에서 폭의 3/4). 열린 문은 가운데가 문틀 뒤에 숨기 쉽다.</summary>
    static Vector3 DoorAim(DoorHinge d)
    {
        Vector3 c = d.CenterWorld;
        Vector3 h = d.transform.parent != null ? d.transform.parent.TransformPoint(d.hingeLocal) : d.hingeLocal;
        Vector3 a = h + (c - h) * 1.5f; a.y = c.y;
        return a;
    }

    Vector3 CarAim()
    {
        var b = new Bounds(dir.Car.transform.position, Vector3.zero);
        foreach (var c in dir.Car.GetComponentsInChildren<Collider>()) if (!c.isTrigger) b.Encapsulate(c.bounds);
        // 사람한테 가까운 몸통 윗면 모서리에서 0.3 m 안쪽(가운데는 2 m 레이 밖일 수 있다).
        Vector3 p = dir.Player.transform.position;
        Vector3 a = b.ClosestPoint(new Vector3(p.x, b.center.y, p.z));
        Vector3 inward = b.center - a; inward.y = 0f;
        a += inward.normalized * Mathf.Min(0.3f, inward.magnitude);
        a.y = b.max.y - 0.05f;
        return a;
    }

    /// <summary>타기·내리기 앞뒤 6 프레임의 카메라 자리 — 중간에 날아가는 프레임이 있는지.</summary>
    IEnumerator CamTrace(string label)
    {
        var c = Camera.main.transform;
        Vector3 prev = c.position; var sb = new StringBuilder($"카메라 {label}: 전 {P(prev)} fov={Camera.main.fieldOfView:F1}");
        for (int i = 1; i <= 6; i++)
        {
            yield return null;
            sb.Append($" | f{i} Δ{Vector3.Distance(prev, c.position):F2}m");
            prev = c.position;
        }
        sb.Append($" 후 {P(prev)} fov={Camera.main.fieldOfView:F1}");
        Line(sb.ToString());
    }

    /// <summary>
    /// 문 5개 시선 점검: 문마다 문 앞 1 m 에 서서 문짝을 바라봄 → 'E 열기' → E → 열림 → 열린 문짝을 바라봄 → 'E 닫기' → E → 닫힘.
    /// 문 양쪽 중 바닥이 있고 몸이 들어가는 쪽에 선다. 결과 = 로그폴더/doors.txt, 진행 = Status.
    /// </summary>
    public static string BeginDoorCheck(string logDirectory)
    {
        var d = FindFirstObjectByType<OnFootDirector>();
        if (d == null) return "FAIL: OnFootDirector 없음";
        var old = FindFirstObjectByType<OnFootRouteBot>();
        if (old != null) Destroy(old.gameObject);
        var bot = new GameObject("_OnFootRouteBot").AddComponent<OnFootRouteBot>();
        bot.logDir = logDirectory; bot.dir = d; bot.doorCheck = true;
        Status = "starting";
        return "OK";
    }

    bool doorCheck;
    string stepNote = "";

    IEnumerator DoorCheck()
    {
        Directory.CreateDirectory(logDir);
        while (dir.State != OnFootDirector.Mode.OnFoot) yield return null;
        var pl = dir.Player; var cam = pl.Cam; var cc = pl.Controller;
        pl.simActive = true; cam.simActive = true; pl.simMove = Vector2.zero;
        int ok = 0, total = 0;
        foreach (var d in FindObjectsByType<DoorHinge>(FindObjectsSortMode.InstanceID))
        {
            if (!d.configured) continue;
            total++; Status = "doors " + d.name;
            if (d.IsOpen) { cam.LookAt(DoorAim(d)); yield return null; }
            Vector3 up = Vector3.up;
            Vector3 leaf = d.transform.parent != null ? d.transform.parent.TransformDirection(d.leafDirLocal) : d.leafDirLocal;
            Vector3 n = Vector3.Cross(up, leaf); n.y = 0f; n.Normalize();
            Vector3 c = d.CenterWorld;
            string res = null;
            foreach (float side in new[] { 1f, -1f })
            {
                Vector3 p = c + n * side * 1.0f;
                if (!Physics.Raycast(new Vector3(p.x, c.y + 0.3f, p.z), Vector3.down, out var hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                p = hit.point + Vector3.up * 0.05f;
                if (Physics.CheckCapsule(p + up * (cc.radius + 0.05f), p + up * (cc.height - cc.radius), cc.radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                pl.Teleport(p, Quaternion.LookRotation(-n * side).eulerAngles.y);
                yield return null;
                cam.LookAt(DoorAim(d)); yield return null; yield return null;
                string p1 = dir.Prompt, g1 = dir.GazeTarget;
                if (p1 != "E 열기") { res = $"FAIL 닫힌 문 안내='{p1}' 시선={g1 ?? "-"} side={side}"; continue; }
                dir.SimInteract(); yield return new WaitForSeconds(0.6f);
                bool opened = d.IsOpen;
                cam.LookAt(DoorAim(d)); yield return null; yield return null;
                string p2 = dir.Prompt, g2 = dir.GazeTarget;
                if (p2 == "E 닫기") { dir.SimInteract(); yield return new WaitForSeconds(0.6f); }
                bool closed = !d.IsOpen;
                bool pass = opened && p2 == "E 닫기" && closed;
                res = $"{(pass ? "OK" : "FAIL")} 섬={P(p)} 열기 안내='{p1}' 시선={g1} → 열림={opened} · 닫기 안내='{p2}' 시선={g2 ?? "-"} → 닫힘={closed}";
                if (pass) { ok++; break; }
            }
            Line($"{d.name}: {res ?? "FAIL 설 자리 없음(문 양쪽 바닥/몸)"}");
        }
        Line($"문 {ok}/{total} 통과");
        File.WriteAllText(Path.Combine(logDir, "doors.txt"), log.ToString(), new UTF8Encoding(false));
        Status = $"doors done {ok}/{total}";
    }

    void Seg(string label, ref float tSeg) { Line($"구간 {label}: {Time.time - tSeg:F1}s"); tSeg = Time.time; }
    void Stuck(string label, string why) { stuck.Add(label); Line($"막힘 {label}: {why}"); }
    void Line(string s) { log.AppendLine(s); }
    static string P(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2})", v.x, v.y, v.z);

    void Finish(float tStart)
    {
        Line($"총 {Time.time - tStart:F1}s 막힘 {stuck.Count}곳: {string.Join(", ", stuck)}");
        Line("감독 기록:"); foreach (var l in dir.Log) Line("  " + l);
        File.WriteAllText(Path.Combine(logDir, firstOrder ? "route_feature2.txt" : "route.txt"), log.ToString(), new UTF8Encoding(false));
        if (!Status.StartsWith("FAIL")) Status = $"done stuck={stuck.Count}";
    }

    // ───────────────────────── 기능 2(과제 20261031-0209): 방 → 사장 대화 → 카트 → 찻집 도착 ─────────────────────────
    bool firstOrder;

    /// <summary>기능 2 봇. 결과 = 로그폴더/route_feature2.txt, 캡처(UI 포함) = 증거폴더/f2_*.png, 진행 = Status.</summary>
    public static string BeginFirstOrder(string evidenceDir, string logDirectory)
    {
        var r = Begin(evidenceDir, logDirectory);
        if (r != "OK") return r;
        var bot = FindFirstObjectByType<OnFootRouteBot>();
        bot.firstOrder = true;
        if (bot.dir.Boss == null || bot.dir.FirstDrop == null) return "WARN: 사장·배달지 배선 없음(PitCrew/기능/정비소 첫 주문 설치 먼저) — 봇은 돈다";
        return "OK";
    }

    // 기능 1 운전 길에서 큰길에 올라선 점까지(진입로는 RoadSurface 가 아닐 수 있다). 그 뒤는 차길 그래프로.
    static readonly Vector2[] Driveway = { new Vector2(233.5f, -48.0f), new Vector2(238.5f, -46.8f), new Vector2(243.0f, -51.0f), new Vector2(245.5f, -57.0f), new Vector2(244.5f, -64.0f) };

    IEnumerator FirstOrderTail(float tStart, float tSeg)
    {
        var pl = dir.Player; var cam = pl.Cam;
        pl.simMove = Vector2.zero;
        Seg("방 → 차고 마당", ref tSeg);

        // 1) 사장 말 듣기 전 카트: 안내 "사장님한테 먼저", E 눌러도 안 탄다
        cam.LookAt(CarAim()); yield return null; yield return null; yield return null;
        string pre = dir.Prompt;
        yield return ShotUI("f2_01_cart_before_talk");
        dir.SimInteract(); yield return null; yield return null;
        bool refused = dir.State == OnFootDirector.Mode.OnFoot;
        Line($"대화 전 카트: 안내='{pre}' 시선={dir.GazeTarget ?? "-"} E→ 안 탐={refused}");
        if (pre != "사장님한테 먼저" || !refused) stuck.Add("대화 전 카트 막기");

        // 2) 사장 앞으로 걸어가서 바라보고 E
        var boss = dir.Boss;
        if (boss == null) { Stuck("사장", "배선 없음"); Status = "FAIL no boss"; Finish(tStart); yield break; }
        Vector3 bp = boss.position; Vector3 me = pl.transform.position;
        Vector3 away = me - bp; away.y = 0f; if (away.sqrMagnitude < 1e-4f) away = boss.forward;
        Vector3 front = bp + away.normalized * 1.6f;
        yield return Walk(new Step("walk", "사장 앞", front.x, front.z));
        Line($"사장 앞 도착 {P(pl.transform.position)} 사장={P(bp)} 거리={Vector3.Distance(Flat(pl.transform.position), Flat(bp)):F2}m");
        cam.LookAt(bp + Vector3.up * 1.3f);
        float w = 0f;
        while (dir.Prompt != "E 말하기" && w < 2f) { w += Time.deltaTime; yield return null; }
        if (dir.Prompt != "E 말하기") { Stuck("사장 보기", $"안내 '{dir.Prompt}' 시선={dir.GazeTarget ?? "-"}"); Status = "FAIL talk"; Finish(tStart); yield break; }
        yield return ShotUI("f2_02_boss_prompt");
        dir.SimInteract(); yield return null; yield return null;
        int shown = 0;
        while (dir.InTalk && shown < 6)
        {
            shown++;
            Line($"자막 {shown}: {dir.Subtitle}");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return ShotUI($"f2_03_talk_{shown}");
            dir.SimInteract(); yield return null; yield return null;
        }
        Line($"대화 끝 줄수={shown} Talked={dir.Talked}");
        if (!dir.Talked) { Stuck("사장 대화", "안 끝남"); Status = "FAIL talk"; Finish(tStart); yield break; }
        Seg("사장 대화", ref tSeg);

        // 3) 카트 옆 → 타기 = 첫 주문 출발
        yield return Walk(new Step("walk", "카트 옆", 229.45f, -57.2f));
        cam.LookAt(CarAim());
        w = 0f;
        while (dir.Prompt != "E 타기" && w < 2f) { w += Time.deltaTime; yield return null; }
        if (dir.Prompt != "E 타기") { Stuck("카트 타기", $"안내 '{dir.Prompt}'"); Status = "FAIL board"; Finish(tStart); yield break; }
        yield return ShotUI("f2_04_beside_cart");
        dir.SimInteract();
        yield return CamTrace("타기");
        if (dir.State != OnFootDirector.Mode.InCart) { Stuck("카트 타기", "타기 안 됨"); Status = "FAIL board"; Finish(tStart); yield break; }
        var mgr = FindFirstObjectByType<DeliveryManager>();
        var cargo = FindFirstObjectByType<CargoBox>();
        var tgt = mgr != null ? mgr.CurrentTarget : null;
        Line($"탑승: 주문시작={dir.OrderStarted} 목표={(tgt != null ? tgt.Label + " " + P(tgt.transform.position) : "없음")} 화물={(cargo != null ? cargo.Type + " " + Pct(cargo) : "없음")}");
        if (!dir.OrderStarted || tgt == null || tgt != dir.FirstDrop) stuck.Add("첫 주문 시작");
        yield return new WaitForSecondsRealtime(1.2f);
        yield return ShotUI("f2_05_hud_target");
        Seg("걷기 끝 → 탑승", ref tSeg);

        // 4) 찻집까지 운전: 진입로(기능 1 점) → 차길 그래프 최단길 → 도착 감지
        float tDrive = Time.time;
        foreach (var q in Driveway) { yield return Drive(new Step("drive", "진입로", q.x, q.y)); if (dir.Delivered) break; }
        Line($"진입로 끝 {P(dir.Car.transform.position)} 화물={Pct(cargo)}");
        if (!dir.Delivered)
        {
            var path = RoadPath(dir.Car.transform.position, dir.FirstDrop.transform.position, out string pinfo);
            Line("길찾기 " + pinfo);
            if (path == null) { Stuck("찻집 길찾기", pinfo); Status = "FAIL path"; Finish(tStart); yield break; }
            yield return Follow(path, cargo);
        }
        float drive = Time.time - tDrive;
        dir.Car.SetMoveInput(Vector2.zero);
        Line($"운전 {drive:F1}s 도착={dir.Delivered} 차={P(dir.Car.transform.position)} 찻집까지 {Vector3.Distance(Flat(dir.Car.transform.position), Flat(dir.FirstDrop.transform.position)):F1}m 화물={Pct(cargo)} 진행판정 runActive={(Services.Run != null && Services.Run.RunActive)}");
        if (!dir.Delivered) stuck.Add("찻집 도착");
        yield return new WaitForSecondsRealtime(1.0f);              // EndRun 이 timeScale 0 으로 만든다 — 실시간으로 기다림
        yield return ShotUI("f2_06_arrival");
        Seg("운전(출발 → 도착)", ref tSeg);
        Line($"결과: 총 {Time.time - tStart:F1}s(게임 시간, 도착 뒤 정지) 화물 상태 {Pct(cargo)}");
        Status = dir.Delivered ? "arrived" : "FAIL not arrived";
        Finish(tStart);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    static string Pct(CargoBox c) => c != null ? (c.IntegrityPercent * 100f).ToString("F0", CultureInfo.InvariantCulture) + "%" : "?";

    /// <summary>
    /// 차길 그래프 최단길(TownPathBot.Pairs 와 같은 방식): 중심선 점 = 노드, 앞뒤 연결 + 다른 길 점 2.5 m 안이면 갈림목.
    /// 계단·인도 띠는 뺀다. 지름길(shortcut)은 달걀이 흔들려서 먼저 빼 보고, 길이 없으면 넣는다.
    /// </summary>
    static List<Vector3> RoadPath(Vector3 from, Vector3 to, out string info)
    {
        var roads = new List<RoadSurface>();
        foreach (var r in FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
            if (r.centerline != null && r.centerline.Length >= 2 && r.kind != "stairs" && (r.kind == null || !r.kind.StartsWith("walk"))) roads.Add(r);
        roads.Sort((a, b) => string.CompareOrdinal(a.roadId, b.roadId));
        var nodes = new List<Vector3>(); var road = new List<int>();
        for (int ri = 0; ri < roads.Count; ri++) foreach (var p in roads[ri].centerline) { nodes.Add(p); road.Add(ri); }
        info = $"길 {roads.Count}개 노드 {nodes.Count}";
        if (nodes.Count == 0) return null;
        var adj = new List<(int, float)>[nodes.Count]; for (int i = 0; i < nodes.Count; i++) adj[i] = new List<(int, float)>();
        void Link(int a, int b) { float d = Vector3.Distance(nodes[a], nodes[b]); adj[a].Add((b, d)); adj[b].Add((a, d)); }
        for (int i = 0; i + 1 < nodes.Count; i++) if (road[i] == road[i + 1]) Link(i, i + 1);
        var grid = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < nodes.Count; i++) { var k = ((int)Mathf.Floor(nodes[i].x / 3f), (int)Mathf.Floor(nodes[i].z / 3f)); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<int>(); l.Add(i); }
        for (int i = 0; i < nodes.Count; i++)
        {
            int gx = (int)Mathf.Floor(nodes[i].x / 3f), gz = (int)Mathf.Floor(nodes[i].z / 3f);
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) if (grid.TryGetValue((gx + dx, gz + dz), out var l))
                foreach (var j in l) if (j > i && road[j] != road[i] && Vector3.Distance(nodes[i], nodes[j]) < 2.5f) Link(i, j);
        }
        int Near(Vector3 q) { int bi = 0; float bd = float.MaxValue; for (int i = 0; i < nodes.Count; i++) { float d = Flat(nodes[i] - q).sqrMagnitude; if (d < bd) { bd = d; bi = i; } } return bi; }
        int na = Near(from), nb = Near(to);
        info += $" 시작점 오차 {Vector3.Distance(Flat(nodes[na]), Flat(from)):F1}m 끝점 오차 {Vector3.Distance(Flat(nodes[nb]), Flat(to)):F1}m";
        foreach (bool rough in new[] { false, true })
        {
            var dist = new float[nodes.Count]; var prev = new int[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
            var pq = new SortedSet<(float, int)>(); dist[na] = 0; pq.Add((0, na));
            while (pq.Count > 0)
            {
                var (d, u) = pq.Min; pq.Remove(pq.Min); if (u == nb) break; if (d > dist[u]) continue;
                foreach (var (wv, c) in adj[u])
                {
                    if (!rough && roads[road[wv]].kind == "shortcut") continue;
                    float nd = d + c; if (nd < dist[wv]) { if (dist[wv] < float.MaxValue) pq.Remove((dist[wv], wv)); dist[wv] = nd; prev[wv] = u; pq.Add((nd, wv)); }
                }
            }
            if (prev[nb] < 0 && na != nb) continue;
            var ids = new List<int>(); for (int x = nb; x >= 0; x = prev[x]) ids.Add(x); ids.Reverse();
            var path = new List<Vector3> { nodes[ids[0]] };
            foreach (var i in ids) if (Vector3.Distance(nodes[i], path[path.Count - 1]) > 0.3f) path.Add(nodes[i]);
            path.Add(to);
            var used = new HashSet<string>(); foreach (var i in ids) used.Add(roads[road[i]].roadId + ":" + roads[road[i]].kind);
            info += $" 길이 {dist[nb]:F0}m 지름길{(rough ? " 포함" : " 뺌")} 지나는 길 {string.Join(",", used)}";
            return path;
        }
        info += " 경로 없음";
        return null;
    }

    /// <summary>순수 추종: 앞 4 + v·0.5 m 점을 향해 조향. 달걀이라 살살(곧은 길 8 m/s, 굽은 길 4.5 m/s, 스로틀 0.5). 막히면 1.5초 후진 후 다시.</summary>
    IEnumerator Follow(List<Vector3> path, CargoBox cargo)
    {
        var car = dir.Car; int idx = 0; float t0 = Time.time, lastImprove = Time.time, bestLeft = float.MaxValue; int recover = 0;
        float total = 0f; for (int i = 1; i < path.Count; i++) total += Vector3.Distance(path[i], path[i - 1]);
        float nextLog = 0f;
        while (!dir.Delivered)
        {
            Transform c = car.transform; Vector3 cp = c.position;
            float bd = float.MaxValue;
            for (int i = idx; i < Mathf.Min(path.Count, idx + 40); i++) { float d = Flat(path[i] - cp).sqrMagnitude; if (d < bd) { bd = d; idx = i; } }
            float v = car.CurrentSpeedMps;
            float look = 4f + Mathf.Abs(v) * 0.5f;
            int li = idx; float acc = 0f;
            while (li + 1 < path.Count && acc < look) { acc += Vector3.Distance(path[li], path[li + 1]); li++; }
            Vector3 la = path[li];
            // 앞 15 m 의 꺾임으로 속도 결정
            int ci = idx; float a2 = 0f; while (ci + 1 < path.Count && a2 < 15f) { a2 += Vector3.Distance(path[ci], path[ci + 1]); ci++; }
            float bend = 0f;
            if (ci > idx + 2) bend = Vector3.Angle(Flat(path[idx + 1] - path[idx]), Flat(path[ci] - path[ci - 1]));
            Vector3 d3 = la - cp;
            float want = Mathf.Atan2(d3.x, d3.z) * Mathf.Rad2Deg;
            float err = Mathf.DeltaAngle(c.eulerAngles.y, want);
            float target = (bend > 45f || Mathf.Abs(err) > 35f) ? 4.5f : 8f;
            float thr = v < target ? 0.5f : (v > target + 1.5f ? -0.2f : 0.08f);
            car.SetMoveInput(new Vector2(Mathf.Clamp(err / 35f, -1f, 1f), thr));
            float left = 0f; for (int i = idx; i + 1 < path.Count; i++) left += Vector3.Distance(path[i], path[i + 1]);
            if (left < bestLeft - 0.5f) { bestLeft = left; lastImprove = Time.time; }
            if (Time.time >= nextLog) { nextLog = Time.time + 5f; Line($"  운전 {Time.time - t0:F0}s 남은 {left:F0}/{total:F0}m 속도 {v:F1} 화물 {Pct(cargo)}"); }
            if (Time.time - lastImprove > 5f)
            {
                recover++;
                Stuck("운전", $"남은 {left:F0}m 차={P(cp)} 속도={v:F1} → 후진 {recover}번째");
                float rb = 0f; while (rb < 1.5f) { car.SetMoveInput(new Vector2(-Mathf.Clamp(err / 35f, -1f, 1f), -0.6f)); rb += Time.deltaTime; yield return null; }
                lastImprove = Time.time;
                if (recover >= 4) { Line("  4번 막혀서 포기"); yield break; }
            }
            if (Time.time - t0 > 300f) { Line("  300초 초과 — 포기"); yield break; }
            if (left < 1f && Flat(path[path.Count - 1] - cp).magnitude < 2f) { car.SetMoveInput(new Vector2(0f, -0.3f)); }
            yield return null;
        }
    }

    /// <summary>UI(자막·안내·HUD) 까지 들어간 화면 캡처 — Shot() 은 카메라만 찍어서 화면 글이 안 나온다.</summary>
    IEnumerator ShotUI(string name)
    {
        yield return new WaitForEndOfFrame();
        var tx = ScreenCapture.CaptureScreenshotAsTexture();
        if (tx != null)
        {
            File.WriteAllBytes(Path.Combine(evDir, name + ".png"), tx.EncodeToPNG());
            Line($"캡처 {name} {tx.width}x{tx.height} 안내='{dir.Prompt}' 자막='{dir.Subtitle}'");
            Destroy(tx);
        }
        else { Line($"캡처 {name} 실패(UI) — 카메라만"); yield return Shot(name); }
    }
}
