#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// QA 전용(과제 20261102-1921 회귀 점검): 오늘 들어간 것들이 서로 안 깨뜨렸나. 게임 로직에는 안 낀다.
/// Monitor.Begin/End — 구간 동안 콘솔 에러·경고(중복 묶음), 프레임 ms(중앙/p95/p99/최대/33ms 넘는 수), 도는 소리 원천이 멈춘 횟수(끊김)를 센다.
/// RunSweep(outDir) — 디버그 창 장소 전부 × (오후, 노을): 이동 → 서 있기 → 걷기 → 점프 → 스크린샷 → 깜빡임(FrameFlickerProbe) →
/// 낚시·카트 운전. 결과 outDir/sweep.json. Play 중 exec 로 RegressSweep.Begin(outDir) 후 Status/Done 을 본다.
/// </summary>
public class RegressSweep : MonoBehaviour
{
    public static string Status = "idle";
    public static bool Done;

    // ───────── 감시기 ─────────
    public class Monitor
    {
        public string tag;
        public readonly List<float> dts = new List<float>();
        public readonly Dictionary<string, int> logs = new Dictionary<string, int>();
        public readonly List<string> dropouts = new List<string>();
        readonly Dictionary<int, bool> wasPlaying = new Dictionary<int, bool>();
        float tStart, srcT; AudioSource[] srcs;

        public void OnLog(string msg, string stack, LogType t)
        {
            if (t == LogType.Log) return;
            string head = msg.Length > 140 ? msg.Substring(0, 140) : msg;
            string key = t + ": " + head.Replace("\n", " ");
            logs.TryGetValue(key, out int n); logs[key] = n + 1;
        }

        /// <summary>50 ms 를 넘은 프레임의 순간: 시각·ms·상태·플레이어 자리·안내·GC 횟수 변화·메모리 변화(원인 짚기용).</summary>
        public readonly List<string> spikes = new List<string>();
        int lastGc = -1; long lastMem; OnFootDirector spikeDir;
        public string SpikesJson()
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < spikes.Count; i++) { if (i > 0) sb.Append(','); sb.Append('"').Append(Esc(spikes[i])).Append('"'); }
            return sb.Append(']').ToString();
        }

        /// <summary>봇이 1920×1080 스크린샷(ReadPixels+PNG 인코딩, 한 번에 60~420 ms)을 찍은 프레임 번호. 그 다음 프레임의 긴 ms 는 게임이 아니라 캡처 비용이다.</summary>
        public static int CaptureMark = -10;
        public int capFrames; public float capMaxMs;

        public void Tick()
        {
            float dtNow = Time.unscaledDeltaTime;
            int gc = System.GC.CollectionCount(0); long mem = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            if (Time.frameCount <= CaptureMark + 2)
            {
                // 20261102-2305 ③: 봇 캡처 프레임 — 프레임 ms 통계에서 빼고 따로 센다(게임 끊김 아님)
                capFrames++; capMaxMs = Mathf.Max(capMaxMs, dtNow * 1000f);
                lastGc = gc; lastMem = mem;
                return;
            }
            if (dtNow > 0.05f && dts.Count > 3)
            {
                if (spikeDir == null) spikeDir = FindFirstObjectByType<OnFootDirector>();
                string st = spikeDir != null ? spikeDir.State + "/" + spikeDir.Prompt : "?";
                var pl = spikeDir != null ? spikeDir.Player : null;
                string pos = pl != null ? pl.transform.position.ToString("F1") : "-";
                spikes.Add(string.Format(CultureInfo.InvariantCulture, "t={0:F1}s f={1} {2:F0}ms gc+{3} mem{4:+0.0;-0.0}MB {5} {6}",
                    Time.realtimeSinceStartup - tStart, Time.frameCount, dtNow * 1000f, lastGc < 0 ? 0 : gc - lastGc, (mem - lastMem) / 1048576f, st, pos));
            }
            lastGc = gc; lastMem = mem;
            dts.Add(dtNow);
            if (srcs == null || Time.realtimeSinceStartup - srcT > 1f) { srcs = FindObjectsByType<AudioSource>(FindObjectsSortMode.None); srcT = Time.realtimeSinceStartup; }   // 목록만 1초마다 — 프레임 ms 를 흔들지 않게
            foreach (var a in srcs)
            {
                if (a == null) continue;
                int id = a.GetInstanceID();
                bool on = a.isPlaying;
                if (wasPlaying.TryGetValue(id, out bool was) && was && !on && a.loop && a.enabled && a.gameObject.activeInHierarchy && a.volume > 0.001f && !AudioListener.pause)
                    dropouts.Add(a.name + "@" + (Time.realtimeSinceStartup - tStart).ToString("F1") + "s");
                wasPlaying[id] = on;
            }
        }

        public void Start() { tStart = Time.realtimeSinceStartup; }
        public string MsJson(int skipFirst = 0)
        {
            var l = new List<float>();
            for (int i = skipFirst; i < dts.Count; i++) l.Add(dts[i] * 1000f);
            if (l.Count == 0) return "{\"n\":0}";
            var s = new List<float>(l); s.Sort();
            int over33 = 0, over50 = 0; foreach (var v in l) { if (v > 33.4f) over33++; if (v > 50f) over50++; }
            return string.Format(CultureInfo.InvariantCulture, "{{\"n\":{0},\"med\":{1:F1},\"p95\":{2:F1},\"p99\":{3:F1},\"max\":{4:F1},\"over33\":{5},\"over50\":{6}}}",
                l.Count, s[s.Count / 2], s[Mathf.Min(s.Count - 1, (int)(s.Count * 0.95f))], s[Mathf.Min(s.Count - 1, (int)(s.Count * 0.99f))], s[s.Count - 1], over33, over50);
        }
        public string LogsJson()
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var kv in logs) { if (!first) sb.Append(','); first = false; sb.Append('"').Append(Esc(kv.Key)).Append(" ×").Append(kv.Value).Append('"'); }
            return sb.Append(']').ToString();
        }
        public string DropJson()
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < dropouts.Count; i++) { if (i > 0) sb.Append(','); sb.Append('"').Append(Esc(dropouts[i])).Append('"'); }
            return sb.Append(']').ToString();
        }
    }

    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "'");
    static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => "[" + F(v.x) + "," + F(v.y) + "," + F(v.z) + "]";

    // 전역 콘솔 집계(Play 내내)
    public static readonly Monitor Global = new Monitor { tag = "global" };
    static Monitor active;
    static bool hooked;
    static void Hook()
    {
        if (hooked) return; hooked = true;
        Application.logMessageReceived += (m, s, t) => { Global.OnLog(m, s, t); active?.OnLog(m, s, t); };
    }

    public static string Begin(string outDir)
    {
        Hook(); Done = false; Status = "starting";
        var go = new GameObject("RegressSweep (qa)");
        go.AddComponent<RegressSweep>().StartCoroutine(go.GetComponent<RegressSweep>().Run(outDir));
        return "OK " + outDir;
    }

    /// <summary>봇 같은 외부 동작 동안 감시만 켠다.</summary>
    public static Monitor MonitorBegin(string tag)
    {
        Hook();
        var m = new Monitor { tag = tag }; m.Start(); active = m;
        var go = new GameObject("RegressMon (qa)"); go.AddComponent<MonTicker>().m = m;
        return m;
    }
    public static string MonitorEnd(Monitor m)
    {
        if (active == m) active = null;
        foreach (var t in FindObjectsByType<MonTicker>(FindObjectsSortMode.None)) if (t.m == m) Destroy(t.gameObject);
        return "{\"tag\":\"" + m.tag + "\",\"ms\":" + m.MsJson(3) + ",\"logs\":" + m.LogsJson() + ",\"dropouts\":" + m.DropJson() + ",\"captureFrames\":" + m.capFrames + ",\"captureMaxMs\":" + m.capMaxMs.ToString("F0", CultureInfo.InvariantCulture) + ",\"spikes\":" + m.SpikesJson() + "}";
    }
    class MonTicker : MonoBehaviour { public Monitor m; void Update() { m.Tick(); } }

    // ───────── 장소 전체 돌기 ─────────
    OnFootDirector dir;
    readonly StringBuilder json = new StringBuilder();
    int rows;

    IEnumerator Run(string outDir)
    {
        System.IO.Directory.CreateDirectory(outDir);
        dir = FindFirstObjectByType<OnFootDirector>();
        if (dir == null) { Status = "FAIL no director"; Done = true; yield break; }
        float t0 = Time.realtimeSinceStartup;
        json.Append("{\"places\":[");
        string[] times = { "afternoon", "dusk" };
        for (int ti = 0; ti < 2; ti++)
        {
            DebugWindow.SetDusk(ti);
            yield return new WaitForSecondsRealtime(0.5f);
            for (int i = 0; i < DebugPlaces.All.Length; i++)
            {
                Status = $"{times[ti]} place {i}";
                yield return OnePlace(outDir, ti, times[ti], i);
            }
        }
        json.Append("],\"modes\":[");
        // 낚시·카트는 장소 돌기 뒤에 노을·오후 각각 한 번
        for (int ti = 0; ti < 2; ti++)
        {
            DebugWindow.SetDusk(ti);
            Status = "fish " + times[ti];
            yield return Fishing(outDir, ti, times[ti]);
            Status = "cart " + times[ti];
            yield return Cart(outDir, ti, times[ti]);
        }
        json.Append("],\"globalLogs\":").Append(Global.LogsJson()).Append(",\"seconds\":").Append(F(Time.realtimeSinceStartup - t0)).Append('}');
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "sweep.json"), json.ToString());
        Status = "done"; Done = true;
        Destroy(gameObject);
    }

    void Row(string s) { if (rows++ > 0 && json[json.Length - 1] != '[') json.Append(','); json.Append(s); }


    /// <summary>처음 보는 자리 n 곳(무작위 좌표): 마을 둘레에서 서 있을 수 있는 점을 뽑아 (오후, 노을) 각각 OnePlace 와 같은 검사. 끝에 부두 끝 추락·사장 몸 통과도 본다.</summary>
    public static string BeginSpots(string outDir, int seed, int n)
    {
        Hook(); Done = false; Status = "starting";
        var go = new GameObject("RegressSweep (qa)");
        var rs = go.AddComponent<RegressSweep>();
        rs.StartCoroutine(rs.RunSpots(outDir, seed, n));
        return "OK " + outDir;
    }

    IEnumerator RunSpots(string outDir, int seed, int n)
    {
        System.IO.Directory.CreateDirectory(outDir);
        dir = FindFirstObjectByType<OnFootDirector>();
        if (dir == null) { Status = "FAIL no director"; Done = true; yield break; }
        var rng = new System.Random(seed);
        var spots = new List<Vector3>();
        int tries = 0;
        while (spots.Count < n && tries++ < 2000)
        {
            float x = 40f + (float)rng.NextDouble() * 250f, z = -290f + (float)rng.NextDouble() * 410f;
            if (!Physics.Raycast(new Vector3(x, 120f, z), Vector3.down, out var h, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (h.point.y < 4f || h.normal.y < 0.92f) continue;
            if (Physics.CheckCapsule(h.point + Vector3.up * 0.4f, h.point + Vector3.up * 1.5f, 0.35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            bool near = false; foreach (var q in spots) if ((q - h.point).sqrMagnitude < 60f * 60f) near = true;
            if (near) continue;
            if (h.collider.name.ToLower().Contains("roof")) continue;
            spots.Add(h.point);
        }
        json.Append("{\"spots\":[");
        for (int k = 0; k < spots.Count; k++) json.Append(k > 0 ? "," : "").Append(V(spots[k]));
        json.Append("],\"places\":[");
        float t0 = Time.realtimeSinceStartup;
        string[] times = { "afternoon", "dusk" };
        for (int ti = 0; ti < 2; ti++)
        {
            DebugWindow.SetDusk(ti);
            yield return new WaitForSecondsRealtime(0.5f);
            for (int k = 0; k < spots.Count; k++)
            {
                Status = $"{times[ti]} spot {k}";
                var feet = spots[k]; float yaw = (float)rng.NextDouble() * 360f; int kk = k;
                yield return OnePlace(outDir, ti, times[ti], k, () => dir.DebugTeleport(feet, yaw, null, yaw + 90f), $"무작위 {k} ({feet.x:F0},{feet.z:F0})");
            }
        }
        json.Append("],\"modes\":[");
        // 부두 끝에서 앞으로 걸어 떨어짐 → 5초 뒤 위치(복귀 있나)
        Status = "pier";
        DebugWindow.GoPlace(5); yield return new WaitForSecondsRealtime(0.8f);
        var pl = dir.Player; pl.simActive = true; pl.simMove = new Vector2(0f, 1f);
        float t = 0f; float minY = 99f; while (t < 3f) { yield return null; t += Time.unscaledDeltaTime; minY = Mathf.Min(minY, pl.transform.position.y); }
        pl.simMove = Vector2.zero; pl.simActive = false;
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, "pier_fall.png"));
        var pAfter3 = pl.transform.position;
        yield return new WaitForSecondsRealtime(5f);
        var pAfter8 = pl.transform.position;
        Row("{" + $"\"mode\":\"pierFall\",\"minY\":{F(minY)},\"after3s\":{V(pAfter3)},\"after8s\":{V(pAfter8)},\"state\":\"{dir.State}\",\"fallRespawnY\":\"n/a\"" + "}");
        // 사장 몸: 콜라이더가 있나 + 정면으로 걸어 지나가나
        Status = "boss";
        var boss = dir.Boss; string bossInfo = boss == null ? "no boss" : "colliders=" + boss.GetComponentsInChildren<Collider>().Length + " pos=" + V(boss.position);
        DebugWindow.GoPlace(2); yield return new WaitForSecondsRealtime(0.8f);
        Vector3 to = boss != null ? boss.position - pl.transform.position : Vector3.forward; to.y = 0f;
        dir.Player.Cam.Yaw = Quaternion.LookRotation(to).eulerAngles.y; yield return null;
        var b0 = pl.transform.position; pl.simActive = true; pl.simMove = new Vector2(0f, 1f);
        float minD = 99f; t = 0f; while (t < 2.5f) { yield return null; t += Time.unscaledDeltaTime; if (boss != null) { var dd = boss.position - pl.transform.position; dd.y = 0f; minD = Mathf.Min(minD, dd.magnitude); } }
        pl.simMove = Vector2.zero; pl.simActive = false;
        Row("{" + $"\"mode\":\"bossWalkInto\",\"info\":\"{Esc(bossInfo)}\",\"startDist\":{F(boss != null ? Vector3.Distance(new Vector3(b0.x, 0, b0.z), new Vector3(boss.position.x, 0, boss.position.z)) : -1f)},\"minDist\":{F(minD)}" + "}");
        json.Append("],\"globalLogs\":").Append(Global.LogsJson()).Append(",\"seconds\":").Append(F(Time.realtimeSinceStartup - t0)).Append('}');
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "spots.json"), json.ToString());
        Status = "done"; Done = true;
        Destroy(gameObject);
    }

    IEnumerator OnePlace(string outDir, int ti, string tname, int i, System.Func<string> goFn = null, string label = null)
    {
        var pl = dir.Player;
        pl.simActive = false; pl.simMove = Vector2.zero;
        string go = goFn != null ? goFn() : DebugWindow.GoPlace(i);
        var mon = new Monitor { tag = tname + "_" + i }; mon.Start(); active = mon;
        // 이동 직후 0.7초(스트리밍·카메라 정리 구간) — 최대 프레임만 따로
        float t = 0f; float hitch = 0f;
        while (t < 0.7f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; hitch = Mathf.Max(hitch, Time.unscaledDeltaTime * 1000f); }
        mon.dts.Clear();
        // 서 있기 1.2초
        t = 0f; while (t < 1.2f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; }
        var p0 = pl.transform.position;
        bool grounded0 = pl.Controller.isGrounded;
        string prompt = dir.Prompt, gaze = dir.GazeTarget ?? "-";
        var amb = AmbienceZones.Active; string ambLabel = amb != null ? amb.Label : "-";
        string state0 = dir.State.ToString();
        RaycastHit h; bool g = Physics.Raycast(p0 + Vector3.up * 0.3f, Vector3.down, out h, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        string gname = g ? h.collider.name : "-"; float gap = g ? p0.y - h.point.y : -99f;
        // 스크린샷(UI 포함 Game 뷰)
        string shot = $"{tname}_{(label != null ? "spot" : "")}{i:00}.png";
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, shot));
        yield return null; yield return null;
        // 걷기 1.5초(앞으로, 달리기 아님)
        pl.simActive = true; pl.simRun = false; pl.simMove = new Vector2(0f, 1f);
        var w0 = pl.transform.position; float minGrounded = 0f, notGroundedFrames = 0;
        t = 0f; int n = 0;
        while (t < 1.5f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; n++; if (!pl.Controller.isGrounded) notGroundedFrames++; }
        var w1 = pl.transform.position;
        float walked = Vector2.Distance(new Vector2(w0.x, w0.z), new Vector2(w1.x, w1.z));
        // 점프(걷는 채로 한 번)
        float y0 = pl.transform.position.y, maxRise = 0f; int jc0 = pl.JumpCount;
        pl.SimJump();
        t = 0f; bool sawAir = false, landed = false;
        while (t < 1.4f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; maxRise = Mathf.Max(maxRise, pl.transform.position.y - y0); if (pl.Airborne) sawAir = true; else if (sawAir && t > 0.2f) landed = true; }
        int jumps = pl.JumpCount - jc0;
        pl.simMove = Vector2.zero; pl.simActive = false;
        var p2 = pl.transform.position;
        // 내려앉음 확인(허공에 서 있지 않음)
        yield return new WaitForSecondsRealtime(0.3f);
        bool end_grounded = pl.Controller.isGrounded;
        active = null;
        Row("{" + $"\"t\":\"{tname}\",\"i\":{i},\"name\":\"{Esc(label ?? DebugPlaces.All[i].name)}\",\"go\":\"{Esc(go)}\",\"state\":\"{state0}\",\"feet\":{V(p0)},\"groundGap\":{F(gap)},\"ground\":\"{Esc(gname)}\",\"grounded\":{grounded0.ToString().ToLower()},\"prompt\":\"{Esc(prompt)}\",\"gaze\":\"{Esc(gaze)}\",\"amb\":\"{Esc(ambLabel)}\",\"hitchMs\":{F(hitch)}," +
            $"\"ms\":{mon.MsJson()},\"walk15s\":{F(walked)},\"airFrames\":{notGroundedFrames},\"jumpRise\":{F(maxRise)},\"jumps\":{jumps},\"sawAir\":{sawAir.ToString().ToLower()},\"landed\":{landed.ToString().ToLower()},\"after\":{V(p2)},\"endGrounded\":{end_grounded.ToString().ToLower()},\"dropouts\":{mon.DropJson()},\"logs\":{mon.LogsJson()},\"shot\":\"{shot}\"" + "}");
        // 깜빡임(제자리) — 같은 자리에서 1.5초
        FrameFlickerProbe.Begin(1.5f, outDir + "/flicker/", $"{tname}_{(label != null ? "spot" : "")}{i:00}");
        while (!FrameFlickerProbe.Done) yield return null;
        yield return null;
        var ff = FrameFlickerProbe.Summary;
        System.IO.File.AppendAllText(System.IO.Path.Combine(outDir, "flicker.jsonl"), ff + "\n");
    }

    IEnumerator Fishing(string outDir, int ti, string tname)
    {
        var mon = new Monitor { tag = "fish_" + tname }; mon.Start(); active = mon;
        string r1 = DebugWindow.Fish();
        float t = 0f; while (t < 1.5f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; }
        string st1 = dir.State.ToString(); string pr1 = dir.Prompt;
        mon.dts.Clear();
        t = 0f; while (t < 2.5f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; }
        string rec = DebugWindow.Recast();
        t = 0f; while (t < 2.0f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; }
        string st2 = dir.State.ToString();
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, $"fish_{tname}.png"));
        yield return null; yield return null;
        // 일어나기(E 와 같은 길 — 앉은 중 SimInteract)
        dir.SimInteract();
        t = 0f; while (t < 1.2f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; }
        string st3 = dir.State.ToString();
        active = null;
        Row("{" + $"\"mode\":\"fish\",\"t\":\"{tname}\",\"sit\":\"{Esc(r1)}\",\"state_after_sit\":\"{st1}\",\"recast\":\"{Esc(rec)}\",\"state_after_recast\":\"{st2}\",\"state_after_interact\":\"{st3}\",\"ms\":{mon.MsJson()},\"dropouts\":{mon.DropJson()},\"logs\":{mon.LogsJson()}" + "}");
    }

    IEnumerator Cart(string outDir, int ti, string tname)
    {
        DebugWindow.GoPlace(8);                       // 큰길
        yield return new WaitForSecondsRealtime(0.8f);
        string tg = DebugWindow.ToggleCart();
        yield return new WaitForSecondsRealtime(0.8f);
        var car = dir.Car; var mon = new Monitor { tag = "cart_" + tname }; mon.Start(); active = mon;
        string st = dir.State.ToString();
        var c0 = car.transform.position;
        // 가속 직진 4초 → 좌 2초 → 우 2초 → 브레이크 2초
        float maxKmh = 0f, minY = c0.y, t = 0f;
        car.SetMoveInput(new Vector2(0f, 1f));
        while (t < 4f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; maxKmh = Mathf.Max(maxKmh, car.CurrentSpeedMps * 3.6f); minY = Mathf.Min(minY, car.transform.position.y); car.SetMoveInput(new Vector2(0f, 1f)); }
        var c1 = car.transform.position;
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outDir, $"cart_{tname}.png"));
        t = 0f; while (t < 2f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; car.SetMoveInput(new Vector2(-1f, 1f)); maxKmh = Mathf.Max(maxKmh, car.CurrentSpeedMps * 3.6f); minY = Mathf.Min(minY, car.transform.position.y); }
        t = 0f; while (t < 2f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; car.SetMoveInput(new Vector2(1f, 1f)); maxKmh = Mathf.Max(maxKmh, car.CurrentSpeedMps * 3.6f); minY = Mathf.Min(minY, car.transform.position.y); }
        float kmhBeforeBrake = car.CurrentSpeedMps * 3.6f;
        t = 0f; while (t < 2f) { yield return null; mon.Tick(); t += Time.unscaledDeltaTime; car.SetMoveInput(new Vector2(0f, -1f)); }
        car.SetMoveInput(Vector2.zero);
        float kmhAfterBrake = car.CurrentSpeedMps * 3.6f;
        var c2 = car.transform.position;
        string ex = DebugWindow.ToggleCart();
        yield return new WaitForSecondsRealtime(0.8f);
        string st2 = dir.State.ToString();
        active = null;
        Row("{" + $"\"mode\":\"cart\",\"t\":\"{tname}\",\"board\":\"{Esc(tg)}\",\"state\":\"{st}\",\"start\":{V(c0)},\"after4s\":{V(c1)},\"end\":{V(c2)},\"dist4s\":{F(Vector3.Distance(c0, c1))},\"maxKmh\":{F(maxKmh)},\"kmhBeforeBrake\":{F(kmhBeforeBrake)},\"kmhAfterBrake\":{F(kmhAfterBrake)},\"minY\":{F(minY)},\"exit\":\"{Esc(ex)}\",\"state_after_exit\":\"{st2}\",\"ms\":{mon.MsJson()},\"dropouts\":{mon.DropJson()},\"logs\":{mon.LogsJson()}" + "}");
    }
}
#endif
