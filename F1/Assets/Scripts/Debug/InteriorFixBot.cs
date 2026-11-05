using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 과제 20261105-0453 검사 봇(건물 안 3인칭 고장 다섯 개). 순간이동 + Sim 입력.
/// ① 건물마다 바깥 벽·문·셔터 1 m 밖에 세워 1인칭인지(RoomDiorama.Active 꺼짐).
/// ③ 정비소 셔터로 걸어 들어가고(W) 나오기(S, 화면 아래 = 문 쪽) — 프레임마다 w·카메라·플레이어를 blend.txt 에. 붙듦 끔(고치기 전) / 켬 두 번.
/// ② 2층 방에서 달력 열고 닫은 뒤 몸 렌더러가 ShadowCastingMode.On 인지. ④ 보관소에서 상자 들고 hold.png. ⑤ 고친 시점 6장. ⑥ 결 A/B 3곳.
/// 실행: exec 'return InteriorFixBot.Begin("D:/Github/F1Game/docs/qa/evidence/interior-fix");' → Status 가 "done …" 이면 끝.
/// </summary>
public class InteriorFixBot : MonoBehaviour
{
    public static string Status = "idle";
    static string dir;
    OnFootDirector d; RoomDiorama dio; OnFootPlayer pl; Camera cam;
    readonly List<string> log = new List<string>();
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static bool doorOnly, fitOnly, leftOnly;
    /// <summary>D-307 ⑧: 방마다 들어가 fit-&lt;이름&gt;.png + 거리·캡슐 화면 높이 %·바닥 모서리 화면 안(실제 카메라로 투영)을 bot-fit.txt 에.</summary>
    public static string BeginFit(string outDir) { var s = Begin(outDir); fitOnly = true; return s; }
    /// <summary>과제 20261105-0535: garage·house2f-hall·postoffice-2f·hold 4장(결 B) + bot-left.txt.</summary>
    public static string BeginLeft(string outDir) { var s = Begin(outDir); leftOnly = true; return s; }
    /// <summary>과제 20261105-0617: 든 상자 3장(dio-front · dio-back · fp-outside) + bot-held.txt.</summary>
    public static string BeginHeld(string outDir) { var s = Begin(outDir); heldOnly = true; return s; }
    static bool heldOnly, carryOnly;
    /// <summary>과제 20261105-0630: 안은 상자 carry-{a,b}-{front,side,back} 6장 + 걷기 떨림 · 결 B/C 3곳 + noise-probe.txt(손잡이별 잔점 %).</summary>
    public static string BeginCarry(string outDir) { var s = Begin(outDir); carryOnly = true; return s; }
    static bool townOnly; static string hillOnly;
    /// <summary>과제 20261105-0942(D-308 마을 그림 실내): {depot,postoffice,room2f}-{day,dusk} · depot-grain-{0,1,2} · depot-out-{black,haze} · town-hill-after + probe.txt.
    /// hill = "before"/"after" 면 언덕 꼭대기 town-hill-{hill} 한 장 + probe-{hill}.txt 만(고정 카메라 자세 — 셰이더 바꾸기 전·후로 한 번씩 돌린다).</summary>
    public static string BeginTown(string outDir, string hill = null) { var s = Begin(outDir); townOnly = true; hillOnly = hill; return s; }
    public static string Begin(string outDir, bool onlyDoor = false)
    {
        doorOnly = onlyDoor; fitOnly = false; leftOnly = false; heldOnly = false; carryOnly = false; townOnly = false;
        var dr = FindFirstObjectByType<OnFootDirector>();
        if (dr == null) return "no dir";
        dir = outDir; Directory.CreateDirectory(dir); Status = "run";
        var c = new GameObject("InteriorFixBot").AddComponent<InteriorFixBot>(); c.d = dr;
        return "OK";
    }

    IEnumerator Start()
    {
        Time.timeScale = 1f;
        cam = Camera.main; dio = cam.GetComponent<RoomDiorama>(); pl = d.Player;
        while (d.State != OnFootDirector.Mode.OnFoot) yield return null;
        yield return Frames(10);
        var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
        if (fitOnly)
        {
            yield return FitViews(list);
            File.WriteAllLines(Path.Combine(dir, "bot-fit.txt"), log);
            Status = "done " + log.Count; Destroy(gameObject); yield break;
        }
        if (carryOnly)
        {
            yield return CarryShots(list);
            File.WriteAllLines(Path.Combine(dir, "bot-carry.txt"), log);
            var probe = new List<string>();
            yield return NoiseProbe(list, probe);
            File.WriteAllLines(Path.Combine(dir, "noise-probe.txt"), probe);
            Status = "done " + log.Count + "/" + probe.Count; Destroy(gameObject); yield break;
        }
        if (townOnly)
        {
            var probe = new List<string>();
            yield return TownShots(list, probe);
            File.WriteAllLines(Path.Combine(dir, hillOnly != null ? "probe-" + hillOnly + ".txt" : "probe.txt"), probe);
            Status = "done " + probe.Count; Destroy(gameObject); yield break;
        }
        if (heldOnly)
        {
            yield return HeldShots(list);
            File.WriteAllLines(Path.Combine(dir, "bot-held.txt"), log);
            Status = "done " + log.Count; Destroy(gameObject); yield break;
        }
        if (leftOnly)
        {
            yield return Left(list);
            File.WriteAllLines(Path.Combine(dir, "bot-left.txt"), log);
            Status = "done " + log.Count; Destroy(gameObject); yield break;
        }
        if (!doorOnly) yield return OutsideCheck(list);
        yield return DoorWalk(list, true);
        yield return DoorWalk(list, false);
        if (!doorOnly)
        {
            yield return CalendarCheck(list);
            yield return Views(list);
            yield return Grain(list);
            yield return HoldCheck(list);
        }
        File.WriteAllLines(Path.Combine(dir, doorOnly ? "bot-door.txt" : "bot.txt"), log);
        Status = "done " + log.Count;
        Destroy(gameObject);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    static IEnumerator Secs(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }
    static string V(Vector3 p) => string.Format(Inv, "({0:0.00},{1:0.00},{2:0.00})", p.x, p.y, p.z);

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
        yield return Frames(3);
    }

    void Teleport(Vector3 feet, float yaw)
    {
        if (Physics.Raycast(feet + Vector3.up * 1.5f, Vector3.down, out var h, 4f, ~0, QueryTriggerInteraction.Ignore)) feet.y = h.point.y + 0.05f;
        d.DebugTeleport(feet, yaw, null, 0f);
    }

    static bool WallHit(Transform bld, Vector3 o, Vector3 dd, out RaycastHit best)
    {
        best = default; bool got = false;
        foreach (var h in Physics.RaycastAll(o, dd, 40f, ~0, QueryTriggerInteraction.Ignore))
            if (h.transform.IsChildOf(bld) && A7ShadeGlobals.IsWall(h.collider) && (!got || h.distance < best.distance)) { best = h; got = true; }
        return got;
    }

    // ① 바깥 벽·문 1 m 밖(더 바깥에 그 건물 벽이 없는 쪽)에 세운다 — 발 = 그 점의 땅
    IEnumerator OutsideCheck(List<A7ShadeGlobals.Interior> list)
    {
        var pts = new List<(string, Vector3, Vector3)>();
        bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
        var seen = new HashSet<Transform>();
        foreach (var it in list)
        {
            Vector3 o = it.box.inverse.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up;
            foreach (var a in new[] { it.building.right, -it.building.right, it.building.forward, -it.building.forward })
                if (WallHit(it.building, o, a, out var h) && !WallHit(it.building, h.point + a, a, out _)) pts.Add((it.name + " 벽", h.point + a - Vector3.up, a));
            if (!seen.Add(it.building)) continue;
            foreach (var c in it.building.GetComponentsInChildren<Collider>())
            {
                if (!c.enabled || c.isTrigger || !(c.name.StartsWith("DOOR") || c.name.StartsWith("SHUTTER"))) continue;
                var b = c.bounds;
                Vector3 r = it.building.right, f = it.building.forward;
                Vector3 n = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(r.x), Mathf.Abs(r.y), Mathf.Abs(r.z)))) < Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.x), Mathf.Abs(f.y), Mathf.Abs(f.z)))) ? r : f;
                var p0 = new Vector3(b.center.x, b.min.y, b.center.z);
                foreach (var s in new[] { n, -n })
                    if (!WallHit(it.building, p0 + s + Vector3.up, s, out _)) pts.Add((it.building.name + "/" + c.name, p0 + s, s));
            }
        }
        Physics.queriesHitBackfaces = bf;
        int fail = 0;
        foreach (var (name, feet, a) in pts)
        {
            Teleport(feet, Quaternion.LookRotation(-a).eulerAngles.y);
            yield return Secs(0.6f);
            bool bad = RoomDiorama.Active || dio.Weight > 0f;
            if (bad) fail++;
            log.Add(string.Format(Inv, "[밖1m] {0} 발={1} → active={2} w={3:0.00} room={4} {5}", name, V(pl.transform.position), RoomDiorama.Active, dio.Weight, dio.RoomNow, bad ? "실패" : "1인칭"));
        }
        log.Add($"[밖1m] 합계 {pts.Count}곳 실패 {fail}");
    }

    // ③ 집 현관(house_home DOOR_01 — 정비소 셔터는 닫힌 콜라이더라 못 지나감): 문 열고 밖 3 m 에서 안으로 W 2.5 초 → 멈춤 1 초
    // → S(화면 아래 = 문 쪽 카메라) 2.5 초. 프레임마다 기록
    IEnumerator DoorWalk(List<A7ShadeGlobals.Interior> list, bool noLatch)
    {
        var it = list.Find(i => i.name.StartsWith("house_home/1F"));
        Collider sh = null;
        foreach (var c in it.building.GetComponentsInChildren<Collider>()) if (c.name == "DOOR_01") sh = c;
        var hinge = sh.GetComponentInParent<DoorHinge>();
        // 열린 채면 문짝 bounds·CenterWorld 가 열린 문짝 자리라 출발점이 문짝 앞이 된다 → 닫고 잰다
        if (hinge != null && hinge.IsOpen) { hinge.Toggle(sh.bounds.center); yield return Secs(0.6f); }
        Physics.SyncTransforms();
        var b = sh.bounds;
        Vector3 cIn = it.box.inverse.MultiplyPoint3x4(Vector3.zero);
        Vector3 n = b.center - cIn; n.y = 0f;
        Vector3 axis = Mathf.Abs(Vector3.Dot(n, it.building.right)) > Mathf.Abs(Vector3.Dot(n, it.building.forward)) ? it.building.right * Mathf.Sign(Vector3.Dot(n, it.building.right)) : it.building.forward * Mathf.Sign(Vector3.Dot(n, it.building.forward));
        Vector3 doorC = hinge != null ? hinge.CenterWorld : b.center;
        OnFootPlayer.NoInputLatch = noLatch;
        // 현관 문턱(HOUSE_shell)이 땅보다 0.7~0.9 m 높아(stepOffset 0.3) 땅에서는 못 올라선다 → 문턱 위 문 밖 0.7 m 에서 출발
        Vector3 st = doorC + axis * 0.7f;
        if (Physics.Raycast(st + Vector3.up * 2f, Vector3.down, out var fl, 5f, ~0, QueryTriggerInteraction.Ignore)) st.y = fl.point.y; else st.y = b.min.y;
        Teleport(st, Quaternion.LookRotation(-axis).eulerAngles.y);
        yield return Secs(1f);
        if (hinge != null && !hinge.IsOpen) { hinge.Toggle(doorC + axis * 3f); yield return Secs(0.6f); }
        var bl = new List<string> { $"# 집 현관 들어가기·나오기 붙듦={(noLatch ? "끔(고치기 전)" : "켬")} 문열림={(hinge != null && hinge.IsOpen)} — t w active fixed cam player room" };
        float t0 = Time.time;
        int toggles = 0, wRev = 0; bool lastA = RoomDiorama.Active; float lastW = dio.Weight, lastDw = 0f;
        pl.simActive = true; pl.simRun = false;
        foreach (var (phase, mv, sec) in new[] { ("in", new Vector2(0f, 1f), 2.5f), ("stop", Vector2.zero, 1f), ("out", new Vector2(0f, -1f), 2.5f) })
        {
            pl.simMove = mv;
            float t = 0f;
            while (t < sec)
            {
                yield return new WaitForEndOfFrame();
                t += Time.deltaTime;
                float w = dio.Weight, dw = w - lastW;
                if (Mathf.Abs(dw) > 1e-4f) { if (lastDw != 0f && Mathf.Sign(dw) != Mathf.Sign(lastDw)) wRev++; lastDw = dw; }
                lastW = w;
                if (RoomDiorama.Active != lastA) { toggles++; lastA = RoomDiorama.Active; }
                bl.Add(string.Format(Inv, "{0} {1:0.000} w={2:0.000} active={3} fixed={4} cam={5} player={6} room={7}", phase, Time.time - t0, w, RoomDiorama.Active ? 1 : 0, RoomDiorama.Fixed ? 1 : 0, V(cam.transform.position), V(pl.transform.position), dio.RoomNow));
            }
        }
        pl.simMove = Vector2.zero; pl.simActive = false; OnFootPlayer.NoInputLatch = false;
        string sum = $"[문 걷기 붙듦={(noLatch ? "끔" : "켬")}] 디오라마 켜짐/꺼짐 바뀜 {toggles}번 · w 방향 뒤집힘 {wRev}번 (한 번 들어갔다 나오면 2번·1번)";
        bl.Insert(1, "# " + sum);
        File.AppendAllLines(Path.Combine(dir, "blend.txt"), bl);
        log.Add(sum);
        yield return Secs(0.5f);
    }

    // ② 2층 방: 달력 열기 → 닫기 → 몸이 보이나
    IEnumerator CalendarCheck(List<A7ShadeGlobals.Interior> list)
    {
        var it = list.Find(i => i.room != null);
        Teleport(it.box.inverse.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)), 270f);
        yield return Secs(1f);
        string before = BodyModes();
        SignalBus.Raise(Signal.CalendarOpened);
        yield return Secs(1.5f);
        string open = $"state={d.State}";
        d.SimCalendarClose();
        float w = 0f;
        while (d.State != OnFootDirector.Mode.OnFoot && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }
        yield return Secs(0.5f);
        string after = BodyModes();
        bool ok = RoomDiorama.Active && !after.Contains("ShadowsOnly");
        log.Add($"[달력] 전 {before} · 열림 {open} · 닫은 뒤 state={d.State} active={RoomDiorama.Active} {after} → {(ok ? "몸 보임" : "몸 안 보임(실패)")}");
        yield return Shot("calendar-after");
    }

    string BodyModes()
    {
        var m = new Dictionary<ShadowCastingMode, int>();
        foreach (var r in pl.GetComponentsInChildren<Renderer>(true)) { m.TryGetValue(r.shadowCastingMode, out var k); m[r.shadowCastingMode] = k + 1; }
        var s = ""; foreach (var kv in m) s += kv.Key + "×" + kv.Value + " ";
        return "몸[" + s.Trim() + "]";
    }

    // int_go.cs 와 같은 자리: a = 문 쪽 안, b = 반대쪽 구석
    IEnumerator GoIn(List<A7ShadeGlobals.Interior> list, A7ShadeGlobals.Interior it, bool a)
    {
        var doors = FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
        float yaw = RoomDiorama.FixedYaw(it, list, doors, null);
        Vector3 fb = it.box.MultiplyVector(Quaternion.Euler(0f, yaw, 0f) * Vector3.forward); fb.y = 0f; fb.Normalize();
        Vector3 lat = new Vector3(-fb.z, 0f, fb.x);
        Vector3 q = a ? -fb * 0.6f : fb * 0.6f + lat * 0.6f; q.y = -1f;
        d.DebugTeleport(it.box.inverse.MultiplyPoint3x4(q) + Vector3.up * 0.1f, yaw, null, 0f);
        yield return Secs(1.2f);
    }

    // ⑤ 고친 시점 6장
    IEnumerator Views(List<A7ShadeGlobals.Interior> list)
    {
        foreach (var (key, a, file) in new[] { ("D198_garage", true, "fixed-garage-a"), ("D198_garage", false, "fixed-garage-b"),
                                                ("D198_postoffice/1F", true, "fixed-postoffice-a"), ("D198_postoffice/1F", false, "fixed-postoffice-b"),
                                                ("house_home/2F#2", true, "fixed-house2f2"), ("house_home/2F#3", true, "fixed-house2f3") })
        {
            var it = list.Find(i => i.name.StartsWith(key));
            if (it == null) { log.Add("[시점] " + key + " 없음"); continue; }
            yield return GoIn(list, it, a);
            log.Add($"[시점] {file} = {it.name} half={V(it.half)} active={RoomDiorama.Active} room={dio.RoomNow} cam={V(cam.transform.position)}");
            yield return Shot(file);
        }
    }

    // D-307 ⑧ 방이 다 보이게 — 방마다(문 쪽 안에 서서) 실제 카메라로 바닥 네 모서리·벽 위 네 모서리·사람 캡슐을 투영해 잰다
    IEnumerator FitViews(List<A7ShadeGlobals.Interior> list)
    {
        var doors = FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
        var cc = pl.GetComponent<CharacterController>();
        float capH = cc != null ? cc.height : dio.capsuleHeight;
        log.Add($"[맞춤] fov={cam.fieldOfView:F1} aspect={cam.aspect:F3} pitch={dio.pitch} margin={dio.fitMargin} minCapsule={dio.minCapsule} grainB={dio.grainB} 캡슐 높이 {capH:F2}");
        foreach (var stood in list)
        {
            yield return GoIn(list, stood, true);
            yield return Secs(0.6f);   // 0.4초 섞임 + SmoothDamp
            // 겹친 상자(작은 쪽이 먼저)·떨어짐이 있으니 재는 건 지금 화면에 보이는 방
            var it = list.Find(i => i.name == dio.RoomNow) ?? stood;
            Vector3 feet = it.box.MultiplyPoint3x4(pl.transform.position);
            log.Add(string.Format(Inv, "[맞춤] 세운 방 {0} → 보이는 방 {1} 발(상자 좌표) {2}", stood.name, dio.RoomNow, V(feet)));
            float yaw = RoomDiorama.FixedYaw(it, list, doors, null);
            float ceilY = RoomDiorama.CeilOf(it.box, dio.ceilCut, dio.ceilMetres);
            RoomDiorama.Fit(it.box.inverse, yaw, dio.pitch, cam.fieldOfView, cam.aspect, dio.fitMargin, ceilY, dio.capsuleHeight, out _, out float dist, out float capF);
            bool fit = capF >= dio.minCapsule;
            // 실제 카메라 투영(맞춤 식과 따로)
            float minX = 9f, maxX = -9f, minY = 9f, maxY = -9f; int outFloor = 0;
            for (int k = 0; k < 8; k++)
            {
                Vector3 q = new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : ceilY, (k & 4) == 0 ? -1f : 1f);
                Vector3 v = cam.WorldToViewportPoint(it.box.inverse.MultiplyPoint3x4(q));
                minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x); minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
                if ((k & 2) == 0 && (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f)) outFloor++;
            }
            Vector3 f0 = cam.WorldToViewportPoint(pl.transform.position), f1 = cam.WorldToViewportPoint(pl.transform.position + Vector3.up * capH);
            float camDist = Vector3.Distance(cam.transform.position, it.box.inverse.MultiplyPoint3x4(Vector3.zero));
            string file = "fit-" + stood.name.Replace('/', '-').Replace('#', '_');
            log.Add(string.Format(Inv, "[맞춤] {0} half={1} {2} 맞춤거리 {3:F1}m 캡슐(바닥 가운데) {4:P1} | 실제: active={5} 카메라-상자가운데 {6:F1}m 플레이어 캡슐 {7:P1} 상자 8점 화면 x {8:F2}~{9:F2} y {10:F2}~{11:F2} 바닥 모서리 화면 밖 {12} → {13}.png",
                it.name, V(it.half), fit ? "고정" : "평행이동", dist, capF, RoomDiorama.Active, camDist, Mathf.Abs(f1.y - f0.y), minX, maxX, minY, maxY, outFloor, file));
            yield return Shot(file);
        }
    }

    // ⑥ 결 A/B — 같은 자리에서 grainB 만 바꿔 두 장
    IEnumerator Grain(List<A7ShadeGlobals.Interior> list)
    {
        bool g0 = dio.grainB;
        foreach (var (key, file) in new[] { ("house_home/ROOM_2F", "grain-room2f"), ("house_home/1F", "grain-house1f"), ("D198_depot", "grain-depot") })
        {
            var it = list.Find(i => i.name.StartsWith(key));
            if (it == null) { log.Add("[결] " + key + " 없음"); continue; }
            yield return GoIn(list, it, false);
            dio.grainB = false; yield return Frames(3); yield return Shot(file + "-A");
            dio.grainB = true; yield return Frames(3); yield return Shot(file + "-B");
            log.Add($"[결] {file} = {it.name} active={RoomDiorama.Active}");
        }
        dio.grainB = g0;   // 과제 20261105-0535 ④: 전엔 false 로 끝내 뒤 hold.png 가 결 A 였다
    }

    // 과제 20261105-0535: 남은 넷 — 정비소 바닥 판 · 2층 복도 앞벽 · 우체국 2층 · 든 상자(결 B, 맞춘 카메라)
    IEnumerator Left(List<A7ShadeGlobals.Interior> list)
    {
        bool g0 = dio.grainB; dio.grainB = true;
        foreach (var it in list)
            if (it.name.StartsWith("house_home") || it.name.StartsWith("D198_postoffice"))
                log.Add($"[상자] {it.name} 가운데={V(it.box.inverse.MultiplyPoint3x4(Vector3.zero))} half={V(it.half)} 점={it.samples} 벽 안쪽 면={A7ShadeGlobals.WallFaces(it)}");
        foreach (var (key, file) in new[] { ("D198_garage", "garage"), ("house_home/2F#2", "house2f-hall"), ("D198_postoffice/2F", "postoffice-2f") })
        {
            var it = list.Find(i => i.name.StartsWith(key));
            if (it == null) { log.Add("[남은] " + key + " 없음"); continue; }
            yield return GoIn(list, it, true);
            yield return Secs(0.6f);
            var now = list.Find(i => i.name == dio.RoomNow) ?? it;
            Vector3 cp = cam.transform.position, chest = pl.transform.position + Vector3.up * 1.1f;
            float ceilY = RoomDiorama.CeilOf(now.box, dio.ceilCut, dio.ceilMetres);
            float cut = RoomDiorama.CutLine(now.box, cp, A7ShadeGlobals.WallFaces(now), dio.wallCut);
            string blk = A7ShadeGlobals.CapsuleBlocker(now, cp, chest, cut, ceilY);
            string under = Physics.Raycast(pl.transform.position + Vector3.up * 0.5f, Vector3.down, out var g, 3f, ~0, QueryTriggerInteraction.Ignore) ? g.collider.name + " y=" + g.point.y.ToString("F2", Inv) : "없음";
            log.Add(string.Format(Inv, "[남은] {0} 세운 방 {1} → 보이는 방 {2} active={3} 발={4} 발(상자)={5} 발 밑={6} 컷선={7:F3} 캡슐 앞 벽={8} 회색 판={9}",
                file, it.name, dio.RoomNow, RoomDiorama.Active, V(pl.transform.position), V(now.box.MultiplyPoint3x4(pl.transform.position)), under, cut, blk ?? "없음",
                GameObject.Find("RepairSpot_Floor (greybox)") != null ? "있음" : "없음"));
            yield return Shot(file);
        }
        yield return HoldCheck(list);
        dio.grainB = g0;
    }

    // 과제 20261105-0617: 보관소에서 상자 든 채 — 카메라 쪽을 봄 · 등짐 · 밖에서 1인칭(든 상자 위치는 안 바뀜)
    IEnumerator HeldShots(List<A7ShadeGlobals.Interior> list)
    {
        bool g0 = dio.grainB; dio.grainB = true;
        var it = list.Find(i => i.name.StartsWith("D198_depot"));
        float yaw = RoomDiorama.FixedYaw(it, list, FindObjectsByType<DoorHinge>(FindObjectsSortMode.None), null);
        Vector3 floor = it.box.inverse.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up * 0.1f;
        d.DebugTeleport(floor, yaw + 180f, null, 0f);
        yield return Secs(1.8f);
        SignalBus.Raise(Signal.BoxTaken, "봇");
        yield return Secs(1f);
        LogHeld("dio-front");
        yield return Shot("dio-front");
        d.DebugTeleport(floor, yaw, null, 0f);
        yield return Secs(1.2f);
        LogHeld("dio-back");
        yield return Shot("dio-back");
        d.DebugTeleport(new Vector3(270.5f, 30.9f, -34.0f), 0f, null, 0f);
        yield return Secs(1.5f);
        LogHeld("fp-outside");
        yield return Shot("fp-outside");
        dio.grainB = g0;
    }

    // 과제 20261105-0630 ①: 보관소 바닥 가운데에서 상자 든 채 — 카메라 쪽(front)·옆(side)·등짐(back) × 팔 없음(a)/있음(b). 그다음 걷기 2초 떨림
    IEnumerator CarryShots(List<A7ShadeGlobals.Interior> list)
    {
        bool g0 = dio.grainB, c0 = dio.grainC, a0 = dio.heldArms; dio.grainB = true; dio.grainC = false;
        var it = list.Find(i => i.name.StartsWith("D198_depot"));
        float yaw = RoomDiorama.FixedYaw(it, list, FindObjectsByType<DoorHinge>(FindObjectsSortMode.None), null);
        Vector3 floor = it.box.inverse.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up * 0.1f;
        d.DebugTeleport(floor, yaw + 180f, null, 0f);
        yield return Secs(1.8f);
        SignalBus.Raise(Signal.BoxTaken, "봇");
        yield return Secs(1f);
        var cc = pl.GetComponent<CharacterController>();
        var bb = new Bounds(pl.transform.position, Vector3.zero); bool got = false;
        foreach (var r in pl.GetComponentsInChildren<Renderer>()) { if (!got) { bb = r.bounds; got = true; } else bb.Encapsulate(r.bounds); }
        log.Add(string.Format(Inv, "[몸] cc 반지름 {0:F3} 키 {1:F2} · 몸 렌더러 bounds 가로 반 {2:F3}/{3:F3} 높이 {4:F2}", cc != null ? cc.radius : -1f, cc != null ? cc.height : -1f, bb.extents.x, bb.extents.z, bb.size.y));
        foreach (var arms in new[] { false, true })
        {
            dio.heldArms = arms;
            foreach (var (tag, dy) in new[] { ("front", 180f), ("side", 90f), ("back", 0f) })
            {
                d.DebugTeleport(floor, yaw + dy, null, 0f);
                yield return Secs(1.2f);
                string f = $"carry-{(arms ? "b" : "a")}-{tag}";
                LogHeld(f);
                LogHug(f, cc);
                yield return Shot(f);
            }
        }
        // 걷기: 화면 오른쪽으로 2초(몸이 옆으로 돈다) — 몸 기준 상자·팔 자리가 프레임마다 얼마나 흔들리나
        d.DebugTeleport(floor - it.box.inverse.MultiplyVector(Vector3.right).normalized * 1.5f, yaw + 90f, null, 0f);
        yield return Secs(1f);
        var hb = cam.transform.Find("HeldBox");
        pl.simActive = true; pl.simRun = false; pl.simMove = new Vector2(1f, 0f);
        float t = 0f, maxDev = 0f, maxStep = 0f; int n = 0; Vector3 lastL = Vector3.zero; bool first = true;
        Vector3 p0 = pl.transform.position;
        while (t < 2f)
        {
            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
            if (hb == null || !RoomDiorama.Active) continue;
            float rad = cc != null ? cc.radius : 0.3f;
            Vector3 want = RoomDiorama.HeldPos(pl.transform.position, pl.transform.rotation, rad, RoomDiorama.HeldBoxSize);
            Vector3 l = Quaternion.Inverse(pl.transform.rotation) * (hb.position - pl.transform.position);
            maxDev = Mathf.Max(maxDev, Vector3.Distance(hb.position, want));
            if (!first) maxStep = Mathf.Max(maxStep, Vector3.Distance(l, lastL));
            lastL = l; first = false; n++;
        }
        pl.simMove = Vector2.zero; pl.simActive = false;
        log.Add(string.Format(Inv, "[걷기] 2초 {0}프레임 · 걸은 거리 {1:F2} m · 상자 ↔ 식 자리 최대 {2:F4} m · 몸 기준 상자 자리 프레임 사이 최대 변화 {3:F4} m → {4}",
            n, Vector3.Distance(p0, pl.transform.position), maxDev, maxStep, maxDev < 0.005f && maxStep < 0.005f ? "몸과 같이 움직임(떨림 없음)" : "떨림"));
        dio.grainB = g0; dio.grainC = c0; dio.heldArms = a0;
    }

    void LogHug(string tag, CharacterController cc)
    {
        var hb = cam.transform.Find("HeldBox");
        if (hb == null) return;
        Vector3 l = Quaternion.Inverse(pl.transform.rotation) * (hb.position - pl.transform.position);
        float rad = cc != null ? cc.radius : 0.3f;
        log.Add(string.Format(Inv, "[안기] {0} 몸 기준 상자 가운데 (옆 {1:F3}, 높이 {2:F3}, 앞 {3:F3}) · 뒷면 ↔ 캡슐 앞면 틈 {4:F3} m · 팔 보임={5} · 상자 회전-몸 회전 {6:F1}°",
            tag, l.x, l.y, l.z, l.z - RoomDiorama.HeldBoxSize.z * 0.5f - rad, dio.ArmsShown, Quaternion.Angle(hb.rotation, pl.transform.rotation)));
    }

    // 과제 20261105-0630 ②: 같은 자리·같은 카메라에서 손잡이만 바꿔 저해상 화면(cam.targetTexture) 잔점 비율을 잰다
    //  잔점 = 이웃 4픽셀 모두보다 밝기가 T 이상 튀거나(밝은 점) 꺼진(어두운 점) 픽셀 · 경계 = 이웃 하나라도 T 이상 차. 검정 배경(방 밖)은 뺀다. T = 0.06(감마 밝기)
    IEnumerator NoiseProbe(List<A7ShadeGlobals.Interior> list, List<string> o)
    {
        bool g0 = dio.grainB, c0 = dio.grainC;
        var knobs = new (string, Vector4, Vector4)[]
        {
            ("B 지금(디더 0.04, 거름 1 — 디오라마에선 거름 안 읽힘)", new Vector4(1f, 0f, 0.04f, 0f), Vector4.zero),
            ("(1) 디더 0", new Vector4(0f, 0f, 0f, 0f), Vector4.zero),
            ("    A 옛 값(디더 0.15)", new Vector4(0f, 0f, 0.15f, 0f), Vector4.zero),
            ("(2) 텍스처 거름 0(바이리니어 1탭)", new Vector4(0f, 0f, 0.04f, 0f), new Vector4(1f, 0f, 0f, 0f)),
            ("(2) 텍스처 거름 1텍셀", new Vector4(0f, 0f, 0.04f, 0f), new Vector4(1f, 1f, 0f, 0f)),
            ("(2) 텍스처 거름 3텍셀", new Vector4(0f, 0f, 0.04f, 0f), new Vector4(1f, 3f, 0f, 0f)),
            ("(3) 밉 바이어스 +2(재질 텍스처 mips=1)", new Vector4(0f, 0f, 0.04f, 0f), new Vector4(2f, 0f, 0f, 0f)),
            ("(3') 밉 대신 화면 픽셀 폭 거름 ×1", new Vector4(0f, 0f, 0.04f, 0f), new Vector4(1f, 0f, 1f, 0f)),
            ("(4) 단 자를 때 결 빼기 1(거름 ×1 위)", new Vector4(0f, 0f, 0.04f, 1f), new Vector4(1f, 0f, 1f, 0f)),
            ("(5) 면 결 _SN 끔(점 그대로)", new Vector4(0f, 0f, 0.04f, 0f), new Vector4(0f, 0f, 0f, 1f)),
            ("C (필드값)", Vector4.zero, Vector4.zero),
        };
        o.Add("# 화면 잔점 비율 — 저해상 화면 " + (cam.targetTexture != null ? cam.targetTexture.width + "x" + cam.targetTexture.height : "?") + " · 잔점/경계 % = 검정(방 밖) 뺀 픽셀 중 · T=0.06");
        o.Add(string.Format(Inv, "# C 필드: 거름 최소 {0} 텍셀 · 화면 픽셀 폭 × {1} · 디더 {2} · 결 빼기 {3} · 면 결 빼기 {4}", dio.grainCFilterTexels, dio.grainCFootprint, dio.grainCDitherWidth, dio.grainCGrainCut, dio.grainCSurfaceCut));
        foreach (var (key, file) in new[] { ("D198_depot", "depot"), ("D198_postoffice/1F", "postoffice"), ("house_home/ROOM_2F", "room2f") })
        {
            var it = list.Find(i => i.name.StartsWith(key));
            if (it == null) { o.Add("[결] " + key + " 없음"); continue; }
            yield return GoIn(list, it, false);
            yield return Secs(0.8f);
            o.Add($"## {file} = {it.name} 보이는 방 {dio.RoomNow} active={RoomDiorama.Active} 카메라 {V(cam.transform.position)} → 방 가운데 {Vector3.Distance(cam.transform.position, it.box.inverse.MultiplyPoint3x4(Vector3.zero)):F1} m");
            foreach (var (name, q, tx) in knobs)
            {
                bool isC = name.StartsWith("C ");
                dio.probe = !isC; dio.probeInQ = q; dio.probeTex = tx; dio.grainC = isC; dio.grainB = true;
                yield return Frames(4);
                yield return new WaitForEndOfFrame();
                Measure(out float spike, out float edge, out int px);
                o.Add(string.Format(Inv, "{0,-44} 잔점 {1,6:F2}%  경계 {2,6:F2}%  (픽셀 {3}, RT {4}칸 = 1 픽셀)", name, spike, edge, px, probeStride));
            }
            dio.probe = false; dio.grainC = false; dio.grainB = true; yield return Frames(4); yield return Shot($"grain-{file}-B");
            dio.grainC = true; yield return Frames(4); yield return Shot($"grain-{file}-C");
        }
        dio.probe = false; dio.grainB = g0; dio.grainC = c0;
    }

    // 과제 20261105-0942: 마을 그림 실내 — 같은 방을 낮·노을로, 결 0/기본/2배, 방 밖 검정/안개색, 언덕 꼭대기(바깥 기준) + 잔점 %·면 한 장 밝기 퍼짐
    IEnumerator TownShots(List<A7ShadeGlobals.Interior> list, List<string> o)
    {
        var sl = SunsetLook.Find(); faceRooms = list;
        bool sl0 = sl != null && sl.IsOn, haze0 = dio.outsideHaze; float g0 = dio.flatGrain;
        o.Add("# 잔점 % — 과제 32 noise-probe 와 같은 식(저해상 화면, 이웃 4픽셀 모두보다 T=0.06 이상 튐, 검정(방 밖) 뺌)");
        o.Add("# 면 퍼짐 = 화면 가운데 레이로 맞은 면(같은 콜라이더·같은 법선) 중 가장 큰 면 한 장의 밝기(감마) 표준편차/평균");
        o.Add(string.Format(Inv, "# townLook={0} flatGrain={1} flatGrainSize={2} faceShade={3} SunsetLook 시작={4}", dio.townLook, dio.flatGrain, dio.flatGrainSize, dio.flatFaceShade, sl0));
        if (sl != null) sl.SetOn(false);
        yield return Frames(6);
        // 바깥: 언덕 꼭대기(낮)
        int hill = -1; for (int k = 0; k < DebugPlaces.All.Length; k++) if (DebugPlaces.All[k].name == "언덕 꼭대기") hill = k;
        if (hill >= 0)
        {
            DebugWindow.GoPlace(hill);
            yield return Secs(2.5f);
            // 따라가기 카메라는 차 흔들림·지연으로 판마다 자세가 달라진다 → 장소 기준 고정 자세로 놓고 찍는다(전·후 같은 그림)
            var fc = cam.GetComponent<FollowCamera>(); var oc = cam.GetComponent<OverviewCamera>();
            bool fc0 = fc != null && fc.enabled, oc0 = oc != null && oc.enabled;
            if (fc != null) fc.enabled = false; if (oc != null) oc.enabled = false;
            var pc = DebugPlaces.All[hill]; var fwd = Quaternion.Euler(0f, pc.yaw, 0f) * Vector3.forward;
            cam.transform.SetPositionAndRotation(pc.feet + Vector3.up * 9f - fwd * 8f, Quaternion.LookRotation(pc.feet + fwd * 24f - (pc.feet + Vector3.up * 9f - fwd * 8f)));
            yield return Frames(4);
            yield return new WaitForEndOfFrame();
            ProbeLine(o, string.Format(Inv, "town-hill(언덕 꼭대기, 낮, 고정 카메라 {0})", cam.transform.position), false);
            yield return Shot(hillOnly != null ? "town-hill-" + hillOnly : "town-hill-after");
            if (fc != null) fc.enabled = fc0; if (oc != null) oc.enabled = oc0;
        }
        else o.Add("[언덕] 장소 표에 「언덕 꼭대기」 없음");
        // 면 퍼짐을 실내와 비슷한 거리(수 m)에서도 — 큰길(길가 마을 집)
        int road = -1; for (int k = 0; k < DebugPlaces.All.Length; k++) if (DebugPlaces.All[k].name == "큰길") road = k;
        if (road >= 0 && hillOnly == null)
        {
            DebugWindow.GoPlace(road);
            yield return Secs(2.5f);
            yield return new WaitForEndOfFrame();
            ProbeLine(o, "town-road(큰길, 낮)", false);
        }
        if (hillOnly != null) { if (sl != null) sl.SetOn(sl0); yield break; }
        foreach (var (key, file) in new[] { ("D198_depot", "depot"), ("D198_postoffice/1F", "postoffice"), ("house_home/ROOM_2F", "room2f") })
        {
            var it = list.Find(i => i.name.StartsWith(key));
            if (it == null) { o.Add("[방] " + key + " 없음"); continue; }
            yield return GoIn(list, it, false);
            yield return Secs(0.8f);
            o.Add($"## {file} = {it.name} 보이는 방 {dio.RoomNow} active={RoomDiorama.Active} 카메라→방 가운데 {Vector3.Distance(cam.transform.position, it.box.inverse.MultiplyPoint3x4(Vector3.zero)):F1} m");
            foreach (bool dusk in new[] { false, true })
            {
                if (sl != null) sl.SetOn(dusk);
                yield return Frames(6);
                yield return new WaitForEndOfFrame();
                ProbeLine(o, $"{file}-{(dusk ? "dusk" : "day")}", true);
                yield return Shot($"{file}-{(dusk ? "dusk" : "day")}");
            }
            if (sl != null) sl.SetOn(false);
            if (file != "depot") continue;
            for (int g = 0; g <= 2; g++)
            {
                dio.flatGrain = g0 * g;
                yield return Frames(4);
                yield return new WaitForEndOfFrame();
                ProbeLine(o, string.Format(Inv, "depot-grain-{0} (flatGrain {1:0.00})", g, dio.flatGrain), true);
                yield return Shot($"depot-grain-{g}");
            }
            dio.flatGrain = g0;
            foreach (bool hz in new[] { false, true })
            {
                dio.outsideHaze = hz;
                yield return Frames(4);
                o.Add(string.Format(Inv, "depot-out-{0} 배경 {1}", hz ? "haze" : "black", dio.OutsideColor()));
                yield return Shot($"depot-out-{(hz ? "haze" : "black")}");
            }
            dio.outsideHaze = haze0;
            // 마을 그림 끈 판(옛 디오라마) 같은 자리 비교 — 잔점 % 만
            dio.townLook = false; yield return Frames(4); yield return new WaitForEndOfFrame();
            ProbeLine(o, "depot-day 옛 판(townLook 끔, 결 B)", true);
            dio.townLook = true; yield return Frames(4);
        }
        if (sl != null) sl.SetOn(sl0);
        dio.flatGrain = g0; dio.outsideHaze = haze0;
    }

    void ProbeLine(List<string> o, string name, bool inside)
    {
        Measure(out float spike, out float edge, out int px);
        string face = FaceSpread(inside);
        o.Add(string.Format(Inv, "{0,-34} 잔점 {1,6:F2}%  경계 {2,6:F2}%  (픽셀 {3}, 1 픽셀 = RT {4}칸)  {5}", name, spike, edge, px, probeStride, face));
    }

    // 화면 한 장에서 같은 면(콜라이더·법선 같음)에 맞은 픽셀을 모아 가장 큰 면의 밝기 퍼짐. 실내 = 지금 보이는 방 상자 안의 벽만(IsWall, 셰이더가 자른 카메라 쪽 벽·천장은 건너뜀), 바깥 = 플레이어 아닌 아무 면
    List<A7ShadeGlobals.Interior> faceRooms;
    // 바깥 기준 면 = 마을 집(LofiSurface 재질, 땅 제외 — 법선이 거의 수평인 면만)
    static bool TownHouse(Collider col)
    {
        var r = col.GetComponent<Renderer>(); if (r == null) r = col.GetComponentInParent<Renderer>();
        return r != null && r.sharedMaterial != null && r.sharedMaterial.shader.name == "PitCrew/LofiPilot/Surface" && !(col is TerrainCollider);
    }
    string FaceSpread(bool inside)
    {
        var L = ReadLum(out int w, out int h);
        if (L == null) return "면 -";
        var it = inside ? faceRooms?.Find(i => i.name == dio.RoomNow) : null;
        if (inside && it == null) return "면 - (보이는 방 없음)";
        Vector3 c = inside ? it.box.MultiplyPoint3x4(cam.transform.position) : Vector3.zero;
        var groups = new Dictionary<string, List<float>>(); var dist = new Dictionary<string, float>();
        for (int y = 2; y < h - 2; y += 2)
            for (int x = 2; x < w - 2; x += 2)
            {
                var ray = cam.ViewportPointToRay(new Vector3((x + 0.5f) / w, (y + 0.5f) / h, 0f));
                RaycastHit hit = default; bool got = false;
                var hits = Physics.RaycastAll(ray, 400f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (p, q) => p.distance.CompareTo(q.distance));
                foreach (var hh in hits)
                {
                    if (hh.collider.transform.IsChildOf(pl.transform)) continue;
                    if (inside)
                    {
                        Vector3 b = it.box.MultiplyPoint3x4(hh.point);
                        bool cut = Mathf.Abs(b.x) > 1.02f || Mathf.Abs(b.y) > 1.02f || Mathf.Abs(b.z) > 1.02f || b.y > 0.85f
                                || (Mathf.Abs(c.x) > 1f && b.x * Mathf.Sign(c.x) > 0.9f) || (Mathf.Abs(c.z) > 1f && b.z * Mathf.Sign(c.z) > 0.9f);
                        if (cut) continue;
                    }
                    hit = hh; got = true; break;
                }
                if (!got || (inside && !A7ShadeGlobals.IsWall(hit.collider)) || (!inside && (!TownHouse(hit.collider) || Mathf.Abs(hit.normal.y) > 0.3f))) continue;
                float l = L[y * w + x]; if (l < 0.02f) continue;
                Vector3 nr = hit.normal; string key = hit.collider.GetInstanceID() + ":" + Mathf.RoundToInt(nr.x * 10) + "," + Mathf.RoundToInt(nr.y * 10) + "," + Mathf.RoundToInt(nr.z * 10);
                if (!groups.TryGetValue(key, out var lst)) { lst = new List<float>(); groups[key] = lst; dist[key] = 0f; }
                lst.Add(l); dist[key] += hit.distance;
            }
        string best = null; foreach (var kv in groups) if (best == null || kv.Value.Count > groups[best].Count) best = kv.Key;
        if (best == null) return "면 없음";
        var v = groups[best]; float m = 0f; foreach (var l in v) m += l; m /= v.Count;
        float s = 0f; foreach (var l in v) s += (l - m) * (l - m); s = Mathf.Sqrt(s / v.Count);
        return string.Format(Inv, "면 퍼짐 sd {0:0.0000} / 평균 {1:0.000} = {2:0.0}% (표본 {3}, 거리 {4:0.0} m)", s, m, 100f * s / Mathf.Max(1e-4f, m), v.Count, dist[best] / v.Count);
    }

    Texture2D probeTex;
    int probeStride;
    // 저해상 화면 밝기(감마). RT 는 renderScale 로 커진 크기(1280x720) — 실제 그리는 픽셀(360 높이) 한 칸씩 띄어 읽는다(칸 가운데). RT 가 없으면 화면을 읽는다
    float[] ReadLum(out int w, out int h)
    {
        var rt = cam.targetTexture; Texture2D tex; int W, H;
        if (rt != null)
        {
            if (probeTex == null || probeTex.width != rt.width || probeTex.height != rt.height) probeTex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            probeTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); probeTex.Apply(false);
            RenderTexture.active = prev; tex = probeTex; W = rt.width; H = rt.height;
        }
        else
        {
            tex = ScreenCapture.CaptureScreenshotAsTexture(); W = tex.width; H = tex.height;
        }
        var px32 = tex.GetPixels32();
        if (rt == null) Destroy(tex);
        int k = Mathf.Max(1, Mathf.RoundToInt(H / 360f)); w = W / k; h = H / k;
        probeStride = k;
        var L = new float[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) { var p = px32[(y * k + k / 2) * W + x * k + k / 2]; L[y * w + x] = (0.299f * p.r + 0.587f * p.g + 0.114f * p.b) / 255f; }
        return L;
    }
    void Measure(out float spike, out float edge, out int count)
    {
        spike = edge = 0f; count = 0;
        var L = ReadLum(out int w, out int h);
        const float T = 0.06f, Black = 0.02f; int sp = 0, ed = 0;
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = y * w + x; float c = L[i];
                float a = L[i - 1], b = L[i + 1], u = L[i - w], dn = L[i + w];
                if (c < Black || a < Black || b < Black || u < Black || dn < Black) continue;
                count++;
                float mx = Mathf.Max(Mathf.Max(a, b), Mathf.Max(u, dn)), mn = Mathf.Min(Mathf.Min(a, b), Mathf.Min(u, dn));
                if (c - mx > T || mn - c > T) sp++;
                if (Mathf.Max(Mathf.Max(Mathf.Abs(c - a), Mathf.Abs(c - b)), Mathf.Max(Mathf.Abs(c - u), Mathf.Abs(c - dn))) > T) ed++;
            }
        if (count > 0) { spike = 100f * sp / count; edge = 100f * ed / count; }
    }

    void LogHeld(string tag)
    {
        var hb = cam.transform.Find("HeldBox");
        if (hb == null) { log.Add($"[든 상자] {tag} HeldBox 없음"); return; }
        string rs = ""; foreach (var r in hb.GetComponentsInChildren<Renderer>(true)) rs += r.name + ":" + (r.sharedMaterial != null ? r.sharedMaterial.name : "-") + "|" + r.sharedMaterial.shader.name + " ";
        var vp = cam.WorldToViewportPoint(hb.position);
        log.Add(string.Format(Inv, "[든 상자] {0} active={1} 디오라마={2} 켜짐={3} 위치={4} 화면={5} 카메라→상자 {6:0.00}m 플레이어 정면·카메라 정면 내적={7:0.00} 렌더러 {8}",
            tag, hb.gameObject.activeInHierarchy, RoomDiorama.Active, hb.gameObject.activeSelf, V(hb.position), V(vp), Vector3.Distance(cam.transform.position, hb.position),
            Vector3.Dot(pl.transform.forward, cam.transform.forward), rs));
    }

    static bool InBox(Vector3 q) => Mathf.Abs(q.x) <= 1f && Mathf.Abs(q.y) <= 1f && Mathf.Abs(q.z) <= 1f;

    // ④ 보관소 안에서 상자 들기 → 손 물건이 가슴 앞인지
    IEnumerator HoldCheck(List<A7ShadeGlobals.Interior> list)
    {
        var it = list.Find(i => i.name.StartsWith("D198_depot"));
        // 과제 20261105-0535 ④: 전엔 뒤 선반 구석(b)에 카메라와 같은 쪽을 보고 서서 가슴 앞 상자가 몸·선반 뒤로 숨었다 →
        // 빈 바닥 가운데에 카메라 쪽을 보고 선다
        float yaw = RoomDiorama.FixedYaw(it, list, FindObjectsByType<DoorHinge>(FindObjectsSortMode.None), null);
        d.DebugTeleport(it.box.inverse.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up * 0.1f, yaw + 180f, null, 0f);
        yield return Secs(1.8f);
        SignalBus.Raise(Signal.BoxTaken, "봇");
        yield return Secs(1f);
        var hb = cam.transform.Find("HeldBox");
        string s = hb == null ? "HeldBox 없음" : string.Format(Inv, "HeldBox 켜짐={0} 화면={1} 가슴까지={2:0.00}m",
            hb.gameObject.activeInHierarchy, V(cam.WorldToViewportPoint(hb.position)), Vector3.Distance(hb.position, pl.transform.position + Vector3.up * 1.1f));
        log.Add($"[손] active={RoomDiorama.Active} 옮긴 손={dio.HandsMoved} grainB={dio.grainB} {s}");
        if (hb != null)
        {
            // 과제 20261105-0535 ④: 보이는가 — 렌더러 켜짐·셰이더·카메라 → 상자 사이 첫 콜라이더(몸 캡슐이면 몸 뒤에 가림)
            foreach (var r in hb.GetComponentsInChildren<Renderer>(true))
                log.Add($"[손] 렌더러 {r.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} forceOff={r.forceRenderingOff} layer={LayerMask.LayerToName(r.gameObject.layer)} cullMask={(cam.cullingMask & (1 << r.gameObject.layer)) != 0} shader={(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-")} bounds={V(r.bounds.center)} size={V(r.bounds.size)}");
            Vector3 cp = cam.transform.position, to = hb.position - cp;
            // 천장(셰이더가 자름)은 빼고 본다 — 이 건물 밖·자른 쪽 콜라이더면 다음으로
            string first = "없음";
            var hits = Physics.RaycastAll(cp, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var fh in hits)
                if (fh.collider.transform.IsChildOf(pl.transform) || InBox(it.box.MultiplyPoint3x4(fh.point)))
                { first = fh.collider.name + " 거리 " + fh.distance.ToString("F2", Inv) + "/" + to.magnitude.ToString("F2", Inv); break; }
            log.Add($"[손] 카메라→상자 첫 콜라이더 {first} · 몸 앞쪽·카메라 앞쪽 내적={Vector3.Dot(pl.transform.forward, cam.transform.forward):F2}");
        }
        yield return Shot("hold");
    }
}
