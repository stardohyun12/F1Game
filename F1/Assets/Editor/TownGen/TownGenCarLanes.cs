using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using PitCrew.TownGen;

/// <summary>
/// 구조 조각 12(D-115): 중심 living_street·service 를 좁은 차로(골목 단면, 폭 = OSM width 4m)로 되돌린 뒤
/// ① 광장 면 안 차도 선을 입구 앞에서 자르고(ClipOutside) ② 차도가 닿는 광장 둘레마다 정차 자리(포켓) 1개 + 입구 볼라드 3개
/// ③ 차 도달 BFS(큰길 망에서 이어진 차도만) — 중심 건물 중 차로 15m 안 비율, 포켓→광장 중앙 걸어갈 거리.
/// </summary>
public static class TownGenCarLanes
{
    public const double PocketLen = 6, PocketDepth = 2.5, ReachMeters = 15, LinkMeters = 5.5;

    /// <summary>폴리라인에서 고리(광장) 안 부분을 빼고 바깥 토막만 돌려준다. 고리 경계를 넘는 곳은 교점에서 back m 바깥으로 물린 점에서 끊는다.</summary>
    public static List<List<Vec2>> ClipOutside(List<Vec2> pl, List<Vec2[]> rings, double back, ref double removed)
    {
        var o = new List<List<Vec2>>(); var cur = new List<Vec2>();
        bool In(Vec2 p) => rings.Any(r => TownGenOsmBuildings.Inside(p, r));
        for (int k = 0; k < pl.Count; k++)
        {
            bool inK = In(pl[k]);
            if (k > 0)
            {
                bool inP = In(pl[k - 1]);
                if (inP && inK) { removed += SplineEdge.Dist(pl[k - 1], pl[k]); continue; }
                if (inP != inK)
                {
                    var a = pl[k - 1]; var b = pl[k];
                    double tt = Cross(a, b, rings);   // a→b 에서 경계 교점 비율
                    var x = new Vec2(a.x + (b.x - a.x) * tt, a.z + (b.z - a.z) * tt);
                    double L = SplineEdge.Dist(a, b);
                    var outer = inP ? b : a; double dOut = SplineEdge.Dist(x, outer);
                    var cut = dOut > back ? new Vec2(x.x + (outer.x - x.x) * back / dOut, x.z + (outer.z - x.z) * back / dOut) : outer;
                    removed += L - SplineEdge.Dist(cut, outer);
                    if (!inP) { if (dOut > back + 0.5) cur.Add(cut); if (cur.Count >= 2) o.Add(cur); cur = new List<Vec2>(); }
                    else { cur = new List<Vec2>(); if (dOut > back + 0.5) cur.Add(cut); cur.Add(b); }
                    continue;
                }
            }
            if (!inK) cur.Add(pl[k]);
        }
        if (cur.Count >= 2) o.Add(cur);
        return o;
    }

    static double Cross(Vec2 a, Vec2 b, List<Vec2[]> rings)
    {
        double best = 0.5, bd = double.MaxValue;
        foreach (var r in rings)
            for (int i = 0; i < r.Length; i++)
            {
                var c = r[i]; var d = r[(i + 1) % r.Length];
                double den = (b.x - a.x) * (d.z - c.z) - (b.z - a.z) * (d.x - c.x); if (Math.Abs(den) < 1e-12) continue;
                double t = ((c.x - a.x) * (d.z - c.z) - (c.z - a.z) * (d.x - c.x)) / den, u = ((c.x - a.x) * (b.z - a.z) - (c.z - a.z) * (b.x - a.x)) / den;
                if (t < 0 || t > 1 || u < 0 || u > 1) continue;
                if (t < bd) { bd = t; best = t; }
            }
        return best;
    }

    static (Vec2 p, double d) NearestOnRing(Vec2 p, Vec2[] r)
    {
        Vec2 bp = r[0]; double bd = double.MaxValue;
        for (int i = 0; i < r.Length; i++)
        {
            var a = r[i]; var b = r[(i + 1) % r.Length]; double dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz;
            double t = L2 > 1e-12 ? Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / L2)) : 0;
            var q = new Vec2(a.x + dx * t, a.z + dz * t); double d = SplineEdge.Dist(p, q);
            if (d < bd) { bd = d; bp = q; }
        }
        return (bp, bd);
    }

    static Vec2 Centroid(Vec2[] r)
    {
        double A = 0, cx = 0, cz = 0;
        for (int i = 0; i < r.Length; i++) { var a = r[i]; var b = r[(i + 1) % r.Length]; double c = a.x * b.z - b.x * a.z; A += c; cx += (a.x + b.x) * c; cz += (a.z + b.z) * c; }
        return Math.Abs(A) < 1e-9 ? r[0] : new Vec2(cx / (3 * A), cz / (3 * A));
    }

    /// <summary>차 반폭(차로 가장자리까지): 큰길 = 차로 3m, 좁은 차로(골목 단면) = alleyHalfWidth.</summary>
    static double CarHalf(SplineEdge e, SplineTownSettings s) => e.alley ? (e.halfW != null && e.halfW.Count > 0 ? e.halfW.Average() : s.alleyHalfWidthMeters) : s.HalfCarriageway;   // 조각 13: 점마다 반폭이면 평균

    /// <summary>큰길 망(가장 긴 큰길에서 시작)에서 끝점이 다른 길 선에서 LinkMeters 안이면 이어진 것으로 본 BFS — 닿는 길 번호.</summary>
    public static HashSet<int> Reachable(SplineTownResult res)
    {
        var E = res.edges; var seen = new HashSet<int>(); if (E.Count == 0) return seen;
        int seed = Enumerable.Range(0, E.Count).Where(i => !E[i].alley).OrderByDescending(i => E[i].Length).DefaultIfEmpty(0).First();
        bool Touch(SplineEdge x, SplineEdge y) => new[] { y.pts[0], y.pts[y.pts.Count - 1] }.Any(p => SplineTown.DistToPoly(p, x.pts) <= LinkMeters) || new[] { x.pts[0], x.pts[x.pts.Count - 1] }.Any(p => SplineTown.DistToPoly(p, y.pts) <= LinkMeters);
        var q = new Queue<int>(); q.Enqueue(seed); seen.Add(seed);
        while (q.Count > 0)
        {
            int i = q.Dequeue();
            for (int j = 0; j < E.Count; j++) if (!seen.Contains(j) && (E[i].a == E[j].a || E[i].a == E[j].b || E[i].b == E[j].a || E[i].b == E[j].b || Touch(E[i], E[j]))) { seen.Add(j); q.Enqueue(j); }
        }
        return seen;
    }

    static JToken Nul(double d) => d > 1e300 ? JValue.CreateNull() : new JValue(Math.Round(d, 1));

    internal class Result { public TownGenSpline.MeshBuilder pockets = new TownGenSpline.MeshBuilder(), bollards = new TownGenSpline.MeshBuilder(); public JObject stats; }

    internal static Result Build(SplineTownResult res, SplineTownSettings s, List<TownGenTownPaths.Area> areas, List<TownGenOsmBuildings.B> placed, Vec2 harbor, double radius, Func<Vec2, bool> isSea)
    {
        var r = new Result(); var t = res.terrain; var reach = Reachable(res);
        var idx = new TownGenTownPaths.BIndex(placed, 8);
        var plazaJ = new JArray();
        foreach (var ar in areas)
        {
            var cen = Centroid(ar.ring); bool cenIn = TownGenOsmBuildings.Inside(cen, ar.ring);
            // 광장 안 차도: 모든 길(닿든 안 닿든) 2m 표본이 고리 안인 길이
            double carInside = 0;
            foreach (var e in res.edges) for (int k = 0; k + 1 < e.pts.Count; k++) { var m = new Vec2((e.pts[k].x + e.pts[k + 1].x) / 2, (e.pts[k].z + e.pts[k + 1].z) / 2); if (TownGenOsmBuildings.Inside(m, ar.ring)) carInside += SplineEdge.Dist(e.pts[k], e.pts[k + 1]); }
            // 포켓 후보: 닿는 길 점 중 고리에서 40m 안, 양쪽. 건물·광장 면·다른 길 차로·바다에 안 걸리는 것 중 둘레에 가장 가까운 것
            double best = double.MaxValue; Vec2 bc = default; double byaw = 0, by = 0; int bEdge = -1; bool bAlley = false;
            foreach (var ei in reach)
            {
                var e = res.edges[ei]; double half = CarHalf(e, s);
                for (int i = 1; i + 1 < e.pts.Count; i++)
                {
                    if (NearestOnRing(e.pts[i], ar.ring).d > 40) continue;
                    var (nx, nz) = SplineTown.Normal(e.pts, i); var tg = new Vec2(-nz, nx);   // Normal = 왼쪽 법선 → 진행 방향 = (−nz, nx)… 방향만 쓰므로 부호 무관
                    foreach (int side in new[] { 1, -1 })
                    {
                        double off = half + PocketDepth / 2;
                        var c = new Vec2(e.pts[i].x + nx * off * side, e.pts[i].z + nz * off * side);
                        var corners = new[] { -1, 1 }.SelectMany(a => new[] { -1, 1 }.Select(b => new Vec2(c.x + tg.x * a * PocketLen / 2 + nx * side * b * PocketDepth / 2, c.z + tg.z * a * PocketLen / 2 + nz * side * b * PocketDepth / 2))).ToArray();
                        var rect = new[] { corners[0], corners[1], corners[3], corners[2] };
                        if (TownGenOsmBuildings.Inside(c, ar.ring) || rect.Any(p => TownGenOsmBuildings.Inside(p, ar.ring))) continue;
                        if (rect.Concat(new[] { c }).Any(p => idx.InsideOf(p) != null || isSea(p))) continue;
                        if (idx.Near(c).Any(b => TownGenTownPaths.RingRing(rect, b.ring) < 0.2)) continue;
                        bool onOther = false;
                        foreach (var oj in reach) { if (oj == ei) continue; var o = res.edges[oj]; if (SplineTown.DistToPoly(c, o.pts) < CarHalf(o, s) + PocketDepth / 2) { onOther = true; break; } }
                        if (onOther) continue;
                        double d = NearestOnRing(c, ar.ring).d;
                        if (d < best) { best = d; bc = c; byaw = Math.Atan2(tg.x, tg.z) * 180 / Math.PI; by = e.y[i] + (e.alley ? 0 : s.curbHeightMeters); bEdge = ei; bAlley = e.alley; }
                    }
                }
            }
            var pj = new JObject { ["osm"] = ar.osm, ["name"] = ar.name, ["area_m2"] = ar.area, ["centre"] = new JArray(Math.Round(cen.x, 1), Math.Round(cen.z, 1)), ["centreInsideRing"] = cenIn, ["carRoadInside_m"] = Math.Round(carInside, 1) };
            if (bEdge < 0 || best > 40) { pj["pocket"] = null; pj["note"] = "no reachable car road within 40m with a free pocket spot"; plazaJ.Add(pj); continue; }
            TownGenSpline.Box(r.pockets, new Vector3((float)bc.x, 0, (float)bc.z), (float)byaw + 90, (float)PocketLen, (float)PocketDepth, (float)Math.Min(by, t.Surface(bc.x, bc.z)) - 0.3f, (float)by + 0.04f);
            // 입구 = 포켓에서 가장 가까운 둘레 점. 볼라드 3개(0.25×0.25×0.9m) — 둘레 접선 따라 1.4m 간격, 광장 쪽으로 0.5m
            var (ent, dEnt) = NearestOnRing(bc, ar.ring);
            var inw = new Vec2((ent.x - bc.x) / Math.Max(dEnt, 1e-6), (ent.z - bc.z) / Math.Max(dEnt, 1e-6)); var tan = new Vec2(-inw.z, inw.x);
            int bol = 0;
            foreach (double a in new[] { -1.4, 0, 1.4 })
            {
                var p = new Vec2(ent.x + inw.x * 0.5 + tan.x * a, ent.z + inw.z * 0.5 + tan.z * a);
                if (idx.InsideOf(p) != null) continue;
                float g = (float)t.Surface(p.x, p.z);
                TownGenSpline.Box(r.bollards, new Vector3((float)p.x, 0, (float)p.z), (float)(Math.Atan2(inw.x, inw.z) * 180 / Math.PI), 0.25f, 0.25f, g - 0.2f, g + 0.9f); bol++;
            }
            double walkIn = SplineEdge.Dist(ent, cen);
            pj["pocket"] = new JObject { ["centre"] = new JArray(Math.Round(bc.x, 1), Math.Round(bc.z, 1)), ["size_m"] = new JArray(PocketLen, PocketDepth), ["onRoad"] = bAlley ? "narrow lane (alley section)" : "main road (beside sidewalk)", ["toRing_m"] = Math.Round(best, 1) };
            pj["bollards"] = bol;
            pj["walk_m"] = new JObject { ["rule"] = "straight lines: pocket centre → nearest plaza outline point (entrance) → plaza polygon centroid (destination)", ["pocketToEntrance"] = Math.Round(dEnt, 1), ["entranceToCentre"] = Math.Round(walkIn, 1), ["total"] = Math.Round(dEnt + walkIn, 1), ["straightPocketToCentre"] = Math.Round(SplineEdge.Dist(bc, cen), 1) };
            plazaJ.Add(pj);
        }
        // 차 도달: 반경 안 놓인 건물 윤곽 ↔ 닿는 차도 차로 가장자리 거리 ≤ 15m
        JObject Share(double rad)
        {
            var mine = placed.Where(b => SplineEdge.Dist(b.c, harbor) <= rad).ToList(); int ok = 0, okAny = 0; var ds = new List<double>();
            foreach (var b in mine)
            {
                double dr = double.MaxValue, da = double.MaxValue;
                for (int ei = 0; ei < res.edges.Count; ei++)
                {
                    var e = res.edges[ei]; double half = CarHalf(e, s);
                    foreach (var q in e.pts) { if (q.x < b.minX - 40 || q.x > b.maxX + 40 || q.z < b.minZ - 40 || q.z > b.maxZ + 40) continue; double d = Math.Max(0, TownGenOsmBuildings.DistToRing(q, b.ring) - half); da = Math.Min(da, d); if (reach.Contains(ei)) dr = Math.Min(dr, d); }
                }
                if (dr <= ReachMeters) ok++; if (da <= ReachMeters) okAny++; ds.Add(dr);
            }
            ds.Sort(); double Q(double qq) => ds.Count == 0 ? 0 : ds[Math.Min(ds.Count - 1, (int)(qq * ds.Count))];
            return new JObject { ["radius_m"] = rad, ["buildings"] = mine.Count, ["within15m_reachable"] = ok, ["ratio"] = mine.Count == 0 ? 0 : Math.Round((double)ok / mine.Count, 3), ["within15m_anyCarRoad"] = okAny, ["dist_m_median"] = Nul(Q(0.5)), ["dist_m_p90"] = Nul(Q(0.9)), ["noCarRoadWithin40m"] = ds.Count(d => d > 1e300) };
        }
        r.stats = new JObject
        {
            ["rule"] = $"car graph BFS from the longest main road; edges linked by shared node or an end point within {LinkMeters}m of another road line. Distance = building outline to carriageway edge (main 3m / narrow lane {s.alleyHalfWidthMeters}m half)",
            ["edges"] = res.edges.Count, ["reachableEdges"] = reach.Count, ["unreachable"] = new JArray(Enumerable.Range(0, res.edges.Count).Where(i => !reach.Contains(i)).Select(i => new JObject { ["alley"] = res.edges[i].alley, ["length_m"] = Math.Round(res.edges[i].Length, 1), ["mid"] = new JArray(Math.Round(res.edges[i].pts[res.edges[i].pts.Count / 2].x), Math.Round(res.edges[i].pts[res.edges[i].pts.Count / 2].z)) })),
            ["buildingsNearCar"] = new JArray(Share(150), Share(radius)), ["plazas"] = plazaJ,
        };
        return r;
    }

    /// <summary>반경 안(구간 중점) 차도 곡선 수·길이 — 큰길 / 좁은 차로(골목 단면) / 닿는 것만.</summary>
    public static JObject CoreCarStats(SplineTownResult res, Vec2 harbor, double radius, HashSet<int> reach)
    {
        double Len(SplineEdge e) { double L = 0; for (int k = 0; k + 1 < e.pts.Count; k++) if (SplineEdge.Dist(new Vec2((e.pts[k].x + e.pts[k + 1].x) / 2, (e.pts[k].z + e.pts[k + 1].z) / 2), harbor) <= radius) L += SplineEdge.Dist(e.pts[k], e.pts[k + 1]); return L; }
        var ins = Enumerable.Range(0, res.edges.Count).Where(i => Len(res.edges[i]) > 0).ToList();
        JObject Part(IEnumerable<int> ids) { var l = ids.ToList(); return new JObject { ["curves"] = l.Count, ["length_m"] = Math.Round(l.Sum(i => Len(res.edges[i])), 1) }; }
        return new JObject
        {
            ["main"] = Part(ins.Where(i => !res.edges[i].alley)), ["lane"] = Part(ins.Where(i => res.edges[i].alley)),
            ["all"] = Part(ins), ["reachable"] = reach == null ? null : Part(ins.Where(reach.Contains)),
            ["globalMain"] = new JObject { ["curves"] = res.Mains.Count(), ["length_m"] = Math.Round(res.Mains.Sum(e => e.Length), 1) },
        };
    }
}
