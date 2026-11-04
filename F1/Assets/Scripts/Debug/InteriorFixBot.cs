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

    static bool doorOnly, fitOnly;
    /// <summary>D-307 ⑧: 방마다 들어가 fit-&lt;이름&gt;.png + 거리·캡슐 화면 높이 %·바닥 모서리 화면 안(실제 카메라로 투영)을 bot-fit.txt 에.</summary>
    public static string BeginFit(string outDir) { var s = Begin(outDir); fitOnly = true; return s; }
    public static string Begin(string outDir, bool onlyDoor = false)
    {
        doorOnly = onlyDoor; fitOnly = false;
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
        foreach (var (key, file) in new[] { ("house_home/ROOM_2F", "grain-room2f"), ("house_home/1F", "grain-house1f"), ("D198_depot", "grain-depot") })
        {
            var it = list.Find(i => i.name.StartsWith(key));
            if (it == null) { log.Add("[결] " + key + " 없음"); continue; }
            yield return GoIn(list, it, false);
            dio.grainB = false; yield return Frames(3); yield return Shot(file + "-A");
            dio.grainB = true; yield return Frames(3); yield return Shot(file + "-B");
            dio.grainB = false;
            log.Add($"[결] {file} = {it.name} active={RoomDiorama.Active}");
        }
    }

    // ④ 보관소 안에서 상자 들기 → 손 물건이 가슴 앞인지
    IEnumerator HoldCheck(List<A7ShadeGlobals.Interior> list)
    {
        var it = list.Find(i => i.name.StartsWith("D198_depot"));
        yield return GoIn(list, it, false);
        SignalBus.Raise(Signal.BoxTaken, "봇");
        yield return Secs(1f);
        var hb = cam.transform.Find("HeldBox");
        string s = hb == null ? "HeldBox 없음" : string.Format(Inv, "HeldBox 켜짐={0} 화면={1} 가슴까지={2:0.00}m",
            hb.gameObject.activeInHierarchy, V(cam.WorldToViewportPoint(hb.position)), Vector3.Distance(hb.position, pl.transform.position + Vector3.up * 1.1f));
        log.Add($"[손] active={RoomDiorama.Active} 옮긴 손={dio.HandsMoved} {s}");
        yield return Shot("hold");
    }
}
