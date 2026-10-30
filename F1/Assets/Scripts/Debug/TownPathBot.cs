using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// 과제 0134 QA: 경로 봇 — 사람 운전 판정 대신 RoadSurface 중심선을 따라 실제 차(ArcadeCarController)를 몬다.
///  cover: 차길마다 처음→끝(경사 10%+ 길은 반대로도) — 끼임(4초 0.6m/s 미만)·이탈(반폭+3m 1초)·추락·시간초과 기록, 오르막에서 멈추면 '못 오름'.
///  pairs: 차고/MART → 지점 5쌍, 큰길(돌길 빼고) vs 지름길(전부) 최단 경로를 화물 싣고 몰아 시간·화물 손상·최대 G.
///  Play 중 ucli exec 로 TownPathBot.Start("cover"|"pairs") → TownPathBot.Status 로 진행 확인. 결과는 docs/qa/tmp/townv6-bot-*.txt.
/// </summary>
public class TownPathBot : MonoBehaviour
{
    public static string Status = "idle";
    public string mode = "cover";
    public float timeScale = 3f;

    ArcadeCarController car; Rigidbody rb; CargoBox cargo;
    System.Reflection.FieldInfo moveF;
    readonly StringBuilder sb = new StringBuilder();

    public static TownPathBot Begin(string mode, float ts = 3f)
    {
        var b = new GameObject("TownPathBot_" + mode).AddComponent<TownPathBot>(); b.mode = mode; b.timeScale = ts; return b;
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        // 씬에 저장된 채 Play 되면 LoopPts(정적)가 비어 있다 — QA 스크립트가 채운 뒤에만 돈다
        if (mode.StartsWith("loop") && LoopPts.Count < 2) { Debug.LogWarning("[TownPathBot] LoopPts 비어 있음 — 건너뜀(" + name + ")"); yield break; }
        car = FindFirstObjectByType<ArcadeCarController>(); rb = car.GetComponent<Rigidbody>(); cargo = FindFirstObjectByType<CargoBox>();
        moveF = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        groundF = typeof(ArcadeCarController).GetField("isGrounded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        FeelLog.Clear();
        Time.timeScale = timeScale;
        var roads = FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length >= 2).OrderBy(r => r.roadId).ToList();
        if (mode == "cover") yield return Cover(roads); else if (mode.StartsWith("loop")) yield return Loop(); else yield return Pairs(roads);
        Time.timeScale = 1f; Set(Vector2.zero);
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, $"../../docs/qa/tmp/townv6-bot-{mode}{(Only != "" ? "-only" : "")}.txt"));
        System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        System.IO.File.WriteAllText(path.Replace(".txt", "-feel.tsv"), FeelLog.ToString(), Encoding.UTF8);
        Status = "done " + mode + " → " + path; Debug.Log("[TownPathBot] " + Status);
    }

    void Set(Vector2 v) => moveF.SetValue(car, v);

    void Place(Vector3 p, Vector3 fwd)
    {
        fwd.y = 0; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        rb.position = p + Vector3.up * .6f; rb.rotation = Quaternion.LookRotation(fwd.normalized);
        car.transform.SetPositionAndRotation(rb.position, rb.rotation);
    }

    static float Grade(IList<Vector3> pts)
    {
        float g = 0; for (int i = 0; i + 4 < pts.Count; i += 2) { float h = new Vector2(pts[i + 4].x - pts[i].x, pts[i + 4].z - pts[i].z).magnitude; if (h > .5f) g = Mathf.Max(g, Mathf.Abs(pts[i + 4].y - pts[i].y) / h); }
        return g;
    }

    /// 비면 전부, 아니면 roadId 가 이 중 하나를 포함하는 길만(쉼표). 실패 재현용.
    public static string Only = "";
    /// 0338: 끼임 판정 순간 그 자세로 CCD 실험(대조 1초 → Discrete 1초).
    public static bool StuckProbe = false;

    public class RunResult { public string result = "ok"; public float time, maxDev, dmg, peakLong, peakLat; public Vector3 at; public int stalls; public string why = "";
        public float azSum, azPeak; public int azN, air; }   // 과제 0320-1e: 차체 수직 가속도·접지 끊김
    public static readonly StringBuilder FeelLog = new StringBuilder();   // x z 종류 값 — 히트맵용(road_feel.py)
    System.Reflection.FieldInfo groundF;
    string curRoad = "", slowNote = "";
    float vPrev;

    /// 멈춘 순간 진단: 차 앞뒤 기울기·바퀴 아래 지면·차 몸에 닿은 콜라이더
    string Why(float thr)
    {
        var t = car.transform; var hit = new List<string>();
        foreach (var c in car.GetComponentsInChildren<Collider>()) if (!c.isTrigger)
        {
            var b = c.bounds; foreach (var o in Physics.OverlapBox(b.center, b.extents + Vector3.one * .05f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (o.GetComponentInParent<ArcadeCarController>() == null && Physics.ComputePenetration(c, c.transform.position, c.transform.rotation, o, o.transform.position, o.transform.rotation, out var dir, out var dist))
                    hit.Add($"{o.name}({dir.x:F1},{dir.y:F1},{dir.z:F1})");
        }
        // 0338: 끼임 원인 가르기 — 런 단계(Driving 아니면 차가 매 스텝 0 으로 묶인다)·접지·차가 받은 입력·물리 상태
        var rc = Services.Run as RunController; bool g = groundF == null || (bool)groundF.GetValue(car);
        string st = $" stage{(rc != null ? rc.CurrentStage.ToString() : "-")} gnd{(g ? 1 : 0)} in{car.RawThrottleInput:F2}/{car.ThrottleInput:F2} v{rb.linearVelocity.magnitude:F2} kin{(rb.isKinematic ? 1 : 0)} ts{Time.timeScale:F1} step{car.StepAssists}";
        var fd = FindFirstObjectByType<OnFootDirector>(); if (fd != null) st += " foot" + fd.State;
        return $"pitch{-t.eulerAngles.x:F0} upY{t.up.y:F2} thr{thr:F2}{st} {slowNote} hit[{string.Join(" ", hit.Distinct().Take(4))}]";
    }

    /// 순수 추종: 앞 4+0.5v m 점을 보고 조향, 굽이 R 로 목표속도 √(5.8R) (기획 리듬 모델 그대로), 상한 22 m/s.
    IEnumerator Drive(List<Vector3> pts, List<float> half, RunResult res)
    {
        var cum = new float[pts.Count]; for (int i = 1; i < pts.Count; i++) cum[i] = cum[i - 1] + Vector3.Distance(pts[i], pts[i - 1]);
        float len = cum[pts.Count - 1], timeout = len / 3f + 20f, t0 = Time.time, slow = 0f, off = 0f, lastThr = 0f, vyPrev = rb.linearVelocity.y, airT = 0f; int idx = 0; bool wasAir = false;
        while (true)
        {
            yield return new WaitForFixedUpdate();
            var p = rb.position; float t = Time.time - t0; res.time = t; res.at = p;
            int best = idx; float bd = float.MaxValue;
            for (int i = idx; i < Mathf.Min(pts.Count, idx + 40); i++) { float d = (new Vector2(pts[i].x - p.x, pts[i].z - p.z)).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            idx = best; float dev = Mathf.Sqrt(bd); res.maxDev = Mathf.Max(res.maxDev, dev);
            // 수직 가속도: 놓은 뒤 2초는 내려앉는 중이라 뺀다. 접지 끊김 = 레이 접지가 0.06초(3스텝) 넘게 빠진 횟수
            float vy = rb.linearVelocity.y, az = (vy - vyPrev) / Time.fixedDeltaTime; vyPrev = vy;
            if (t > 2f)
            {
                res.azSum += az * az; res.azN++; if (Mathf.Abs(az) > res.azPeak) res.azPeak = Mathf.Abs(az);
                if (Mathf.Abs(az) > 6f) FeelLog.AppendLine($"{p.x:F1}	{p.z:F1}	az	{az:F1}	{curRoad}");
                bool g = groundF == null || (bool)groundF.GetValue(car);
                if (!g) { airT += Time.fixedDeltaTime; if (airT > .06f && !wasAir) { wasAir = true; res.air++; FeelLog.AppendLine($"{p.x:F1}	{p.z:F1}	air	1	{curRoad}"); } }
                else { airT = 0f; wasAir = false; }
            }
            if (cargo != null) { res.peakLong = Mathf.Max(res.peakLong, cargo.PeakLongG); res.peakLat = Mathf.Max(res.peakLat, cargo.PeakLatG); }
            if (idx >= pts.Count - 3 || len - cum[idx] < 3f) break;
            if (p.y < -2f) { res.result = "추락"; break; }
            if (t > timeout) { res.result = "시간초과"; break; }
            float v = rb.linearVelocity.magnitude;
            if (v < .6f && t > 2f) { if (slow == 0f) slowNote = $"slowFrom v{vPrev:F1}@{t:F1}s"; slow += Time.fixedDeltaTime; } else slow = 0f;
            vPrev = v;
            if (slow > 4f)
            {
                var ahead = pts.Skip(idx).Take(12).ToList();
                res.result = Grade(ahead) > .10f ? "못오름" : "끼임"; res.why = Why(lastThr);
                if (StuckProbe)
                {
                    // 0338: 끼인 자세 그대로 원인 가르기 — 대조(그대로 1초) → CCD 를 Discrete 로 1초. 움직인 거리만 기록.
                    var mode0 = rb.collisionDetectionMode; var p0 = rb.position;
                    for (int k = 0; k < 50; k++) { Set(new Vector2(0f, 1f)); yield return new WaitForFixedUpdate(); }
                    float dCtl = Vector3.Distance(p0, rb.position); p0 = rb.position;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    for (int k = 0; k < 50; k++) { Set(new Vector2(0f, 1f)); yield return new WaitForFixedUpdate(); }
                    rb.collisionDetectionMode = mode0;
                    res.why += $" probe ctl{dCtl:F2}m discrete{Vector3.Distance(p0, rb.position):F2}m";
                }
                break;
            }
            if (dev > half[idx] + 3f) off += Time.fixedDeltaTime; else off = 0f;
            if (off > 1f) { res.result = "이탈"; res.why = Why(lastThr); break; }
            // 조향
            float look = 4f + v * .5f; int ti = idx; while (ti < pts.Count - 1 && cum[ti] - cum[idx] < look) ti++;
            var fw = car.transform.forward; fw.y = 0; var to = pts[ti] - p; to.y = 0;
            float steer = Mathf.Clamp(Vector3.SignedAngle(fw, to, Vector3.up) / 25f, -1f, 1f);
            // 목표속도: 앞 25m 안 10m 현의 꺾임 최댓값 → R
            float maxAng = 0f; for (int i = idx; i < pts.Count - 1 && cum[i] - cum[idx] < 25f; i += 2)
            {
                int j = i; while (j < pts.Count - 1 && cum[j] - cum[i] < 5f) j++; int k = j; while (k < pts.Count - 1 && cum[k] - cum[j] < 5f) k++;
                if (k == j || j == i) break;
                maxAng = Mathf.Max(maxAng, Vector3.Angle(Flat(pts[j] - pts[i]), Flat(pts[k] - pts[j])));
            }
            float R = maxAng < .5f ? 999f : 5f / (maxAng * Mathf.Deg2Rad);
            float vt = Mathf.Clamp(Mathf.Sqrt(5.8f * R), 4f, 22f);
            float thr = Mathf.Clamp((vt - v) * .4f, -1f, 1f); if (v < 3f) thr = Mathf.Max(thr, .8f);
            Set(new Vector2(steer, thr)); lastThr = thr;
        }
        Set(Vector2.zero);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

    /// 과제 0248: 튜토리얼 고리(LoopPts, 2m 간격 XZ) 를 정방향·역방향 각 LoopN 회. 실패는 위치·원인 줄로.
    public static List<Vector3> LoopPts = new List<Vector3>();
    public static int LoopN = 20; public static float LoopHalf = 3.5f;
    IEnumerator Loop()
    {
        sb.AppendLine("run\tdir\tresult\ttime\tmaxDev\tat\twhy\tair");
        int ok = 0, fail = 0;
        for (int n = 0; n < LoopN; n++) foreach (var dir in new[] { 1, -1 })
        {
            var P = dir > 0 ? LoopPts.ToList() : Enumerable.Reverse(LoopPts).ToList();
            for (int i = 0; i < P.Count; i++) { var q = P[i]; if (Physics.Raycast(new Vector3(q.x, 200f, q.z), Vector3.down, out var h, 400f, ~0, QueryTriggerInteraction.Ignore)) q.y = h.point.y; P[i] = q; }
            // 막다른 길 끝(되돌이, 꺾임 > 150°)에서 다리를 나눈다 — 거기선 차를 돌려 놓고 이어 감(배달 뒤 플레이어가 돌리는 자리)
            var legs = new List<List<Vector3>> { new List<Vector3> { P[0] } };
            for (int i = 1; i < P.Count; i++) { legs[legs.Count - 1].Add(P[i]); if (i + 1 < P.Count && Vector3.Angle(Flat(P[i] - P[i - 1]), Flat(P[i + 1] - P[i])) > 150f) legs.Add(new List<Vector3> { P[i] }); }
            var res = new RunResult(); float tSum = 0f; int step0 = car.StepAssists;
            foreach (var L in legs.Where(l => l.Count > 4))
            {
                Place(L[0], L[3] - L[0]); yield return new WaitForFixedUpdate();
                var r1 = new RunResult(); yield return Drive(L, Enumerable.Repeat(LoopHalf, L.Count).ToList(), r1);
                tSum += r1.time; res.maxDev = Mathf.Max(res.maxDev, r1.maxDev); res.air += r1.air; res.at = r1.at;
                if (r1.result != "ok") { res.result = r1.result; res.why = r1.why; break; }
            }
            res.time = tSum; res.why += $" legs{legs.Count} step{car.StepAssists - step0}";
            if (res.result == "ok") ok++; else fail++;
            sb.AppendLine($"{n}\t{dir}\t{res.result}\t{res.time:F1}\t{res.maxDev:F1}\t{res.at.x:F0},{res.at.y:F0},{res.at.z:F0}\t{res.why}\t{res.air}");
            Status = $"loop {n}/{LoopN} ok{ok} fail{fail}";
        }
        sb.Insert(0, $"# loop ok {ok} fail {fail}\n");
    }

    IEnumerator Cover(List<RoadSurface> roads)
    {
        sb.AppendLine("road\tdir\tlen\tgrade\tresult\ttime\tmaxDev\tat\twhy\tazRms\tazPeak\tair\tkind");
        int ok = 0, fail = 0, skip = 0, n = 0; var counts = new Dictionary<string, int>();
        foreach (var r in roads)
        {
            n++;
            if (Only != "" && !Only.Split(',').Any(s => r.roadId.Contains(s))) continue;
            var pts = r.centerline.ToList(); float len = 0; for (int i = 1; i < pts.Count; i++) len += Vector3.Distance(pts[i], pts[i - 1]);
            if (len < 8f) { skip++; continue; }
            float g = Grade(pts);
            foreach (var dir in g >= .10f ? new[] { 1, -1 } : new[] { 1 })
            {
                var P = dir > 0 ? pts : Enumerable.Reverse(pts).ToList();
                Place(P[0], P[Mathf.Min(3, P.Count - 1)] - P[0]);
                yield return new WaitForFixedUpdate();
                var res = new RunResult(); var half = Enumerable.Repeat(r.width * .5f, P.Count).ToList();
                Status = $"cover {n}/{roads.Count} {r.roadId} ok{ok} fail{fail}"; curRoad = r.roadId;
                yield return Drive(P, half, res);
                if (res.result == "ok") ok++; else fail++;
                counts[res.result] = counts.TryGetValue(res.result, out var c) ? c + 1 : 1;
                sb.AppendLine($"{r.roadId}\t{(dir > 0 ? "+" : "-")}\t{len:F0}\t{g * 100:F1}\t{res.result}\t{res.time:F1}\t{res.maxDev:F1}\t{res.at.x:F0},{res.at.y:F1},{res.at.z:F0}\t{res.why}\t{Mathf.Sqrt(res.azSum / Mathf.Max(1, res.azN)):F2}\t{res.azPeak:F1}\t{res.air}\t{r.kind}");
            }
        }
        sb.Insert(0, $"# cover: 길 {roads.Count} 주행 {ok + fail} 성공 {ok} 실패 {fail} 짧아서 건넘(<8m) {skip} | " + string.Join(" ", counts.Select(kv => kv.Key + "=" + kv.Value)) + "\n");
    }

    // ---------------------------------------------------------------- 배달 쌍
    IEnumerator Pairs(List<RoadSurface> roads)
    {
        // 그래프: 중심선 점 = 노드, 앞뒤 연결 + 다른 길 점과 2.5m 안이면 연결(갈림목)
        var nodes = new List<Vector3>(); var road = new List<int>(); var start = new List<int>();
        for (int ri = 0; ri < roads.Count; ri++) { start.Add(nodes.Count); foreach (var p in roads[ri].centerline) { nodes.Add(p); road.Add(ri); } }
        var adj = new List<(int, float)>[nodes.Count]; for (int i = 0; i < nodes.Count; i++) adj[i] = new List<(int, float)>();
        void Link(int a, int b) { float d = Vector3.Distance(nodes[a], nodes[b]); adj[a].Add((b, d)); adj[b].Add((a, d)); }
        for (int i = 0; i + 1 < nodes.Count; i++) if (road[i] == road[i + 1]) Link(i, i + 1);
        var grid = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < nodes.Count; i++) { var k = ((int)Mathf.Floor(nodes[i].x / 3f), (int)Mathf.Floor(nodes[i].z / 3f)); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<int>(); l.Add(i); }
        int junc = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            int gx = (int)Mathf.Floor(nodes[i].x / 3f), gz = (int)Mathf.Floor(nodes[i].z / 3f);
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) if (grid.TryGetValue((gx + dx, gz + dz), out var l))
                foreach (var j in l) if (j > i && road[j] != road[i] && Vector3.Distance(nodes[i], nodes[j]) < 2.5f) { Link(i, j); junc++; }
        }
        List<int> Path(int a, int b, bool allowRough)
        {
            var dist = new float[nodes.Count]; var prev = new int[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
            var pq = new SortedSet<(float, int)>(); dist[a] = 0; pq.Add((0, a));
            while (pq.Count > 0)
            {
                var (d, u) = pq.Min; pq.Remove(pq.Min); if (u == b) break; if (d > dist[u]) continue;
                foreach (var (w, c) in adj[u])
                {
                    if (!allowRough && roads[road[w]].kind == "shortcut") continue;
                    float nd = d + c; if (nd < dist[w]) { if (dist[w] < float.MaxValue) pq.Remove((dist[w], w)); dist[w] = nd; prev[w] = u; pq.Add((nd, w)); }
                }
            }
            if (prev[b] < 0 && a != b) return null;
            var path = new List<int>(); for (int x = b; x >= 0; x = prev[x]) path.Add(x); path.Reverse(); return path;
        }
        int NearNode(Vector3 q) { int bi = 0; float bd = float.MaxValue; for (int i = 0; i < nodes.Count; i++) { float d = (Flat(nodes[i] - q)).sqrMagnitude; if (d < bd) { bd = d; bi = i; } } return bi; }
        Vector3 Pos(string n) { var g = GameObject.Find(n); return g != null ? g.transform.position : Vector3.zero; }
        var pairs = new[] { ("GARAGE_Return", "STOP_tea", "Box"), ("GARAGE_Return", "STOP_top_house", "Eggs"), ("GARAGE_Return", "STOP_lighthouse", "Glass"), ("GARAGE_Return", "STOP_hill_field", "Box"), ("STOP_MART", "STOP_top_house", "Eggs") };
        sb.AppendLine($"# pairs: 노드 {nodes.Count} 갈림목 연결 {junc}");
        sb.AppendLine("pair\troute\tm\troughM\tresult\ttime\tdmg%\tpeakLongG\tpeakLatG\tmaxDev");
        int pn = 0;
        foreach (var (a, b, cg) in pairs)
        {
            int na = NearNode(Pos(a)), nb = NearNode(Pos(b));
            List<int> prevPath = null;
            foreach (var rough in new[] { false, true })
            {
                pn++; Status = $"pairs {pn}/10 {a}->{b} {(rough ? "short" : "big")}";
                var path = Path(na, nb, rough); string tag = rough ? "지름길" : "큰길";
                if (path == null) { sb.AppendLine($"{a}->{b}\t{tag}\t-\t-\t경로없음"); continue; }
                bool same = prevPath != null && prevPath.SequenceEqual(path); prevPath = path;
                var P = path.Select(i => nodes[i]).ToList();
                // 갈림목 점프로 생긴 0거리 중복 제거
                var Q = new List<Vector3> { P[0] }; foreach (var p in P.Skip(1)) if (Vector3.Distance(p, Q[Q.Count - 1]) > .3f) Q.Add(p);
                var half = new List<float>(); float roughM = 0; var halfOf = path.Select(i => roads[road[i]].width * .5f).ToList();
                for (int i = 0; i < Q.Count; i++) half.Add(3f);
                for (int i = 1; i < path.Count; i++) if (roads[road[path[i]]].kind == "shortcut") roughM += Vector3.Distance(nodes[path[i]], nodes[path[i - 1]]);
                float m = 0; for (int i = 1; i < Q.Count; i++) m += Vector3.Distance(Q[i], Q[i - 1]);
                if (same) { sb.AppendLine($"{a}->{b}\t{tag}\t{m:F0}\t{roughM:F0}\t큰길과 같은 경로(갈래 없음)"); continue; }
                Place(Q[0], Q[Mathf.Min(3, Q.Count - 1)] - Q[0]);
                if (cargo != null) { if (System.Enum.TryParse<CargoType>(cg, out var ct)) cargo.SetCargoType(ct); cargo.Repair(); cargo.StartGracePeriod(1.5f); }
                yield return new WaitForSeconds(1.6f);
                if (cargo != null) cargo.Repair();
                var res = new RunResult();
                yield return Drive(Q, half, res);
                float dmg = cargo != null ? (1f - cargo.IntegrityPercent) * 100f : -1f;
                sb.AppendLine($"{a}->{b}\t{tag}\t{m:F0}\t{roughM:F0}\t{res.result}\t{res.time:F1}\t{dmg:F0}\t{res.peakLong:F2}\t{res.peakLat:F2}\t{res.maxDev:F1}");
            }
        }
    }
}
