using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using PitCrew.TownGen;

/// <summary>
/// 구조 조각 13(도현 승인 2026-10-20, 아이디어 1+4 — 아말피 중심 400m 만):
/// ① 길 폭 = 건물 사이 틈. 마을 길(좁은 차로·보행길) 가운데 선 2m 점마다 양옆 수직으로 건물 벽까지 잰다.
///    양쪽 다 벽(종류별 최대 폭 안) → 가운데 선을 틈 가운데로(최대 3m, 넘으면 원래 선 + 플래그), 폭 = 틈(종류별 최소·최대).
///    한쪽만 벽 → 기본 폭을 유지하되 벽에서 떨어지게 비킴(최대 3m). 벽 없음 → 기본 폭 그대로. 큰길은 그대로.
///    옮긴 양은 이웃 5점 평균으로 다듬고 끝 6m 는 0 으로 모은다(입구·잇기 점 고정).
///    좁은 차로가 최소 3m 를 못 얻으면 3m 로 두고 걸린 건물은 Classify 가 뺀다(차가 지나가야 함).
///    보행길은 최소 폭 아래로도 좁혀 벽에 붙인다(건물을 안 뺌) — 최소 미달 길이는 따로 센다.
/// ② 겹침 검사: 건물 윤곽 ∩ 길 면(차로 돌·보행 띠·계단 디딤·광장 포장·큰길 차로)을 다각형 교차로 센다(면을 0.05m 안으로 줄여 맞닿음은 뺌).
/// </summary>
public static class TownGenGapWidth
{
    public const double MaxShift = 3, Station = 2, EndTaper = 6, Shrink = 0.05;

    /// <summary>종류별 폭(m) — 가설. 차로 최소 3m 는 PD 지시.</summary>
    public static (double min, double max) Range(string kind)
    {
        switch (kind)
        {
            case "lane": return (3, 6);
            case "pedestrian": return (2, 8);
            case "living_street": case "service": return (2, 6);
            case "steps": return (1, 4);
            case "path": return (0.8, 3);
            default: return (0.8, 4);   // footway 등
        }
    }

    public class Stats
    {
        public Dictionary<string, List<double>> widths = new Dictionary<string, List<double>>();
        public Dictionary<string, int[]> counts = new Dictionary<string, int[]>();   // [표본, 양벽(틈), 한벽, 벽없음, 3m 넘어 원래 선, 선이 건물 안, 최소로 올림, 최대로 자름, 최소 미달(보행)]
        public Dictionary<string, double> shiftMax = new Dictionary<string, double>();
        public List<JObject> flags = new List<JObject>();
        public int[] C(string k) { if (!counts.TryGetValue(k, out var c)) counts[k] = c = new int[9]; return c; }
        public JObject ToJson()
        {
            var o = new JObject();
            foreach (var k in counts.Keys.OrderBy(x => x))
            {
                var w = widths.TryGetValue(k, out var l) ? l.OrderBy(x => x).ToList() : new List<double>(); var c = counts[k];
                double Q(double q) => w.Count == 0 ? 0 : Math.Round(w[Math.Min(w.Count - 1, (int)(q * w.Count))], 2);
                var (mn, mx) = Range(k);
                var hist = new JObject(); foreach (var g in w.GroupBy(x => Math.Floor(x)).OrderBy(g => g.Key)) hist[$"{g.Key:0}-{g.Key + 1:0}m"] = g.Count();
                o[k] = new JObject
                {
                    ["range_m"] = new JArray(mn, mx), ["stations"] = c[0], ["bothWalls"] = c[1], ["oneWall"] = c[2], ["noWall"] = c[3], ["shiftOver3m_keptLine"] = c[4], ["centreInBuilding"] = c[5],
                    ["raisedToMin"] = c[6], ["cutToMax"] = c[7], ["belowMin_walk"] = c[8], ["maxShift_m"] = Math.Round(shiftMax.TryGetValue(k, out var sm) ? sm : 0, 2),
                    ["width_m"] = new JObject { ["p10"] = Q(0.1), ["median"] = Q(0.5), ["p90"] = Q(0.9), ["min"] = w.Count == 0 ? 0 : Math.Round(w[0], 2), ["max"] = w.Count == 0 ? 0 : Math.Round(w[w.Count - 1], 2), ["hist"] = hist },
                };
            }
            return new JObject { ["rule"] = "stations every 2m on the lane/path centreline; rays both sides to the nearest placed-building wall. Both walls within the kind's max width → centre moves to the gap middle (≤3m, else original line + flag) and width = gap clamped to the kind range; one wall → default width, pushed off the wall (≤3m); none → default width. Shift smoothed over 5 stations, ends (6m) fixed. Lanes keep min 3m (blocking buildings are dropped); walking paths may narrow below min (no building dropped).", ["byKind"] = o, ["flags"] = new JArray(flags.Take(40)) };
        }
    }

    // ---- 기하
    static double RayToRing(Vec2 p, Vec2 d, Vec2[] r, double reach)
    {
        double best = reach;
        for (int i = 0; i < r.Length; i++)
        {
            var a = r[i]; var b = r[(i + 1) % r.Length]; double ex = b.x - a.x, ez = b.z - a.z;
            double den = d.x * ez - d.z * ex; if (Math.Abs(den) < 1e-12) continue;
            double t = ((a.x - p.x) * ez - (a.z - p.z) * ex) / den, u = ((a.x - p.x) * d.z - (a.z - p.z) * d.x) / den;
            if (t >= 0 && t < best && u >= 0 && u <= 1) best = t;
        }
        return best;
    }
    static double Ray(Vec2 p, Vec2 d, TownGenTownPaths.BIndex idx, double reach)
    {
        double m = reach;
        foreach (var b in idx.Near(p)) m = Math.Min(m, RayToRing(p, d, b.ring, m));
        return m;
    }
    public static Vec2 Nrm(List<Vec2> p, int i) { var (x, z) = SplineTown.Normal(p, i); return new Vec2(x, z); }

    /// <summary>점 목록(약 2m 간격)에 폭 맞추기: 옮긴 점 · 점마다 반폭. walk = 보행길(최소 미달 허용).</summary>
    public static (List<Vec2> pts, List<double> half) Fit(List<Vec2> pts, string kind, double nominalW, TownGenTownPaths.BIndex idx, Stats st, string label, bool walk, Func<int, bool> fixedAt = null)
    {
        var (mn, mx) = Range(kind); mx = Math.Max(mx, nominalW); double h0 = Math.Max(mn, Math.Min(mx, nominalW)) / 2, minH = mn / 2, maxH = mx / 2, R = mx;
        int n = pts.Count; var off = new double[n]; var mode = new int[n]; var c = st.C(kind);
        var arc = new double[n]; for (int i = 1; i < n; i++) arc[i] = arc[i - 1] + SplineEdge.Dist(pts[i - 1], pts[i]);
        for (int i = 0; i < n; i++)
        {
            if (fixedAt != null && fixedAt(i)) { mode[i] = -1; continue; }
            var p = pts[i]; var nv = Nrm(pts, i); c[0]++;
            if (idx.InsideOf(p) != null) { mode[i] = 5; c[5]++; continue; }
            double dL = Ray(p, nv, idx, R), dR = Ray(p, new Vec2(-nv.x, -nv.z), idx, R);
            bool wl = dL < R, wr = dR < R;
            if (wl && wr)
            {
                c[1]++; mode[i] = 1; double o = (dL - dR) / 2;
                if (Math.Abs(o) > MaxShift) { c[4]++; mode[i] = 4; if (st.flags.Count < 200) st.flags.Add(new JObject { ["kind"] = kind, ["way"] = label, ["at"] = new JArray(Math.Round(p.x, 1), Math.Round(p.z, 1)), ["shift_m"] = Math.Round(o, 1), ["note"] = "gap middle > 3m from line — kept original line" }); o = 0; }
                off[i] = o;
            }
            else if (wl || wr)
            {
                c[2]++; mode[i] = 2; double dw = wl ? dL : dR;
                if (dw < h0) off[i] = (wl ? -1 : 1) * Math.Min(MaxShift, h0 - dw);
            }
            else { c[3]++; mode[i] = 3; }
        }
        // 다듬기(이웃 5점 평균) + 끝 6m 고정
        var sm = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sum = 0; int k = 0; for (int j = Math.Max(0, i - 2); j <= Math.Min(n - 1, i + 2); j++) { sum += off[j]; k++; }
            double taper = Math.Min(1, Math.Min(arc[i], arc[n - 1] - arc[i]) / EndTaper);
            sm[i] = mode[i] == -1 ? 0 : sum / k * taper;
        }
        var outP = new List<Vec2>(n); var half = new List<double>(n); if (!st.shiftMax.ContainsKey(kind)) st.shiftMax[kind] = 0;
        for (int i = 0; i < n; i++) { var nv = Nrm(pts, i); outP.Add(new Vec2(pts[i].x + nv.x * sm[i], pts[i].z + nv.z * sm[i])); st.shiftMax[kind] = Math.Max(st.shiftMax[kind], Math.Abs(sm[i])); }
        if (!st.widths.ContainsKey(kind)) st.widths[kind] = new List<double>();
        for (int i = 0; i < n; i++)
        {
            if (mode[i] == -1) { half.Add(h0); continue; }
            var p = outP[i]; var nv = Nrm(outP, i); double h;
            if (idx.InsideOf(p) != null) h = walk ? 0.35 : minH;
            else
            {
                double dL = Ray(p, nv, idx, R), dR = Ray(p, new Vec2(-nv.x, -nv.z), idx, R);
                bool wl = dL < R, wr = dR < R;
                h = wl && wr ? Math.Min(dL, dR) : wl ? Math.Min(h0, dL) : wr ? Math.Min(h0, dR) : h0;
            }
            if (h > maxH) { h = maxH; c[7]++; }
            if (h < minH) { if (walk) { c[8]++; h = Math.Max(0.35, h); } else { c[6]++; h = minH; } }
            half.Add(h); st.widths[kind].Add(h * 2);
        }
        return (outP, half);
    }

    /// <summary>폴리라인을 2m 점으로 다시 찍는다(끝점 포함).</summary>
    public static List<Vec2> Stations(List<Vec2> pl)
    {
        double L = SplineEdge.PolyLength(pl); int n = Math.Max(1, (int)Math.Round(L / Station)); var o = new List<Vec2>();
        double acc = 0; int seg = 0;
        for (int k = 0; k <= n; k++)
        {
            double a = L * k / n;
            while (seg + 1 < pl.Count - 1 && acc + SplineEdge.Dist(pl[seg], pl[seg + 1]) < a) { acc += SplineEdge.Dist(pl[seg], pl[seg + 1]); seg++; }
            double sl = SplineEdge.Dist(pl[seg], pl[seg + 1]); double t = sl > 1e-9 ? Math.Min(1, (a - acc) / sl) : 0;
            o.Add(new Vec2(pl[seg].x + (pl[seg + 1].x - pl[seg].x) * t, pl[seg].z + (pl[seg + 1].z - pl[seg].z) * t));
        }
        return o;
    }

    /// <summary>중심 반경 안 골목(좁은 차로) — 가운데 선 옮김 + 점마다 반폭(SplineEdge.halfW). 다른 골목 반폭은 그대로.</summary>
    public static void AdjustLanes(SplineTownResult res, List<TownGenOsmBuildings.B> bs, Vec2 harbor, double radius, SplineTownSettings s, Func<Vec2, Vec2, bool> segUnder, Stats st)
    {
        // 벽 후보 = 바다·큰길 차로에 걸려 어차피 빠질 건물을 뺀 나머지
        var mains = res.edges.Where(e => !e.alley).ToList();
        bool OnMain(TownGenOsmBuildings.B b) => mains.Any(e => e.pts.Any(q => q.x > b.minX - 6 && q.x < b.maxX + 6 && q.z > b.minZ - 6 && q.z < b.maxZ + 6 && TownGenOsmBuildings.DistToRing(q, b.ring) <= s.HalfCarriageway));
        var walls = bs.Where(b => SplineEdge.Dist(b.c, harbor) <= radius + 40 && !OnMain(b)).ToList();
        var idx = new TownGenTownPaths.BIndex(walls, 10);
        int k = 0;
        foreach (var e in res.edges.Where(e => e.alley && e.pts.Count >= 2 && SplineEdge.Dist(e.pts[e.pts.Count / 2], harbor) <= radius).ToList())
        {
            var pts0 = e.pts;
            Func<int, bool> under = segUnder == null ? null : (Func<int, bool>)(i => segUnder(pts0[Math.Max(0, i - 1)], pts0[Math.Min(pts0.Count - 1, i + 1)]));
            var (p, h) = Fit(e.pts, "lane", s.alleyHalfWidthMeters * 2, idx, st, $"lane#{k++} {e.highway}", false, under);
            e.pts = p; e.halfW = h;
        }
    }

    // ---- 겹침 검사
    static bool Cross(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
    {
        double O(Vec2 p, Vec2 q, Vec2 r) => (q.x - p.x) * (r.z - p.z) - (q.z - p.z) * (r.x - p.x);
        return O(a, b, c) * O(a, b, d) < 0 && O(c, d, a) * O(c, d, b) < 0;
    }
    public static bool PolyHit(Vec2[] q, Vec2[] ring)
    {
        if (q.Any(p => TownGenOsmBuildings.Inside(p, ring)) || ring.Any(p => TownGenOsmBuildings.Inside(p, q))) return true;
        for (int i = 0; i < q.Length; i++) for (int j = 0; j < ring.Length; j++) if (Cross(q[i], q[(i + 1) % q.Length], ring[j], ring[(j + 1) % ring.Length])) return true;
        return false;
    }
    static Vec2[] ShrinkPoly(Vec2[] q)
    {
        double cx = q.Average(p => p.x), cz = q.Average(p => p.z);
        return q.Select(p => { double d = SplineEdge.Dist(p, new Vec2(cx, cz)); return d < Shrink * 2 ? new Vec2(cx, cz) : new Vec2(p.x + (cx - p.x) * Shrink / d, p.z + (cz - p.z) * Shrink / d); }).ToArray();
    }
    public static Vec2[] Quad(Vec2 a, Vec2 b, Vec2 na, Vec2 nb, double ha, double hb) =>
        new[] { new Vec2(a.x - na.x * ha, a.z - na.z * ha), new Vec2(b.x - nb.x * hb, b.z - nb.z * hb), new Vec2(b.x + nb.x * hb, b.z + nb.z * hb), new Vec2(a.x + na.x * ha, a.z + na.z * ha) };

    /// <summary>놓인 건물(반경 안) ↔ 길 면 겹침. kind = lane · path · steps · plaza · main_carriageway. 같은 (건물, 길, 종류) 는 한 번만 센다.</summary>
    public static JObject Overlaps(List<TownGenOsmBuildings.B> placed, SplineTownResult res, SplineTownSettings s, List<(string kind, string way, Vec2[] poly)> townSurf, Vec2 harbor, double radius, Func<Vec2, Vec2, bool> segUnder, out int total, out int failCount)
    {
        var surf = new List<(string kind, string way, Vec2[] poly)>(townSurf);
        for (int ei = 0; ei < res.edges.Count; ei++)
        {
            var e = res.edges[ei];
            for (int i = 0; i + 1 < e.pts.Count; i++)
            {
                var a = e.pts[i]; var b = e.pts[i + 1];
                if (SplineEdge.Dist(new Vec2((a.x + b.x) / 2, (a.z + b.z) / 2), harbor) > radius + 20) continue;
                if (segUnder != null && segUnder(a, b)) continue;   // tunnel/covered — 건물 아래로 지나감
                double ha = e.alley ? e.AlleyHalf(i, s.alleyHalfWidthMeters) : s.HalfCarriageway, hb = e.alley ? e.AlleyHalf(i + 1, s.alleyHalfWidthMeters) : s.HalfCarriageway;
                surf.Add((e.alley ? "lane" : "main_carriageway", $"edge{ei}:{e.highway}", Quad(a, b, Nrm(e.pts, i), Nrm(e.pts, i + 1), ha, hb)));
            }
        }
        var mine = placed.Where(b => SplineEdge.Dist(b.c, harbor) <= radius).ToList();
        var seen = new HashSet<(string, string, string)>(); var list = new JArray(); var byKind = new Dictionary<string, int>();
        foreach (var (kind, way, poly) in surf)
        {
            double x0 = poly.Min(p => p.x), x1 = poly.Max(p => p.x), z0 = poly.Min(p => p.z), z1 = poly.Max(p => p.z);
            Vec2[] sp = null;
            foreach (var b in mine)
            {
                if (b.maxX < x0 || b.minX > x1 || b.maxZ < z0 || b.minZ > z1) continue;
                var key = (kind, way, b.osm); if (seen.Contains(key)) continue;
                sp = sp ?? ShrinkPoly(poly);
                if (!PolyHit(sp, b.ring)) continue;
                seen.Add(key); byKind[kind] = (byKind.TryGetValue(kind, out var n) ? n : 0) + 1;
                list.Add(new JObject { ["kind"] = kind, ["way"] = way, ["building"] = b.osm, ["at"] = new JArray(Math.Round(poly.Average(p => p.x), 1), Math.Round(poly.Average(p => p.z), 1)) });
            }
        }
        total = list.Count; failCount = list.Count;
        var bk = new JObject(); foreach (var k in new[] { "lane", "path", "steps", "plaza", "main_carriageway", "sheet" }) bk[k] = byKind.TryGetValue(k, out var v) ? v : 0;
        return new JObject { ["rule"] = $"placed building outline (within {radius}m) ∩ drawn road surface polygon shrunk {Shrink}m (touching does not count). lane = narrow car lane stone, path = walking ribbon, steps = step treads, plaza = plaza paving cells, main_carriageway = big road 6m carriageway (sidewalks not counted). tunnel/covered segments skipped. One count per (kind, road, building).", ["total"] = list.Count, ["byKind"] = bk, ["buildingsChecked"] = mine.Count, ["surfacePolygons"] = surf.Count, ["overlaps"] = list };
    }
}
