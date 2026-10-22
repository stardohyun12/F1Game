using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PitCrew.TownGen;

/// <summary>
/// 조각 15a(D-116 윗길 고리): 씬 차도 그래프 `carGraph` + 경로 다양성(R1·R2·R5·R6) + 큰길 경사 위반 목록.
/// 그래프 = SplineTown 이 실제로 그린 길(res.edges: 큰길·좁은 차로). 좁은 차로(골목)는 코어에서 큰길과 노드를 공유하지 않으므로(입구는 기하로만 붙음)
/// 골목 끝점에서 6m 안 다른 길 점에 나눔 노드를 넣어 잇는다(lane_mouth). 간선 비용 = 중심선 길이.
/// 경로 재는 법(스펙 §3.3 고정): 경로 1 = 최단, 그 간선 비용 ×5 → 다시 최단(최대 4개). 서로 다른 경로 = 경로 1 과 겹침 ≤ 50% 그리고 길이 ≤ 경로 1 × 1.3.
/// 성격 = 경로에서 가장 긴 종류(primary_SS163 / upper(윗길 가지) / 그 밖 OSM 종류 / lane / connector / entry7b / boundaryJoin).
/// </summary>
public static class TownGenCarGraph
{
    /// <summary>D-116 상자 경계 잇기: Via Primo Maggio 서쪽 끝 ↔ Via Miramare 서쪽 끝(실제 공유 노드 40.6171748, 14.5689036 는 상자 서쪽 약 8m 밖 — 그 점을 지나는 실제 길은 약 54m).</summary>
    public static readonly Vec2 JoinA = new Vec2(21.78, 464.44), JoinB = new Vec2(9.66, 445.94);
    public const long JoinWayId = -116, EntryJoinWayId = -7, MiramareWay = 536405704, PrimoMaggioWay = 522140502;
    const double MouthReach = 6, NearRaw = 6, NearUpper = 4, NearJoin = 8;

    /// <summary>SS163(primary) 를 뺀 차도를 같은 좌표 점으로 묶었을 때 Via Primo Maggio 가 든 덩어리 = 윗길 가지(OSM 약 9.85km).</summary>
    public static HashSet<long> UpperBranchWays(JObject nd) => BranchWays(nd, PrimoMaggioWay);

    /// <summary>SS163(primary) 를 뺀 차도 덩어리 중 way 가 든 것의 way 들.</summary>
    public static HashSet<long> BranchWays(JObject nd, long seedWay)
    {
        var car = new HashSet<string> { "secondary", "residential", "unclassified", "living_street", "service" };
        var segs = nd["segments"].Where(sg => car.Contains(sg["highway"].ToString())).ToList();
        var par = new Dictionary<string, string>();
        string K(JToken p) => $"{Math.Round(p[0].Value<double>() * 2) / 2},{Math.Round(p[1].Value<double>() * 2) / 2}";
        string F(string x) { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; }
        foreach (var sg in segs) foreach (var p in sg["points_m"]) { var k = K(p); if (!par.ContainsKey(k)) par[k] = k; }
        foreach (var sg in segs) { var P = sg["points_m"].ToList(); for (int i = 1; i < P.Count; i++) { var a = F(K(P[i - 1])); var b = F(K(P[i])); if (a != b) par[a] = b; } }
        var pm = segs.FirstOrDefault(sg => sg["osm_way"].Value<long>() == seedWay); if (pm == null) return new HashSet<long>();
        var root = F(K(pm["points_m"][0]));
        return new HashSet<long>(segs.Where(sg => F(K(sg["points_m"][0])) == root).Select(sg => sg["osm_way"].Value<long>()));
    }

    static double SegDist(Vec2 p, Vec2 a, Vec2 b, out double u)
    {
        double dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz; u = L2 > 1e-12 ? Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / L2)) : 0;
        double qx = a.x + dx * u - p.x, qz = a.z + dz * u - p.z; return Math.Sqrt(qx * qx + qz * qz);
    }
    static double SegDist(Vec2 p, Vec2 a, Vec2 b) => SegDist(p, a, b, out _);
    static bool NearPoly(Vec2 p, List<Vec2> pl, double r) { for (int k = 0; k + 1 < pl.Count; k++) if (SegDist(p, pl[k], pl[k + 1]) <= r) return true; return false; }

    class G { public int a, b, src; public double L; public Dictionary<string, double> by = new Dictionary<string, double>(); public string Top => by.OrderByDescending(kv => kv.Value).First().Key; }

    public static JObject Build(SplineTownResult res, SplineTownSettings s, List<List<Vec2>> upperLines, List<Vec2> entryJoin, Vec2 harbor, Vec2 conca, Vec2 upperVillage, double coreR)
    {
        var E = res.edges;
        var joinLine = new List<Vec2> { JoinA, JoinB };
        var newConn = res.connectorRaw;   // 이 빌드에서 생성기가 새로 깐 잇기(지도에 없음)
        // 선분 종류: 잇기 원본 6m 안 → connector, 7b 입구 잇기 → entry7b, 경계 잇기 8m 안 → boundaryJoin, 윗길 OSM 선 4m 안 → upper, 아니면 OSM 종류
        string SegKind(SplineEdge e, Vec2 m)
        {
            if (e.alley) return "lane";
            if (NearPoly(m, joinLine, NearJoin)) return "boundaryJoin";
            if (entryJoin != null && NearPoly(m, entryJoin, NearRaw)) return "entry7b";
            if (newConn.Any(pl => NearPoly(m, pl, NearRaw))) return "connector";
            if (upperLines.Any(pl => NearPoly(m, pl, NearUpper))) return "upper";
            return e.highway == "primary" ? "primary_SS163" : e.highway;
        }
        var arc = new List<double[]>(); var segKind = new List<string[]>();
        foreach (var e in E)
        {
            var a = new double[e.pts.Count]; var k = new string[Math.Max(0, e.pts.Count - 1)];
            for (int i = 1; i < e.pts.Count; i++) { a[i] = a[i - 1] + SplineEdge.Dist(e.pts[i - 1], e.pts[i]); k[i - 1] = SegKind(e, new Vec2((e.pts[i - 1].x + e.pts[i].x) / 2, (e.pts[i - 1].z + e.pts[i].z) / 2)); }
            arc.Add(a); segKind.Add(k);
        }
        // 골목 끝 → 다른 길(6m 안) 나눔 노드
        var nodeP = new List<Vec2>(res.nodes); var splits = E.Select(_ => new List<(double s, int node)>()).ToList(); var mouths = new List<(int a, int b, double L)>();
        var degRaw = new int[res.nodes.Count]; foreach (var e in E) { degRaw[e.a]++; degRaw[e.b]++; }
        for (int ei = 0; ei < E.Count; ei++)
        {
            if (!E[ei].alley) continue;
            foreach (int end in new[] { E[ei].a, E[ei].b })
            {
                if (degRaw[end] > 1) continue;   // 이미 다른 골목과 이어진 끝
                var p = res.nodes[end]; double best = MouthReach; int be = -1, bi = 0; double bu = 0;
                for (int fj = 0; fj < E.Count; fj++)
                {
                    if (fj == ei) continue; var f = E[fj];
                    for (int i = 0; i + 1 < f.pts.Count; i++) { double d = SegDist(p, f.pts[i], f.pts[i + 1], out double u); if (d < best) { best = d; be = fj; bi = i; bu = u; } }
                }
                if (be < 0) continue;
                double sAt = arc[be][bi] + (arc[be][bi + 1] - arc[be][bi]) * bu; var f2 = E[be];
                int nn = nodeP.Count; nodeP.Add(new Vec2(f2.pts[bi].x + (f2.pts[bi + 1].x - f2.pts[bi].x) * bu, f2.pts[bi].z + (f2.pts[bi + 1].z - f2.pts[bi].z) * bu));
                splits[be].Add((sAt, nn)); mouths.Add((end, nn, best));
            }
        }
        // 질문 점(항구·콘카 광장·윗길 마을)도 가장 가까운 길 점에 나눔 노드 — 교차점 노드까지 수십~백 m 떨어져 잡히던 것
        var query = new Dictionary<string, (int node, double d)>();
        foreach (var (qn, qp) in new[] { ("harbor", harbor), ("conca", conca), ("upper", upperVillage) })
        {
            double best = double.MaxValue; int be = -1, bi = 0; double bu = 0;
            for (int fj = 0; fj < E.Count; fj++) { var f = E[fj]; for (int i = 0; i + 1 < f.pts.Count; i++) { double d = SegDist(qp, f.pts[i], f.pts[i + 1], out double u); if (d < best) { best = d; be = fj; bi = i; bu = u; } } }
            if (be < 0) continue; var f2 = E[be]; int nn = nodeP.Count;
            nodeP.Add(new Vec2(f2.pts[bi].x + (f2.pts[bi + 1].x - f2.pts[bi].x) * bu, f2.pts[bi].z + (f2.pts[bi + 1].z - f2.pts[bi].z) * bu));
            splits[be].Add((arc[be][bi] + (arc[be][bi + 1] - arc[be][bi]) * bu, nn)); query[qn] = (nn, best);
        }
        var g = new List<G>();
        for (int ei = 0; ei < E.Count; ei++)
        {
            var e = E[ei]; var cut = new List<(double s, int node)> { (0, e.a) }; cut.AddRange(splits[ei].OrderBy(x => x.s)); cut.Add((arc[ei][arc[ei].Length - 1], e.b));
            for (int c = 0; c + 1 < cut.Count; c++)
            {
                double s0 = cut[c].s, s1 = cut[c + 1].s; var ge = new G { a = cut[c].node, b = cut[c + 1].node, src = ei, L = Math.Max(0, s1 - s0) };
                for (int i = 0; i + 1 < e.pts.Count; i++) { double ov = Math.Min(s1, arc[ei][i + 1]) - Math.Max(s0, arc[ei][i]); if (ov > 0) ge.by[segKind[ei][i]] = (ge.by.TryGetValue(segKind[ei][i], out var v) ? v : 0) + ov; }
                if (ge.by.Count == 0) ge.by[e.alley ? "lane" : e.highway] = ge.L;
                g.Add(ge);
            }
        }
        foreach (var (a, b, L) in mouths) { var ge = new G { a = a, b = b, src = -1, L = L }; ge.by["lane_mouth"] = L; g.Add(ge); }

        int NN = nodeP.Count; var adj = new List<int>[NN]; for (int i = 0; i < NN; i++) adj[i] = new List<int>();
        for (int k = 0; k < g.Count; k++) { adj[g[k].a].Add(k); if (g[k].b != g[k].a) adj[g[k].b].Add(k); }
        int Nearest(Vec2 p) { int b = -1; double d = double.MaxValue; for (int i = 0; i < NN; i++) { if (adj[i].Count == 0) continue; double dd = SplineEdge.Dist(nodeP[i], p); if (dd < d) { d = dd; b = i; } } return b; }
        // 덩어리
        var comp = Enumerable.Repeat(-1, NN).ToArray(); int C = 0; var compLen = new List<double>();
        for (int i = 0; i < NN; i++)
        {
            if (comp[i] >= 0 || adj[i].Count == 0) continue; var st = new Stack<int>(); st.Push(i); comp[i] = C;
            while (st.Count > 0) { int u = st.Pop(); foreach (int k in adj[u]) { int v = g[k].a == u ? g[k].b : g[k].a; if (comp[v] < 0) { comp[v] = C; st.Push(v); } } }
            compLen.Add(0); C++;
        }
        foreach (var ge in g) compLen[comp[ge.a]] += ge.L;
        List<int> Dij(int src, int dst, Dictionary<int, double> pen)
        {
            var D = Enumerable.Repeat(double.MaxValue, NN).ToArray(); var P = Enumerable.Repeat(-1, NN).ToArray(); D[src] = 0;
            var q = new SortedSet<(double, int)> { (0, src) };
            while (q.Count > 0)
            {
                var (d, u) = q.Min; q.Remove(q.Min); if (u == dst) break;
                foreach (int k in adj[u])
                {
                    int v = g[k].a == u ? g[k].b : g[k].a; double w = g[k].L * (pen.TryGetValue(k, out var f) ? f : 1);
                    if (d + w < D[v] - 1e-9) { if (D[v] < double.MaxValue) q.Remove((D[v], v)); D[v] = d + w; P[v] = k; q.Add((D[v], v)); }
                }
            }
            if (D[dst] == double.MaxValue) return null;
            var path = new List<int>(); int x = dst; while (x != src) { int k = P[x]; path.Add(k); x = g[k].a == x ? g[k].b : g[k].a; }
            path.Reverse(); return path;
        }
        JObject Alts(string name, string qa, string qb, Vec2 A, Vec2 B)
        {
            int sN = query.TryGetValue(qa, out var q1) ? q1.node : Nearest(A), tN = query.TryGetValue(qb, out var q2) ? q2.node : Nearest(B); var pen = new Dictionary<int, double>(); var outR = new List<(List<int> p, double L, double ov, Dictionary<string, double> by)>();
            for (int k = 0; k < 4; k++)
            {
                var p = Dij(sN, tN, pen); if (p == null) break;
                double L = p.Sum(i => g[i].L); var by = new Dictionary<string, double>();
                foreach (int i in p) foreach (var kv in g[i].by) by[kv.Key] = (by.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                double ov = 0; if (outR.Count > 0) { var f = new HashSet<int>(outR[0].p); ov = p.Where(f.Contains).Sum(i => g[i].L) / L; }
                if (!outR.Any(o => o.p.SequenceEqual(p))) outR.Add((p, L, ov, by));
                foreach (int i in p) pen[i] = (pen.TryGetValue(i, out var f2) ? f2 : 1) * 5;
            }
            var arr = new JArray(); int distinct = 0; var chars = new List<string>();
            for (int i = 0; i < outR.Count; i++)
            {
                var r = outR[i]; bool ok = i == 0 || (r.ov <= 0.5 && r.L <= outR[0].L * 1.3); string top = r.by.OrderByDescending(kv => kv.Value).First().Key;
                if (ok) { distinct++; if (!chars.Contains(top)) chars.Add(top); }
                arr.Add(new JObject { ["length_m"] = Math.Round(r.L, 1), ["vsRoute1_pct"] = Math.Round((r.L / outR[0].L - 1) * 100, 1), ["overlapWithRoute1"] = Math.Round(r.ov, 3), ["distinct"] = ok, ["character"] = top,
                    ["byKind_m"] = new JObject(r.by.OrderByDescending(kv => kv.Value).Select(kv => new JProperty(kv.Key, Math.Round(kv.Value, 0)))), ["sceneEdges"] = new JArray(r.p.Select(j => g[j].src).Where(x => x >= 0).Distinct()), ["via"] = new JArray(r.p.Where((_, j) => j % Math.Max(1, r.p.Count / 12) == 0).Select(j => new JArray(Math.Round(nodeP[g[j].a].x), Math.Round(nodeP[g[j].a].z)))) });
            }
            return new JObject { ["pair"] = name, ["from"] = new JArray(Math.Round(A.x, 1), Math.Round(A.z, 1)), ["to"] = new JArray(Math.Round(B.x, 1), Math.Round(B.z, 1)),
                ["snap_m"] = new JArray(Math.Round(SplineEdge.Dist(nodeP[sN], A), 1), Math.Round(SplineEdge.Dist(nodeP[tN], B), 1)), ["sameComponent"] = comp[sN] == comp[tN], ["distinctRoutes"] = distinct, ["distinctCharacters"] = new JArray(chars), ["routes"] = arr };
        }

        // R5: 항구 반경 안(양 끝이 반경 안) 간선 부분 그래프의 고리 수 = E − V + C
        var core = Enumerable.Range(0, g.Count).Where(k => SplineEdge.Dist(nodeP[g[k].a], harbor) <= coreR && SplineEdge.Dist(nodeP[g[k].b], harbor) <= coreR && (g[k].src < 0 || E[g[k].src].pts.All(p => SplineEdge.Dist(p, harbor) <= coreR))).ToList();
        int coreLoops; { var vs = new HashSet<int>(core.SelectMany(k => new[] { g[k].a, g[k].b })); var par = vs.ToDictionary(v => v, v => v); int Fd(int x) { while (par[x] != x) x = par[x] = par[par[x]]; return x; } foreach (int k in core) { int a = Fd(g[k].a), b = Fd(g[k].b); if (a != b) par[a] = b; } coreLoops = core.Count - vs.Count + vs.Select(Fd).Distinct().Count(); }

        // 경사(§2.9: OSM 큰길 8% · 잇기 10% · 패드/헤어핀 5% · 골목 12%) — 중심선 점 사이 구간마다
        var gradeV = new JArray(); int gradeSegs = 0, gradeUpperSegs = 0, overUpper = 0, overAll = 0; double maxUpper = 0;
        double upCutMax = 0; var upCutAt = new JArray(); int upCutOver = 0;
        for (int ei = 0; ei < E.Count; ei++)
        {
            var e = E[ei];
            for (int i = 0; i + 1 < e.pts.Count; i++)
            {
                double L = SplineEdge.Dist(e.pts[i], e.pts[i + 1]); if (L < 0.5) continue; bool up = segKind[ei][i] == "upper" || segKind[ei][i] == "boundaryJoin";
                double gr = Math.Abs(e.y[i + 1] - e.y[i]) / L; bool pad = e.pad.Count > i + 1 && (e.pad[i] || e.pad[i + 1]); bool hp = e.hairpin.Count > i + 1 && (e.hairpin[i] || e.hairpin[i + 1]);
                double lim = e.alley ? s.alleyMaxGrade : (pad || hp) ? s.hairpinMaxGrade : e.connector ? s.generatedMaxGrade : s.mainMaxGrade;
                gradeSegs++; if (up) { gradeUpperSegs++; maxUpper = Math.Max(maxUpper, gr); }
                if (gr > lim + 1e-4) { overAll++; if (up) overUpper++; if (gradeV.Count < 200) gradeV.Add(new JObject { ["edge"] = ei, ["kind"] = segKind[ei][i], ["at"] = new JArray(Math.Round(e.pts[i].x, 1), Math.Round(e.y[i], 2), Math.Round(e.pts[i].z, 1)), ["grade_pct"] = Math.Round(gr * 100, 2), ["limit_pct"] = Math.Round(lim * 100, 1) }); }
                if (up) { double d = Math.Abs(e.y[i] - res.terrainOriginal.Surface(e.pts[i].x, e.pts[i].z)); upCutMax = Math.Max(upCutMax, d); if (d > s.maxCutFillMeters) { upCutOver++; if (upCutAt.Count < 30) upCutAt.Add(new JArray(Math.Round(e.pts[i].x, 1), Math.Round(e.pts[i].z, 1), Math.Round(d, 2))); } }
            }
        }

        double joinDist = double.MaxValue; var jm = new Vec2((JoinA.x + JoinB.x) / 2, (JoinA.z + JoinB.z) / 2);
        foreach (var e in E) for (int i = 0; i + 1 < e.pts.Count; i++) joinDist = Math.Min(joinDist, SegDist(jm, e.pts[i], e.pts[i + 1]));
        double KindLen(string k) => g.Sum(x => x.by.TryGetValue(k, out var v) ? v : 0);

        var nodes = new JArray(); var usedN = new HashSet<int>(g.SelectMany(x => new[] { x.a, x.b }));
        for (int i = 0; i < NN; i++) if (usedN.Contains(i)) nodes.Add(new JObject { ["id"] = i, ["x"] = Math.Round(nodeP[i].x, 2), ["z"] = Math.Round(nodeP[i].z, 2), ["degree"] = adj[i].Count, ["virtual"] = i >= res.nodes.Count });
        var edges = new JArray();
        for (int k = 0; k < g.Count; k++)
        {
            var ge = g[k]; var pl = new JArray();
            if (ge.src >= 0) { var e = E[ge.src]; double acc = 1e9; for (int i = 0; i < e.pts.Count; i++) { if (i > 0) acc += SplineEdge.Dist(e.pts[i - 1], e.pts[i]); if (acc >= 8 || i == e.pts.Count - 1) { pl.Add(new JArray(Math.Round(e.pts[i].x, 1), Math.Round(e.y[i], 2), Math.Round(e.pts[i].z, 1))); acc = 0; } } }
            edges.Add(new JObject { ["id"] = k, ["a"] = ge.a, ["b"] = ge.b, ["length_m"] = Math.Round(ge.L, 1), ["kind"] = ge.Top, ["byKind_m"] = new JObject(ge.by.Select(kv => new JProperty(kv.Key, Math.Round(kv.Value, 1)))),
                ["sceneEdge"] = ge.src, ["sceneEdgeHighway"] = ge.src >= 0 ? E[ge.src].highway : "lane_mouth", ["pts_xyz_8m_sceneEdge"] = pl });
        }
        return new JObject
        {
            ["rule"] = "scene car graph = SplineTown drawn roads (main roads + narrow lanes). Narrow-lane ends are linked to the nearest road point within 6m (virtual split node, edge kind lane_mouth). Edge cost = centreline length. Segment kinds: connector (within 6m of a generator join drawn this build — not on the map), entry7b (the kept 7b Conca entry join), boundaryJoin (D-116 box-edge join), upper (within 4m of upper-branch OSM lines), primary_SS163, other OSM highway, lane. Edges with several kinds carry byKind_m. pts are the parent scene edge polyline (8m).",
            ["nodeCount"] = nodes.Count, ["edgeCount"] = g.Count, ["components"] = C, ["componentLengths_m"] = new JArray(compLen.OrderByDescending(x => x).Select(x => Math.Round(x, 0))), ["total_m"] = Math.Round(g.Sum(x => x.L), 1),
            ["byKind_m"] = new JObject(g.SelectMany(x => x.by).GroupBy(kv => kv.Key).OrderByDescending(gr => gr.Sum(kv => kv.Value)).Select(gr => new JProperty(gr.Key, Math.Round(gr.Sum(kv => kv.Value), 0)))),
            ["R6_notOnMap"] = new JObject
            {
                ["rule"] = "joins not on the real map. R6 counts the D-116 box-edge join only; the 7b Conca entry join and generator connectors are listed separately",
                ["boundaryJoin_inputStraight_m"] = Math.Round(SplineEdge.Dist(JoinA, JoinB), 1), ["boundaryJoin_drawn_m"] = Math.Round(KindLen("boundaryJoin"), 1), ["boundaryJoin_graphDistToMidpoint_m"] = Math.Round(joinDist, 2),
                ["entry7b_input_m"] = entryJoin == null ? 0 : Math.Round(SplineEdge.PolyLength(entryJoin), 1), ["entry7b_drawn_m"] = Math.Round(KindLen("entry7b"), 1),
                ["generatorConnectors"] = new JArray(newConn.Select(pl => new JObject { ["length_m"] = Math.Round(SplineEdge.PolyLength(pl), 1), ["from"] = new JArray(Math.Round(pl[0].x), Math.Round(pl[0].z)), ["to"] = new JArray(Math.Round(pl[pl.Count - 1].x), Math.Round(pl[pl.Count - 1].z)) })),
                ["generatorConnectors_m"] = Math.Round(newConn.Sum(SplineEdge.PolyLength), 1),
            },
            ["routes"] = new JArray(Alts("harbor→conca_plaza", "harbor", "conca", harbor, conca), Alts("harbor→upper_village", "harbor", "upper", harbor, upperVillage)),
            ["R5_coreLoops"] = new JObject { ["rule"] = $"cycle rank (E − V + C) of graph edges lying fully within {coreR}m of the harbour point (lanes linked)", ["edges"] = core.Count, ["loops"] = coreLoops },
            ["grade"] = new JObject { ["rule"] = "§2.9 limits per centreline segment: OSM main 8%, generated connector 10%, hairpin/junction pad 5%, narrow lane 12%. upper = segments of kind upper/boundaryJoin", ["segments"] = gradeSegs, ["upperSegments"] = gradeUpperSegs, ["over"] = overAll, ["overUpper"] = overUpper, ["upperMax_pct"] = Math.Round(maxUpper * 100, 2), ["violations"] = gradeV },
            ["upperCutFill"] = new JObject { ["rule"] = $"|road height − original terrain| on upper-road centreline points, limit {s.maxCutFillMeters}m (M7)", ["max_m"] = Math.Round(upCutMax, 2), ["over"] = upCutOver, ["overAt"] = upCutAt },
            ["nodes"] = nodes, ["edges"] = edges,
        };
    }
}
