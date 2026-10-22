using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using PitCrew.TownGen;

/// <summary>
/// 구조 조각 11: 아말피 중심(항구점 반경 400m) OSM 보행길 전 종류(living_street·service·pedestrian·footway·steps·path)를
/// OSM 선 그대로 지형 위에 깐다(tools/town-gen/out/structure_a/town_paths.json, town_paths_prep.py). 차도(primary·residential 등)는 SplineTown 이 그대로 그린다.
/// - 폭 = OSM width 태그, 없으면 종류별 가설 폭(prep 의 WIDTH).
/// - 건물과 겹치면 건물을 빼지 않고 길 폭을 줄인다: 반폭 = min(가설 반폭, 가장 가까운 건물 윤곽까지 거리), 최소 0.35m.
/// - 중심선이 건물 안이면 그 구간은 그리지 않는다 — under(tunnel/covered = yes·building_passage)면 "건물 아래로 지나감", 아니면 "건물 관통(태그 없음)"으로 센다.
/// - steps = 디딤 0.35m·단 0.17m 상자(공용 계단과 같은 꼴), 나머지 = 지형 위 0.06m 띠. 광장(area=yes) = 1m 칸 포장, 건물 안 칸은 뺀다.
/// </summary>
public static class TownGenTownPaths
{
    public class PathWay { public long osm; public string highway, name; public bool under; public double width; public List<Vec2> pts = new List<Vec2>();
        /// <summary>조각 13: pts(2m 점)마다 반폭 — 건물 사이 틈(TownGenGapWidth.Fit). null = 예전 규칙(가설 반폭, 건물 거리로 줄임).</summary>
        public List<double> fitHalf; }
    public class Area { public long osm; public string name; public Vec2[] ring; public double area; }

    public static (List<PathWay> paths, List<PathWay> cars, List<Area> areas, Vec2 harbor, double radius) Load(string path)
    {
        var doc = JObject.Parse(File.ReadAllText(path));
        var paths = new List<PathWay>(); var cars = new List<PathWay>();
        foreach (var w in doc["ways"])
        {
            var pw = new PathWay { osm = w["osm_way"].Value<long>(), highway = w["highway"].ToString(), name = w["name"].ToString(), under = w["under"].Value<bool>(), width = w["width_m"].Value<double>() };
            pw.pts.AddRange(w["points_m"].Select(p => new Vec2(p[0].Value<double>(), p[1].Value<double>())));
            (w["kind"].ToString() == "car" ? cars : paths).Add(pw);
        }
        var areas = doc["areas"].Select(a => new Area { osm = a["osm_way"].Value<long>(), name = a["name"].ToString(), area = a["area_m2"].Value<double>(), ring = a["ring"].Select(p => new Vec2(p[0].Value<double>(), p[1].Value<double>())).ToArray() }).ToList();
        var h = doc["harbor"]; return (paths, cars, areas, new Vec2(h[0].Value<double>(), h[1].Value<double>()), doc["radius_m"].Value<double>());
    }

    /// <summary>놓인 건물 윤곽 공간 해시: 점이 안에 있나 / 가장 가까운 윤곽까지 거리(reach 밖이면 reach).</summary>
    public class BIndex
    {
        const double C = 16; readonly Dictionary<(int, int), List<TownGenOsmBuildings.B>> h = new Dictionary<(int, int), List<TownGenOsmBuildings.B>>();
        public BIndex(IEnumerable<TownGenOsmBuildings.B> bs, double reach)
        {
            foreach (var b in bs)
                for (int cx = (int)Math.Floor((b.minX - reach) / C); cx <= (int)Math.Floor((b.maxX + reach) / C); cx++)
                    for (int cz = (int)Math.Floor((b.minZ - reach) / C); cz <= (int)Math.Floor((b.maxZ + reach) / C); cz++)
                    { if (!h.TryGetValue((cx, cz), out var l)) h[(cx, cz)] = l = new List<TownGenOsmBuildings.B>(); l.Add(b); }
        }
        public IEnumerable<TownGenOsmBuildings.B> Near(Vec2 p) => h.TryGetValue(((int)Math.Floor(p.x / C), (int)Math.Floor(p.z / C)), out var l) ? l : Enumerable.Empty<TownGenOsmBuildings.B>();
        public TownGenOsmBuildings.B InsideOf(Vec2 p)
        {
            foreach (var b in Near(p)) if (p.x >= b.minX && p.x <= b.maxX && p.z >= b.minZ && p.z <= b.maxZ && TownGenOsmBuildings.Inside(p, b.ring)) return b;
            return null;
        }
        public double Clearance(Vec2 p, double reach)
        {
            double m = reach;
            foreach (var b in Near(p)) { if (p.x < b.minX - m || p.x > b.maxX + m || p.z < b.minZ - m || p.z > b.maxZ + m) continue; m = Math.Min(m, TownGenOsmBuildings.DistToRing(p, b.ring)); }
            return m;
        }
    }

    public class Sample { public Vec2 p, dir; public double half, nominalHalf; public int state; }   // state 0 그림 · 1 건물 아래(under 태그) · 2 건물 관통(태그 없음) · 3 바다

    static List<(Vec2 p, Vec2 dir)> Resample(List<Vec2> pl, double step)
    {
        var o = new List<(Vec2, Vec2)>();
        for (int k = 0; k + 1 < pl.Count; k++)
        {
            var a = pl[k]; var b = pl[k + 1]; double L = SplineEdge.Dist(a, b); if (L < 1e-6) continue;
            var d = new Vec2((b.x - a.x) / L, (b.z - a.z) / L); int n = Math.Max(1, (int)Math.Ceiling(L / step));
            for (int i = (o.Count == 0 ? 0 : 1); i <= n; i++) o.Add((new Vec2(a.x + (b.x - a.x) * i / n, a.z + (b.z - a.z) * i / n), d));
        }
        return o;
    }

    internal class Result
    {
        public TownGenSpline.MeshBuilder ribbon = new TownGenSpline.MeshBuilder(), steps = new TownGenSpline.MeshBuilder(), plaza = new TownGenSpline.MeshBuilder();
        public List<Vec2> finePts = new List<Vec2>(); public List<(PathWay w, List<Sample> s)> drawn = new List<(PathWay, List<Sample>)>();
        /// <summary>조각 13 겹침 검사용: 그린 면 다각형(보행 띠 한 칸 · 계단 디딤 · 광장 칸).</summary>
        public List<(string kind, string way, Vec2[] poly)> surf = new List<(string, string, Vec2[])>();
        public JObject stats;
    }

    internal static Result Build(List<PathWay> paths, List<Area> areas, List<TownGenOsmBuildings.B> placed, TerrainGrid t, double seaLevel, bool exactPlaza = false)
    {
        var r = new Result(); var idx = new BIndex(placed, 6);
        var lenBy = new Dictionary<string, double[]>();   // [그림, 건물 아래(under), 건물 관통, 바다, 폭 줄임]
        int stepCount = 0, cellNarrowed = 0, cellSkipped = 0; double skippedM = 0; var skippedAt = new JArray();
        foreach (var w in paths)
        {
            bool isSteps = w.highway == "steps"; double nh = w.width / 2;
            var raw = Resample(w.pts, isSteps ? 0.35 : 1.0);
            var ss = new List<Sample>();
            // 조각 13: 2m 점 반폭을 길이 따라 보간
            double[] stArc = null; if (w.fitHalf != null) { stArc = new double[w.pts.Count]; for (int i = 1; i < w.pts.Count; i++) stArc[i] = stArc[i - 1] + SplineEdge.Dist(w.pts[i - 1], w.pts[i]); }
            double HalfAt(double a)
            {
                int j = Array.BinarySearch(stArc, a); if (j >= 0) return w.fitHalf[j]; j = ~j;
                if (j <= 0) return w.fitHalf[0]; if (j >= stArc.Length) return w.fitHalf[stArc.Length - 1];
                double u = (a - stArc[j - 1]) / Math.Max(1e-9, stArc[j] - stArc[j - 1]); return w.fitHalf[j - 1] * (1 - u) + w.fitHalf[j] * u;
            }
            double sArc = 0; Vec2? prevP = null;
            foreach (var (p, d) in raw)
            {
                if (prevP.HasValue) sArc += SplineEdge.Dist(prevP.Value, p); prevP = p;
                double want = stArc != null ? HalfAt(sArc) : nh;
                var smp = new Sample { p = p, dir = d, nominalHalf = nh };
                if (idx.InsideOf(p) != null) smp.state = w.under ? 1 : 2;
                else if (t.Surface(p.x, p.z) < seaLevel - 0.5) smp.state = 3;
                else smp.half = Math.Max(0.35, Math.Min(want, idx.Clearance(p, want)));
                ss.Add(smp);
            }
            if (!lenBy.TryGetValue(w.highway, out var acc)) lenBy[w.highway] = acc = new double[5];
            for (int i = 0; i + 1 < ss.Count; i++)
            {
                var a = ss[i]; var b = ss[i + 1]; double L = SplineEdge.Dist(a.p, b.p);
                int st = Math.Max(a.state, b.state) == 0 ? 0 : (a.state != 0 ? a.state : b.state);
                acc[st] += L; if (st == 0 && Math.Min(a.half, b.half) < nh - 0.05) acc[4] += L;
                if (st != 0) continue;
                // 조각 13(exact): 칸(띠 한 칸·디딤 하나)마다 건물 윤곽과 겹치면 반폭을 0.8배씩 줄이고(0.2m 까지), 그래도 걸리면 그 칸은 그리지 않는다(끊김으로 셈)
                bool CellHits(Vec2[] q) => exactPlaza && idx.Near(new Vec2(q.Average(v => v.x), q.Average(v => v.z))).Any(bb => TownGenGapWidth.PolyHit(q, bb.ring));
                if (isSteps)
                {
                    // 디딤 하나 = 두 표본 사이. 윗면 = 그 자리 지형을 0.17m 단으로(지형 위 0.05m)
                    var m = new Vec2((a.p.x + b.p.x) / 2, (a.p.z + b.p.z) / 2); float g = (float)t.Surface(m.x, m.z); float top = Mathf.Ceil((g + 0.05f) / 0.17f) * 0.17f;
                    float yaw = Mathf.Atan2((float)a.dir.x, (float)a.dir.z) * Mathf.Rad2Deg;
                    double hh = Math.Min(a.half, b.half), hl = (L + 0.02) / 2; var f = a.dir; var nr = new Vec2(f.z, -f.x);
                    Vec2[] Tread(double h) => new[] { new Vec2(m.x - f.x * hl - nr.x * h, m.z - f.z * hl - nr.z * h), new Vec2(m.x + f.x * hl - nr.x * h, m.z + f.z * hl - nr.z * h), new Vec2(m.x + f.x * hl + nr.x * h, m.z + f.z * hl + nr.z * h), new Vec2(m.x - f.x * hl + nr.x * h, m.z - f.z * hl + nr.z * h) };
                    while (hh > 0.2 && CellHits(Tread(hh))) { hh *= 0.8; cellNarrowed++; }
                    if (CellHits(Tread(hh))) { cellSkipped++; skippedM += L; if (skippedAt.Count < 60) skippedAt.Add(new JArray($"{w.highway}:{w.osm}:{w.name}", Math.Round(m.x, 1), Math.Round(m.z, 1))); continue; }
                    TownGenSpline.Box(r.steps, new Vector3((float)m.x, 0, (float)m.z), yaw, (float)(hh * 2), (float)L + 0.02f, g - 0.4f, top);
                    r.surf.Add(("steps", $"{w.highway}:{w.osm}:{w.name}", Tread(hh)));
                    stepCount++;
                }
                else
                {
                    double ha = a.half, hb = b.half;
                    Vector3 P(Sample s0, double h, double side) { var n = new Vec2(-s0.dir.z, s0.dir.x); double x = s0.p.x + n.x * h * side, z = s0.p.z + n.z * h * side; return new Vector3((float)x, (float)t.Surface(x, z) + 0.06f, (float)z); }
                    Vec2[] Q2() => new[] { P(a, ha, -1), P(b, hb, -1), P(b, hb, 1), P(a, ha, 1) }.Select(v => new Vec2(v.x, v.z)).ToArray();
                    while (Math.Max(ha, hb) > 0.2 && CellHits(Q2())) { ha *= 0.8; hb *= 0.8; cellNarrowed++; }
                    if (CellHits(Q2())) { cellSkipped++; skippedM += L; if (skippedAt.Count < 60) skippedAt.Add(new JArray($"{w.highway}:{w.osm}:{w.name}", Math.Round(a.p.x, 1), Math.Round(a.p.z, 1))); continue; }
                    r.ribbon.Quad(P(a, ha, -1), P(b, hb, -1), P(b, hb, 1), P(a, ha, 1), Vector3.up);
                    r.surf.Add(("path", $"{w.highway}:{w.osm}:{w.name}", Q2()));
                }
                r.finePts.Add(a.p);
            }
            r.drawn.Add((w, ss));
        }
        // 광장 면: 1m 칸, 칸 가운데가 고리 안이고 건물 밖이면 포장
        var areaJ = new JArray(); var plazaPts = new List<Vec2>();
        foreach (var ar in areas)
        {
            double x0 = ar.ring.Min(q => q.x), x1 = ar.ring.Max(q => q.x), z0 = ar.ring.Min(q => q.z), z1 = ar.ring.Max(q => q.z); int cells = 0, inB = 0;
            for (double x = Math.Floor(x0); x < x1; x += 1)
                for (double z = Math.Floor(z0); z < z1; z += 1)
                {
                    var c = new Vec2(x + 0.5, z + 0.5); if (!TownGenOsmBuildings.Inside(c, ar.ring)) continue;
                    Vector3 V(double px, double pz) => new Vector3((float)px, (float)t.Surface(px, pz) + 0.05f, (float)pz);
                    if (exactPlaza)
                    {
                        // 조각 13: 건물 윤곽에 걸친 칸은 0.25m 작은 칸으로 쪼개 걸리지 않는 것만 깐다(가운데 점 판정 → 칸 모서리가 벽 안으로 들어가던 것)
                        Vec2[] Sq(double sx, double sz, double e) => new[] { new Vec2(sx, sz), new Vec2(sx + e, sz), new Vec2(sx + e, sz + e), new Vec2(sx, sz + e) };
                        bool Hit(Vec2[] q) => idx.Near(new Vec2(q[0].x + 0.01, q[0].z + 0.01)).Concat(idx.Near(q[2])).Any(b => TownGenGapWidth.PolyHit(q, b.ring));
                        var full = Sq(x, z, 1);
                        if (!Hit(full)) { r.plaza.Quad(V(x, z), V(x + 1, z), V(x + 1, z + 1), V(x, z + 1), Vector3.up); cells++; r.finePts.Add(c); r.surf.Add(("plaza", $"plaza:{ar.osm}:{ar.name}", full)); continue; }
                        inB++;
                        for (int a2 = 0; a2 < 4; a2++) for (int b2 = 0; b2 < 4; b2++)
                        {
                            double sx = x + a2 * 0.25, sz = z + b2 * 0.25; var q = Sq(sx, sz, 0.25);
                            if (!TownGenOsmBuildings.Inside(new Vec2(sx + 0.125, sz + 0.125), ar.ring) || Hit(q)) continue;
                            r.plaza.Quad(V(sx, sz), V(sx + 0.25, sz), V(sx + 0.25, sz + 0.25), V(sx, sz + 0.25), Vector3.up); r.surf.Add(("plaza", $"plaza:{ar.osm}:{ar.name}", q));
                        }
                        r.finePts.Add(c); continue;
                    }
                    if (idx.InsideOf(c) != null) { inB++; continue; }
                    r.plaza.Quad(V(x, z), V(x + 1, z), V(x + 1, z + 1), V(x, z + 1), Vector3.up); cells++; r.finePts.Add(c);
                    r.surf.Add(("plaza", $"plaza:{ar.osm}:{ar.name}", new[] { new Vec2(x, z), new Vec2(x + 1, z), new Vec2(x + 1, z + 1), new Vec2(x, z + 1) }));
                }
            areaJ.Add(new JObject { ["osm"] = ar.osm, ["name"] = ar.name, ["area_m2"] = ar.area, ["paved_cells_1m"] = cells, ["cells_inside_buildings"] = inB });
        }
        var byHw = new JObject();
        foreach (var kv in lenBy.OrderByDescending(k => k.Value.Sum() - k.Value[4]))
            byHw[kv.Key] = new JObject { ["drawn_m"] = Math.Round(kv.Value[0], 1), ["under_building_tagged_m"] = Math.Round(kv.Value[1], 1), ["through_building_untagged_m"] = Math.Round(kv.Value[2], 1), ["sea_m"] = Math.Round(kv.Value[3], 1), ["width_reduced_m"] = Math.Round(kv.Value[4], 1),
                ["width_m"] = new JArray(paths.Where(p => p.highway == kv.Key).Select(p => p.width).Distinct().OrderBy(v => v)) };
        r.stats = new JObject
        {
            ["rule"] = "OSM line as-is, width = OSM width tag else guess by highway; half width = min(guess/2, distance to nearest placed building outline), min 0.35m; centreline inside a building → not drawn (tagged tunnel/covered = under_building, untagged = through_building)",
            ["paths"] = paths.Count, ["lengthByHighway_m"] = byHw, ["stepTreads"] = stepCount, ["plazaAreas"] = areaJ,
            ["cellFit"] = !exactPlaza ? null : new JObject { ["rule"] = "piece 13: each ribbon cell / step tread touching a building outline is narrowed ×0.8 down to 0.2m half; still touching → not drawn (gap in the path)", ["narrowSteps"] = cellNarrowed, ["skippedCells"] = cellSkipped, ["skipped_m"] = Math.Round(skippedM, 1), ["skippedAt"] = skippedAt },
        };
        return r;
    }

    /// <summary>윤곽 ↔ 윤곽 최소 거리(겹치면 0).</summary>
    public static double RingRing(Vec2[] a, Vec2[] b)
    {
        if (a.Any(p => TownGenOsmBuildings.Inside(p, b)) || b.Any(p => TownGenOsmBuildings.Inside(p, a))) return 0;
        double m = double.MaxValue;
        foreach (var p in a) m = Math.Min(m, TownGenOsmBuildings.DistToRing(p, b));
        foreach (var p in b) m = Math.Min(m, TownGenOsmBuildings.DistToRing(p, a));
        return m;
    }

    /// <summary>측정: 반경 안 놓인 건물의 이웃 건물 틈(중앙값·p10·p90) · 길에 면한 비율(윤곽이 길 가장자리에서 1.5m 안 — 보행길 띠·광장 면·차도 보도 끝·골목 끝).</summary>
    internal static JObject Measure(Result r, List<Area> areas, List<TownGenOsmBuildings.B> placed, SplineTownResult res, SplineTownSettings s, Vec2 harbor, double radius)
    {
        var mine = placed.Where(b => SplineEdge.Dist(b.c, harbor) <= radius).ToList();
        var idx = new BIndex(placed, 40);
        var gaps = new List<double>();
        foreach (var b in mine)
        {
            double g = double.MaxValue;
            foreach (var o in placed)
            {
                if (o == b || o.maxX < b.minX - 40 || o.minX > b.maxX + 40 || o.maxZ < b.minZ - 40 || o.minZ > b.maxZ + 40) continue;
                g = Math.Min(g, RingRing(b.ring, o.ring));
            }
            if (g < double.MaxValue) gaps.Add(g);
        }
        gaps.Sort();
        double Q(double q) => gaps.Count == 0 ? 0 : gaps[Math.Min(gaps.Count - 1, (int)(q * gaps.Count))];
        const double face = 1.5; int facing = 0, facePath = 0, faceCar = 0, facePlaza = 0;
        foreach (var b in mine)
        {
            bool fp = false, fc = false, fz = false;
            foreach (var (w, ss) in r.drawn)
                foreach (var sm in ss)
                {
                    if (sm.state != 0 || sm.p.x < b.minX - 8 || sm.p.x > b.maxX + 8 || sm.p.z < b.minZ - 8 || sm.p.z > b.maxZ + 8) continue;
                    if (TownGenOsmBuildings.DistToRing(sm.p, b.ring) - sm.half <= face) { fp = true; break; }
                }
            foreach (var e in res.edges)
            {
                double half = e.alley ? s.alleyHalfWidthMeters : s.HalfSection;
                foreach (var q in e.pts) if (q.x > b.minX - 12 && q.x < b.maxX + 12 && q.z > b.minZ - 12 && q.z < b.maxZ + 12 && TownGenOsmBuildings.DistToRing(q, b.ring) - half <= face) { fc = true; break; }
                if (fc) break;
            }
            foreach (var a in areas) if (TownGenTownPaths.RingRing(b.ring, a.ring) <= face) { fz = true; break; }
            if (fp) facePath++; if (fc) faceCar++; if (fz) facePlaza++; if (fp || fc || fz) facing++;
        }
        return new JObject
        {
            ["buildingsInRadius"] = mine.Count,
            ["gap_m"] = new JObject { ["rule"] = "per building: min outline-to-outline distance to any other placed building (0 = touching/overlapping)", ["median"] = Math.Round(Q(0.5), 2), ["p10"] = Math.Round(Q(0.1), 2), ["p90"] = Math.Round(Q(0.9), 2), ["touching_le_0.3m"] = gaps.Count(g => g <= 0.3) },
            ["facingStreet"] = new JObject { ["rule"] = $"outline within {face}m of a drawn path edge, a plaza area, or a car road section edge (sidewalk end) / alley edge", ["ratio"] = mine.Count == 0 ? 0 : Math.Round((double)facing / mine.Count, 3), ["count"] = facing, ["byPath"] = facePath, ["byCarRoad"] = faceCar, ["byPlaza"] = facePlaza },
        };
    }
}
