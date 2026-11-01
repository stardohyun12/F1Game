#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 기본 점프 검증 봇(과제 20261102-0315, D-248 ②). OnFootPlayer.SimJump 로 Space 를 대신한다.
/// 장소 4곳(평지·비탈·집 앞 돌계단·부두 끝)에서 제자리·걸으며 점프 높이/체공 실측 + 연속 프레임 6장,
/// 비탈·계단 내려가며 연속 점프 10회(끼임), 문턱 넘기, 코요테/미리 누름, 대화·낚시·카트에서 점프 불가.
/// Play 중 exec 로 JumpProbe.Begin(증거폴더). 진행 = JumpProbe.Status, 결과 = 증거폴더/jump.json · jump.txt.
/// </summary>
public class JumpProbe : MonoBehaviour
{
    public static string Status = "idle";

    [System.Serializable]
    public class Rec
    {
        public string site, kind;
        public float startY, apexRise, airTime, horizDist, landDy, camDipMax, camRiseAtApex;
        public int ok;
    }
    [System.Serializable] class Out { public List<Rec> jumps = new List<Rec>(); public List<string> checks = new List<string>(); }

    struct Site { public string name; public Vector3 feet; public float yaw; public Site(string n, float x, float y, float z, float yaw) { name = n; feet = new Vector3(x, y, z); this.yaw = yaw; } }

    string outDir;
    OnFootDirector dir; OnFootPlayer pl; OnFootCamera cam;
    readonly Out o = new Out();
    readonly StringBuilder txt = new StringBuilder();
    Site pier;

    public static string Begin(string evidenceDir)
    {
        var d = FindFirstObjectByType<OnFootDirector>();
        if (d == null) return "FAIL: OnFootDirector 없음";
        var old = FindFirstObjectByType<JumpProbe>();
        if (old != null) Destroy(old.gameObject);
        var p = new GameObject("_JumpProbe").AddComponent<JumpProbe>();
        p.outDir = evidenceDir; p.dir = d;
        Status = "starting";
        return "OK";
    }

    void Line(string s) { txt.AppendLine(s); Debug.Log("[JumpProbe] " + s); }
    void Check(string name, bool pass, string detail) { string l = (pass ? "PASS " : "FAIL ") + name + " — " + detail; o.checks.Add(l); Line(l); }

    IEnumerator Wait(float s) { float t = Time.time; while (Time.time - t < s) yield return null; }

    IEnumerator Go(Site s, Vector2 move)
    {
        dir.DebugTeleport(s.feet, s.yaw, null, 0f);
        pl.simMove = Vector2.zero;
        cam.Yaw = s.yaw; cam.Pitch = 8f;
        yield return Wait(0.7f);
        cam.Yaw = s.yaw;
        pl.simMove = move;
        if (move != Vector2.zero) yield return Wait(0.35f);
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(outDir);
        float w = 0f;
        while (dir.State == OnFootDirector.Mode.Starting && w < 10f) { w += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSeconds(0.5f);
        pl = dir.Player; cam = pl.Cam;
        pl.simActive = true; cam.simActive = true;
        Time.captureFramerate = 60;    // 프레임당 1/60 s 고정 — 에디터 렉(다른 exec 컴파일)이 점프 물리를 못 건드린다
        var spot = dir.DebugSpot;
        var sp = spot != null ? spot.StandPoint() : Vector3.zero;
        pier = new Site("pier", sp.x, sp.y, sp.z, spot != null ? spot.SeatYaw : 180f);
        var sites = new[]
        {
            new Site("flat", 205f, 20.3f, -76.5f, 90f),
            new Site("slope", 238.5f, 26.3f, -46.8f, 133f),
            new Site("stairs", 196.35f, 24.2f, -69.0f, 175f),
            pier,
        };

        // 1) 높이·체공 실측: 장소마다 제자리 3 + 걸으며 3 (걸으며 첫 번째에 연속 프레임 6장)
        foreach (var s in sites)
        {
            Status = "measure " + s.name;
            for (int k = 0; k < 3; k++) { Rec r = null; yield return OneJump(s, Vector2.zero, "still", false, x => r = x); o.jumps.Add(r); }
            for (int k = 0; k < 3; k++)
            {
                Rec r = null;
                // 부두는 끝이 가까워 제자리 시작점에서 앞으로 걸으면 떨어진다 → 바다 쪽을 보고 뒷걸음(땅 쪽으로)
                yield return OneJump(s, s.name == "pier" ? new Vector2(0f, -1f) : new Vector2(0f, 1f), "walk", k == 0, x => r = x);
                o.jumps.Add(r);
            }
        }
        // 부두 끝(가장자리 0.3 m 앞) 제자리 점프 — 판자 끝에서 안 미끄러지는지
        {
            Status = "pier end still";
            var fw = Quaternion.Euler(0f, pier.yaw, 0f) * Vector3.forward;
            var end = new Site("pier_end", pier.feet.x + fw.x * 1.9f, pier.feet.y, pier.feet.z + fw.z * 1.9f, pier.yaw);
            Rec r = null; yield return OneJump(end, Vector2.zero, "still", false, x => r = x); o.jumps.Add(r);
        }

        // 1b) 처음 보는 자리: 고정 좌표 2곳 + 시드 고정 무작위 3곳(마을 한 구역에서 위에서 레이로 땅을 찾음)
        {
            var extra = new List<Site> { new Site("garageyard", 229.4f, 26.8f, -57.6f, 200f) };
            var rng = new System.Random(1102);
            for (int i = 0; i < 3; i++)
            {
                float x = 180f + (float)rng.NextDouble() * 70f, z = -80f + (float)rng.NextDouble() * 40f;
                if (Physics.Raycast(new Vector3(x, 80f, z), Vector3.down, out var h, 120f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && h.normal.y > 0.9f)
                    extra.Add(new Site($"rand{i}_{h.collider.name}", x, h.point.y, z, (float)rng.NextDouble() * 360f));
            }
            foreach (var s in extra)
            {
                Status = "extra " + s.name;
                foreach (var mv in new[] { Vector2.zero, new Vector2(0f, 1f) })
                { Rec r = null; yield return OneJump(s, mv, mv == Vector2.zero ? "still" : "walk", false, x => r = x); o.jumps.Add(r); }
            }
        }

        // 2) 내려가며 연속 점프 10회
        yield return Downhill(sites[1], 10);
        yield return Downhill(sites[2], 10);

        // 3) 문턱 넘기 (방문 · 현관문)
        yield return Doorway("방문", new Vector3(187.94f, 28.5f, -62.6f), 3f, new Vector3(188.06f, 28.5f, -59.9f), true);
        yield return Doorway("현관문", new Vector3(184.62f, 25.4f, -61.8f), 180f, new Vector3(184.6f, 25.4f, -65.6f), false);

        // 4) 코요테·미리 누름
        yield return Coyote();
        yield return Buffer();

        // 5) 막힌 상태
        yield return Blocked();

        pl.simMove = Vector2.zero;
        Time.captureFramerate = 0;
        File.WriteAllText(Path.Combine(outDir, "jump.json"), JsonUtility.ToJson(o, true));
        File.WriteAllText(Path.Combine(outDir, "jump.txt"), txt.ToString());
        Status = "DONE";
    }

    // 한 번 뛰고 높이·체공·착지 눌림을 잰다.
    IEnumerator OneJump(Site s, Vector2 move, string kind, bool grab, System.Action<Rec> done)
    {
        yield return Go(s, move);
        var p0 = pl.transform.position; float baseCam = cam.transform.position.y;
        int jc = pl.JumpCount;
        pl.SimJump();
        int shot = 0;
        var r = new Rec { site = s.name, kind = kind, startY = p0.y };
        float t0 = Time.time; bool took = false; float landedAt = -1f;
        while (Time.time - t0 < 2.5f)
        {
            if (grab && shot < 6 && Time.time - t0 >= shot * 0.11f)
            {
                PresentGrab.Begin(Path.Combine(outDir, "frames"), $"{s.name}_{kind}_{shot:00}", 1, 0f);
                shot++;
            }
            yield return null;
            var p = pl.transform.position;
            r.apexRise = Mathf.Max(r.apexRise, p.y - p0.y);
            if (pl.Jumping) { took = true; r.camRiseAtApex = Mathf.Max(r.camRiseAtApex, cam.transform.position.y - baseCam); }
            r.camDipMax = Mathf.Max(r.camDipMax, cam.LandDip);
            if (took && !pl.Jumping && landedAt < 0f) { landedAt = Time.time; r.airTime = pl.LastAirTime; r.landDy = p.y - p0.y; r.horizDist = new Vector2(p.x - p0.x, p.z - p0.z).magnitude; }
            if (landedAt > 0f && Time.time - landedAt > 0.3f) break;
        }
        r.ok = (took && landedAt > 0f && pl.JumpCount == jc + 1) ? 1 : 0;
        Line($"{s.name,-8} {kind,-5} apex {r.apexRise:F3} m  air {r.airTime:F3} s  horiz {r.horizDist:F2} m  landDy {r.landDy:F2}  camDip {r.camDipMax * 100f:F1} cm  ok={r.ok}");
        pl.simMove = Vector2.zero;
        done(r);
    }

    // 계속 앞으로 걸으며 땅에 닿는 즉시 다시 뛴다(Space 연타). 끼임 = 한 번 뛴 거리가 너무 짧거나 위치가 멈춤.
    IEnumerator Downhill(Site s, int jumps)
    {
        Status = "downhill " + s.name;
        yield return Go(s, new Vector2(0f, 1f));
        int jc0 = pl.JumpCount; var start = pl.transform.position; float t0 = Time.time;
        var lastTake = start; int lastJc = jc0; int stuck = 0; float maxStep = 0f, minDist = 1e9f; var prev = start; float lastMove = Time.time; bool bumpy = false;
        float maxApexRel = 0f; float apexRef = start.y; var landed = new List<string>();
        while (pl.JumpCount - jc0 < jumps && Time.time - t0 < 15f)
        {
            pl.SimJump();
            yield return null;
            var p = pl.transform.position;
            maxStep = Mathf.Max(maxStep, Mathf.Abs(p.y - prev.y));
            if ((p - prev).sqrMagnitude > 1e-6f) lastMove = Time.time;
            prev = p;
            if (pl.JumpCount != lastJc)
            {
                float d = new Vector2(p.x - lastTake.x, p.z - lastTake.z).magnitude;
                if (lastJc != jc0) { minDist = Mathf.Min(minDist, d); if (d < 0.5f) stuck++; }
                lastTake = p; lastJc = pl.JumpCount; apexRef = p.y;
            }
            maxApexRel = Mathf.Max(maxApexRel, p.y - apexRef);
            if (Time.time - lastMove > 1.0f) { stuck++; bumpy = true; break; }
        }
        pl.simMove = Vector2.zero;
        var end = pl.transform.position; int made = pl.JumpCount - jc0;
        bool pass = made >= jumps && stuck == 0 && !bumpy;
        Check("연속점프 내리막 " + s.name, pass,
            $"점프 {made}/{jumps}회 · 끼임 {stuck} · 점프당 최소 수평 {(minDist > 1e8f ? 0f : minDist):F2} m · 최대 프레임당 y변화 {maxStep:F2} m · 점프 후 최대 상승(이륙점 기준) {maxApexRel:F2} m · 이동 {new Vector2(end.x - start.x, end.z - start.z).magnitude:F1} m, 높이차 {end.y - start.y:F2} · {Time.time - t0:F1}s");
    }

    // 문을 열고 그 사이로 걸으며 계속 뛴다. 문턱·문틀에 머리가 닿아도 끼지 말고 지나가야 한다.
    IEnumerator Doorway(string label, Vector3 from, float yaw, Vector3 to, bool alongZPlus)
    {
        Status = "doorway " + label;
        DoorHinge best = null; float bd = 1e9f; var mid = (from + to) * 0.5f;
        foreach (var d in FindObjectsByType<DoorHinge>(FindObjectsSortMode.None))
        {
            if (!d.configured) continue;
            float dd = new Vector2(d.transform.position.x - mid.x, d.transform.position.z - mid.z).magnitude;
            if (dd < bd) { bd = dd; best = d; }
        }
        if (best == null || bd > 3f) { Check("문턱 " + label, false, "문을 못 찾음(가장 가까운 " + bd.ToString("F1") + " m)"); yield break; }
        string tp = dir.DebugTeleport(from, yaw, null, 0f);
        Line("문 이동: " + tp);
        pl.simMove = Vector2.zero; cam.Yaw = yaw;
        yield return Wait(0.5f);
        if (!best.IsOpen) { best.Toggle(pl.transform.position); yield return Wait(1.2f); }
        int jc0 = pl.JumpCount; var start = pl.transform.position; float t0 = Time.time;
        Vector3 dirv = new Vector3(to.x - from.x, 0f, to.z - from.z).normalized;
        float totalLen = new Vector2(to.x - from.x, to.z - from.z).magnitude;
        pl.simMove = new Vector2(0f, 1f); cam.Yaw = Mathf.Atan2(dirv.x, dirv.z) * Mathf.Rad2Deg;
        bool passed = false; float maxY = start.y, lastMove = Time.time; var prev = start; bool stuck = false;
        while (Time.time - t0 < 6f)
        {
            pl.SimJump();
            yield return null;
            var p = pl.transform.position;
            maxY = Mathf.Max(maxY, p.y);
            if ((p - prev).sqrMagnitude > 1e-6f) lastMove = Time.time;
            prev = p;
            float prog = Vector3.Dot(new Vector3(p.x - start.x, 0f, p.z - start.z), dirv);
            if (prog >= totalLen) { passed = true; break; }
            if (Time.time - lastMove > 1.0f) { stuck = true; break; }
        }
        pl.simMove = Vector2.zero;
        var e = pl.transform.position;
        Check("문턱 " + label, passed && !stuck && pl.JumpCount - jc0 >= 2, $"{(passed ? "통과" : "못 지남")} · 점프 {pl.JumpCount - jc0}회 · {Time.time - t0:F1}s · 시작 대비 끝 {new Vector2(e.x - start.x, e.z - start.z).magnitude:F2}/{totalLen:F2} m · 최고 y 상승 {maxY - start.y:F2} · 끼임 {stuck}");
        if (best.IsOpen) { best.Toggle(pl.transform.position); yield return Wait(0.8f); }
    }

    // 땅 끝을 걸어 나간 뒤 delay 초에 Space: 0.05 s 는 뛰어야, 0.2 s 는 안 뛰어야(코요테 0.1 s)
    IEnumerator Coyote()
    {
        foreach (float delay in new[] { 0.05f, 0.2f })
        {
            Status = "coyote " + delay;
            yield return Go(pier, Vector2.zero);
            pl.simMove = new Vector2(0f, 1f);       // 부두 끝(바다 쪽)으로 걷는다
            int jc0 = pl.JumpCount; float tAir = -1f; float t0 = Time.time; bool pressed = false;
            while (Time.time - t0 < 4f)
            {
                yield return null;
                if (tAir < 0f && pl.Airborne) tAir = Time.time;
                if (tAir > 0f && !pressed && Time.time - tAir >= delay) { pl.SimJump(); pressed = true; t0 = Time.time - 3.5f; }   // 누른 뒤 0.5 s 만 본다
            }
            bool jumped = pl.JumpCount > jc0;
            bool expect = delay < 0.1f;
            Check($"코요테 {delay:F2}s 뒤 Space", jumped == expect, $"뜬 뒤 {delay:F2}s 에 누름 → 점프 {(jumped ? "함" : "안 함")} (기대 {(expect ? "함" : "안 함")}) · 뜬 시각 {(tAir > 0 ? "확인" : "못 봄")}");
            pl.simMove = Vector2.zero;
        }
    }

    // 점프 1회 뒤 착지 0.06 s 전에 또 누르면 닿는 즉시 두 번째 점프, 0.25 s 전이면 안 뛴다.
    IEnumerator Buffer()
    {
        var flat = new Site("flat", 205f, 20.3f, -76.5f, 90f);
        foreach (float lead in new[] { 0.06f, 0.25f })
        {
            Status = "buffer " + lead;
            yield return Go(flat, Vector2.zero);
            pl.SimJump(); yield return null; yield return null;
            while (pl.Jumping) yield return null;
            float T = pl.LastAirTime;                // 방금 잰 체공 — lead 만큼 일찍 누른다
            yield return Wait(0.4f);
            pl.SimJump(); yield return null; yield return null;
            float tTake = Time.time - 2f / 60f; int jc1 = pl.JumpCount;
            while (Time.time - tTake < T - lead) yield return null;
            pl.SimJump();
            while (pl.Jumping) yield return null;
            float tLand = Time.time;
            yield return null; yield return null; yield return null;
            bool second = pl.JumpCount == jc1 + 1;
            bool expect = lead < 0.1f;
            Check($"미리 누름 착지 {lead:F2}s 전", second == expect, $"2번째 점프 {(second ? "함" : "안 함")} (기대 {(expect ? "함" : "안 함")}) · 체공 {pl.LastAirTime:F3}s");
            yield return Wait(1.2f);
        }
    }

    IEnumerator Blocked()
    {
        var flat = new Site("flat", 205f, 20.3f, -76.5f, 90f);
        // 대화 중
        Status = "blocked talk";
        yield return Go(flat, Vector2.zero);
        // 사장 곁으로 순간이동 없이 ForceTalk(시선 무관)
        int jc = pl.JumpCount; float y0 = pl.transform.position.y;
        bool talk = dir.ForceTalk();
        yield return Wait(0.1f);
        for (int i = 0; i < 12; i++) { pl.SimJump(); yield return Wait(0.05f); }
        float dy = pl.transform.position.y - y0;
        Check("대화 중 점프 불가", talk && dir.InTalk && pl.JumpCount == jc && Mathf.Abs(dy) < 0.02f, $"ForceTalk={talk} InTalk={dir.InTalk} 점프 {pl.JumpCount - jc}회 y변화 {dy:F3}");
        // 대화 중에 눌러 둔 Space 가 끝난 뒤 튀어 나오지 않는다
        dir.DebugTeleport(flat.feet, flat.yaw, null, 0f);
        yield return Wait(0.6f);
        Check("대화 뒤 유령 점프 없음", pl.JumpCount == jc && !pl.Jumping, $"점프 {pl.JumpCount - jc}회");

        // 낚시 앉기
        Status = "blocked seated";
        jc = pl.JumpCount;
        string sit = dir.DebugFishNow();
        yield return Wait(0.5f);
        bool seated = dir.State == OnFootDirector.Mode.Seated;
        for (int i = 0; i < 10; i++) { pl.SimJump(); yield return Wait(0.05f); }
        Check("낚시 앉은 상태 점프 불가", seated && pl.JumpCount == jc, $"{sit} State={dir.State} 점프 {pl.JumpCount - jc}회");
        dir.DebugTeleport(flat.feet, flat.yaw, null, 0f);
        yield return Wait(0.6f);
        Check("일어난 뒤 유령 점프 없음", pl.JumpCount == jc && !pl.Jumping, $"점프 {pl.JumpCount - jc}회");

        // 카트 안
        Status = "blocked cart";
        jc = pl.JumpCount;
        string ride = dir.DebugToggleCart();
        yield return Wait(0.5f);
        bool inCart = dir.State == OnFootDirector.Mode.InCart;
        for (int i = 0; i < 10; i++) { pl.SimJump(); yield return Wait(0.05f); }
        bool noJumpInCart = pl.JumpCount == jc;
        string back = dir.DebugToggleCart();
        yield return Wait(0.6f);
        Check("카트 안 점프 불가", inCart && noJumpInCart, $"{ride} State→InCart={inCart} 점프 {pl.JumpCount - jc}회");
        Check("내린 뒤 유령 점프 없음", dir.State == OnFootDirector.Mode.OnFoot && pl.JumpCount == jc && !pl.Jumping, $"{back} State={dir.State} 점프 {pl.JumpCount - jc}회");
    }
}
#endif
