using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using PitCrew.TownGen;

/// <summary>
/// 구조 조각 14(도현 승인 A 2026-10-20, 리서치 2 1순위 — 아말피 중심 400m 만): 마을 길을 한 장 면으로.
/// 좁은 차로·보행길(조각 13 틈 폭)·광장·계단 디딤을 buffer 합집합 → 안쪽 모서리 둥글리기(가설 1.5m) → 건물·큰길 단면·계단 디딤을 뺀 면.
/// 2D 계산은 tools/town-gen/road_sheet.py(shapely)가 하고, 여기는 입력을 내보내고 결과 삼각형에 높이를 준다.
/// - 차로 부분(아스팔트) 높이 = 그 골목 종단 높이 + 0.06, 보행 부분(포장) = 지형 + 0.06(삼각형이 2m 지형 삼각형 안에 있어 정확).
/// - 보행 교차점(차수 ≥ 3)은 지형을 원판 평균 높이로 누른다(±0.3m 까지, 건물·계단·차로 둘레는 안 건드림) → 면도 따라 평평.
/// - 중심 반경 안 골목 돌·어깨, 보행 띠, 광장 칸은 이 면이 대신한다(계단 디딤은 그대로).
/// </summary>
public static class TownGenRoadSheet
{
    public const double Fillet = 1.5, Lift = 0.06, FlattenCap = 0.3;

    internal class Result
    {
        public TownGenSpline.MeshBuilder lane = new TownGenSpline.MeshBuilder(), walk = new TownGenSpline.MeshBuilder();
        public List<(string kind, string way, Vec2[] poly)> surf = new List<(string, string, Vec2[])>();
        public List<Vec2> finePts = new List<Vec2>();
        public JObject stats;
    }

    /// <summary>중심 반경 안 골목 선분이면 true — 그 돌·어깨는 면이 대신 그린다.</summary>
    public static bool CoreLaneSeg(SplineEdge e, int i, Vec2 c, double R) => SplineEdge.Dist(e.pts[i], c) <= R && SplineEdge.Dist(e.pts[i + 1], c) <= R;

    static JArray P(Vec2 p) => new JArray(Math.Round(p.x, 3), Math.Round(p.z, 3));
    static JArray Ring(IEnumerable<Vec2> r) => new JArray(r.Select(P));

    internal static Result Build(SplineTownResult res, SplineTownSettings s, List<TownGenTownPaths.PathWay> paths, List<TownGenTownPaths.Area> areas,
        List<(string kind, string way, Vec2[] poly)> v13Surf, List<TownGenOsmBuildings.B> placed, Vec2 c, double R, double seaLevel, string outDir)
    {
        var t = res.terrain;
        Directory.CreateDirectory(outDir);
        var lanes = res.edges.Select((e, i) => (e, i)).Where(x => x.e.alley && x.e.pts.Any(p => SplineEdge.Dist(p, c) <= R + 10)).ToList();
        var v13 = new JArray();
        foreach (var (kind, way, poly) in v13Surf) v13.Add(new JObject { ["way"] = $"{kind}:{way}", ["poly"] = Ring(poly) });
        foreach (var (e, ei) in lanes)
            for (int i = 0; i + 1 < e.pts.Count; i++)
            {
                if (!CoreLaneSeg(e, i, c, R)) continue;
                var q = TownGenGapWidth.Quad(e.pts[i], e.pts[i + 1], TownGenGapWidth.Nrm(e.pts, i), TownGenGapWidth.Nrm(e.pts, i + 1), e.AlleyHalf(i, s.alleyHalfWidthMeters), e.AlleyHalf(i + 1, s.alleyHalfWidthMeters));
                v13.Add(new JObject { ["way"] = $"lane:edge{ei}", ["poly"] = Ring(q) });
            }
        var mains = res.edges.Where(e => !e.alley && e.pts.Any(p => SplineEdge.Dist(p, c) <= R + 40)).ToList();
        var input = new JObject
        {
            ["center"] = P(c), ["radius"] = R, ["fillet"] = Fillet, ["terrainStep"] = t.step,
            ["lanes"] = new JArray(lanes.Select(x => new JObject { ["id"] = x.i, ["pts"] = Ring(x.e.pts), ["half"] = new JArray(Enumerable.Range(0, x.e.pts.Count).Select(i => Math.Round(x.e.AlleyHalf(i, s.alleyHalfWidthMeters), 3))) })),
            ["paths"] = new JArray(paths.Select(w => new JObject
            {
                ["id"] = $"{w.highway}:{w.osm}", ["highway"] = w.highway, ["pts"] = Ring(w.pts),
                ["half"] = new JArray(Enumerable.Range(0, w.pts.Count).Select(i => Math.Round(Math.Max(0.35, w.fitHalf != null ? w.fitHalf[Math.Min(i, w.fitHalf.Count - 1)] : w.width / 2), 3))),
                ["sea"] = new JArray(w.pts.Select(p => t.Surface(p.x, p.z) < seaLevel - 0.5)),
            })),
            ["plazas"] = new JArray(areas.Select(a => new JObject { ["id"] = a.osm, ["ring"] = Ring(a.ring) })),
            ["treads"] = new JArray(v13Surf.Where(x => x.kind == "steps").Select(x => new JObject { ["way"] = "steps:" + x.way, ["ring"] = Ring(x.poly) })), ["buildingClear"] = 0.01,
            ["buildings"] = new JArray(placed.Where(b => SplineEdge.Dist(b.c, c) <= R + 80).Select(b => Ring(b.ring))),
            ["mains"] = new JArray(mains.Select(e => new JObject { ["pts"] = Ring(TownGenSpline.ClippedPolyline(e)), ["half"] = s.HalfSection })),
            ["junctionRings"] = new JArray(res.junctionLayouts.Where(jl => SplineEdge.Dist(new Vec2(res.nodes[jl.node].x, res.nodes[jl.node].z), c) <= R + 40).Select(jl => Ring(jl.ringP))),
            ["junctionGrow"] = s.HalfSection - s.HalfCarriageway,
            ["v13"] = v13,
        };
        string inPath = Path.Combine(outDir, "road_sheet_in.json"), outPath = Path.Combine(outDir, "road_sheet_out.json");
        File.WriteAllText(inPath, input.ToString(Newtonsoft.Json.Formatting.None), new UTF8Encoding(false));
        RunPython(Path.Combine(TownGenRunner.ToolsDir, "road_sheet.py"), inPath, outPath);
        var o = JObject.Parse(File.ReadAllText(outPath));

        // ---- 보행 교차점 평탄화: 지형 격자점(건물·계단·차로·큰길 둘레 제외)을 원판 평균으로, ±0.3m 까지, 바깥 2m 에 걸쳐 풀어 줌
        var bIdx = new TownGenTownPaths.BIndex(placed, 2);
        var treadC = v13Surf.Where(x => x.kind == "steps").Select(x => new Vec2(x.poly.Average(v => v.x), x.poly.Average(v => v.z))).ToList();
        var laneSegs = new List<(Vec2 a, Vec2 b, double ya, double yb, double ha, double hb)>();
        foreach (var (e, _) in lanes) for (int i = 0; i + 1 < e.pts.Count; i++) laneSegs.Add((e.pts[i], e.pts[i + 1], e.y[i], e.y[i + 1], e.AlleyHalf(i, s.alleyHalfWidthMeters), e.AlleyHalf(i + 1, s.alleyHalfWidthMeters)));
        var mainPolys = mains.Select(TownGenSpline.ClippedPolyline).ToList();
        bool Protected(Vec2 p)
        {
            if (bIdx.Clearance(p, 0.6) < 0.6 || bIdx.InsideOf(p) != null) return true;
            if (treadC.Any(q => SplineEdge.Dist(q, p) < 1.5)) return true;
            foreach (var sg in laneSegs) { if (SegDist(p, sg.a, sg.b, out _) <= Math.Max(sg.ha, sg.hb) + s.alleyShoulderMeters + 0.5) return true; }
            foreach (var pl in mainPolys) if (pl.Count >= 2 && SplineTown.DistToPoly(p, pl) <= s.HalfShoulder + 0.5) return true;
            return false;
        }
        var junc = new JArray(); int flatJ = 0, capped = 0, moved = 0; double maxMove = 0; var spreadBefore = new List<double>(); var spreadAfter = new List<double>();
        foreach (var j in o["junctions"])
        {
            double jx = j[0].Value<double>(), jz = j[1].Value<double>(), r = j[2].Value<double>();
            var pts = new List<(int k, double d)>();
            for (int gj = Math.Max(0, (int)((jz - r - 2) / t.step)); gj <= Math.Min(t.nz - 1, (int)Math.Ceiling((jz + r + 2) / t.step)); gj++)
                for (int gi = Math.Max(0, (int)((jx - r - 2) / t.step)); gi <= Math.Min(t.nx - 1, (int)Math.Ceiling((jx + r + 2) / t.step)); gi++)
                {
                    var p = new Vec2(gi * t.step, gj * t.step); double d = SplineEdge.Dist(p, new Vec2(jx, jz));
                    if (d <= r + 2 && !Protected(p)) pts.Add((gj * t.nx + gi, d));
                }
            var inner = pts.Where(x => x.d <= r).ToList();
            if (inner.Count < 2) { junc.Add(new JArray(jx, jz, r, "skipped_protected")); continue; }
            double before = inner.Max(x => t.h[x.k]) - inner.Min(x => t.h[x.k]); double target = inner.Average(x => t.h[x.k]);
            bool cap = false;
            foreach (var (k, d) in pts)
            {
                double w = d <= r ? 1 : 1 - (d - r) / 2, dh = target - t.h[k];
                if (Math.Abs(dh) > FlattenCap) { dh = Math.Sign(dh) * FlattenCap; cap = true; }
                dh *= w; if (Math.Abs(dh) > 1e-4) { t.h[k] += dh; moved++; maxMove = Math.Max(maxMove, Math.Abs(dh)); }
            }
            double after = inner.Max(x => t.h[x.k]) - inner.Min(x => t.h[x.k]);
            spreadBefore.Add(before); spreadAfter.Add(after); flatJ++; if (cap) capped++;
            junc.Add(new JArray(Math.Round(jx, 1), Math.Round(jz, 1), r, Math.Round(before, 2), Math.Round(after, 2)));
        }

        // ---- 삼각형 → 메시
        var r0 = new Result();
        double LaneY(Vec2 p)
        {
            double best = double.MaxValue, y = t.Surface(p.x, p.z) + 0.1;
            foreach (var sg in laneSegs)
            {
                double d = SegDist(p, sg.a, sg.b, out double u);
                if (d < best) { best = d; y = sg.ya + (sg.yb - sg.ya) * u; }
            }
            return y;
        }
        // 차로 높이 찾기용 선분 해시(16m 칸)
        var lh = new Dictionary<(int, int), List<int>>();
        for (int k = 0; k < laneSegs.Count; k++)
        {
            var sg = laneSegs[k];
            for (int cx = (int)Math.Floor((Math.Min(sg.a.x, sg.b.x) - 8) / 16); cx <= (int)Math.Floor((Math.Max(sg.a.x, sg.b.x) + 8) / 16); cx++)
                for (int cz = (int)Math.Floor((Math.Min(sg.a.z, sg.b.z) - 8) / 16); cz <= (int)Math.Floor((Math.Max(sg.a.z, sg.b.z) + 8) / 16); cz++)
                { if (!lh.TryGetValue((cx, cz), out var l)) lh[(cx, cz)] = l = new List<int>(); l.Add(k); }
        }
        double LaneYFast(Vec2 p)
        {
            if (!lh.TryGetValue(((int)Math.Floor(p.x / 16), (int)Math.Floor(p.z / 16)), out var l)) return LaneY(p);
            double best = double.MaxValue, y = 0;
            foreach (int k in l) { var sg = laneSegs[k]; double d = SegDist(p, sg.a, sg.b, out double u); if (d < best) { best = d; y = sg.ya + (sg.yb - sg.ya) * u; } }
            return y;
        }
        int under = 0; double laneMinAbove = double.MaxValue, walkEdgeStepMax = 0; int edgeV = 0, edgeStep10 = 0, edgeStep5 = 0;
        // 차로 가장자리에서 Blend m 안 보행 꼭짓점: 차로 높이 → 지형으로 이어 붙임(지형 아래로는 안 감) — 2m 격자 지형이 차로 끝에서 차로 높이와 어긋나 단차가 생기던 것(첫 빌드 최대 0.40m)
        const double Blend = 1.5;
        double WalkY(Vec2 p, double g)
        {
            if (!lh.TryGetValue(((int)Math.Floor(p.x / 16), (int)Math.Floor(p.z / 16)), out var l)) return g + Lift;
            double best = double.MaxValue, ly = 0;
            foreach (int k in l) { var sg = laneSegs[k]; double d = SegDist(p, sg.a, sg.b, out double u), h = sg.ha + (sg.hb - sg.ha) * u; if (d - h < best) { best = d - h; ly = sg.ya + (sg.yb - sg.ya) * u; } }
            if (best >= Blend) return g + Lift;
            double w = Math.Max(0, best) / Blend; return Math.Max(g + 0.02, ly * (1 - w) + g * w + Lift);
        }
        void Emit(JToken tris, TownGenSpline.MeshBuilder mb, bool isLane)
        {
            foreach (var tr in tris)
            {
                var v = new Vector3[3]; var poly = new Vec2[3];
                for (int q = 0; q < 3; q++)
                {
                    double x = tr[q * 2].Value<double>(), z = tr[q * 2 + 1].Value<double>(); poly[q] = new Vec2(x, z);
                    double g = t.Surface(x, z), y = isLane ? LaneYFast(poly[q]) + Lift : WalkY(poly[q], g);
                    if (isLane) { laneMinAbove = Math.Min(laneMinAbove, y - g); if (y < g + 0.01) { under++; y = g + 0.01; } }
                    v[q] = new Vector3((float)x, (float)y, (float)z);
                }
                mb.Tri(v[0], v[1], v[2]);
                r0.surf.Add(("sheet", isLane ? "sheet:lane" : "sheet:walk", poly));
                r0.finePts.Add(new Vec2((poly[0].x + poly[1].x + poly[2].x) / 3, (poly[0].z + poly[1].z + poly[2].z) / 3));
            }
        }
        Emit(o["lane_tris"], r0.lane, true); Emit(o["walk_tris"], r0.walk, false);
        // 차로·보행 경계 단차: 보행 삼각형 꼭짓점 중 차로 가장자리 0.05m 안에 있는 것 — 차로 높이와 보행 높이 차 최대
        foreach (var tr in o["walk_tris"])
            for (int q = 0; q < 3; q++)
            {
                var p = new Vec2(tr[q * 2].Value<double>(), tr[q * 2 + 1].Value<double>());
                if (!lh.TryGetValue(((int)Math.Floor(p.x / 16), (int)Math.Floor(p.z / 16)), out var l)) continue;
                foreach (int k in l)
                {
                    var sg = laneSegs[k]; double d = SegDist(p, sg.a, sg.b, out double u), hw = sg.ha + (sg.hb - sg.ha) * u;
                    if (Math.Abs(d - hw) <= 0.05) { double st = Math.Abs(sg.ya + (sg.yb - sg.ya) * u + Lift - WalkY(p, t.Surface(p.x, p.z))); walkEdgeStepMax = Math.Max(walkEdgeStepMax, st); edgeV++; if (st > 0.1) edgeStep10++; if (st > 0.05) edgeStep5++; break; }
                }
            }
        foreach (var p in o.Properties().Where(p => p.Name.EndsWith("_tris")).ToList()) p.Remove();
        o["heights"] = new JObject
        {
            ["rule"] = $"lane triangles = nearest core lane profile height + {Lift}m; walk triangles = terrain + {Lift}m (vertices inside one 2m terrain triangle → exact)",
            ["laneVerticesBelowTerrain_clamped"] = under, ["laneMinAboveTerrain_m"] = laneMinAbove == double.MaxValue ? (double?)null : Math.Round(laneMinAbove, 3),
            ["laneWalkEdge"] = new JObject { ["rule"] = $"walk vertices on a lane edge (±0.05m): |lane height − walk height|; walk height blends from lane height to terrain over {Blend}m (never below terrain + 0.02)", ["vertices"] = edgeV, ["stepMax_m"] = Math.Round(walkEdgeStepMax, 3), ["over5cm"] = edgeStep5, ["over10cm"] = edgeStep10 },
        };
        spreadBefore.Sort(); spreadAfter.Sort();
        double Med(List<double> l) => l.Count == 0 ? 0 : l[l.Count / 2];
        o["flatten"] = new JObject
        {
            ["rule"] = $"walking-path junctions (degree ≥ 3): terrain grid points within r (= max half width + 1m) set to the disc mean, change capped ±{FlattenCap}m, linear fade over 2m outside; points within 0.6m of a building, 1.5m of a step tread, or a lane/main road shoulder are not touched",
            ["junctions"] = o["junctions"].Count(), ["flattened"] = flatJ, ["capped"] = capped, ["gridPointsMoved"] = moved, ["maxMove_m"] = Math.Round(maxMove, 3),
            ["spreadInDisc_m"] = new JObject { ["before_median"] = Math.Round(Med(spreadBefore), 3), ["after_median"] = Math.Round(Med(spreadAfter), 3), ["before_max"] = spreadBefore.Count == 0 ? 0 : Math.Round(spreadBefore.Max(), 3), ["after_max"] = spreadAfter.Count == 0 ? 0 : Math.Round(spreadAfter.Max(), 3) },
            ["list"] = junc,
        };
        o.Remove("junctions");
        o["input"] = new JObject { ["lanes"] = lanes.Count, ["paths"] = paths.Count, ["plazas"] = areas.Count, ["treads"] = v13Surf.Count(x => x.kind == "steps"), ["buildings"] = ((JArray)input["buildings"]).Count, ["mains"] = mains.Count, ["v13Polygons"] = v13.Count, ["file"] = inPath.Replace('\\', '/') };
        r0.stats = o;
        return r0;
    }

    static double SegDist(Vec2 p, Vec2 a, Vec2 b, out double u)
    {
        double dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz;
        u = L2 > 1e-12 ? Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / L2)) : 0;
        double qx = a.x + dx * u - p.x, qz = a.z + dz * u - p.z; return Math.Sqrt(qx * qx + qz * qz);
    }

    static void RunPython(string script, string inPath, string outPath)
    {
        var psi = new ProcessStartInfo("py", $"-X utf8 \"{script}\" \"{inPath}\" \"{outPath}\"")
        { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        using (var p = Process.Start(psi))
        {
            string so = p.StandardOutput.ReadToEnd(), se = p.StandardError.ReadToEnd();
            if (!p.WaitForExit(600000)) { p.Kill(); throw new Exception("road_sheet.py 시간 초과(10분)"); }
            if (p.ExitCode != 0) throw new Exception($"road_sheet.py 실패(exit {p.ExitCode}): {se}");
            UnityEngine.Debug.Log($"[TownGen] road_sheet.py: {so.Trim()}");
        }
    }
}
