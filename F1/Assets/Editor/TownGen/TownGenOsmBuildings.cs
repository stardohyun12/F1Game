using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using PitCrew.TownGen;

/// <summary>
/// 구조 조각 10(D-114): OSM 실제 건물 윤곽(tools/town-gen/out/structure_a/buildings.json, buildings_prep.py) → 바닥 = 윤곽 그대로인 단순 압출 상자.
/// 높이 = height 태그 → building:levels × 3m → 없으면 2~4층(가설, osm id 해시로 결정적) × 3m.
/// 앉히기 = 규칙 집과 같은 문턱(SplineTownSettings.seatGround/seatWall/seatTerraceMax): 0 땅에 바로(≤0.3m, 밑 지형 돋움) · 1 벽 내림(≤1.2m) · 2 반지하(≤3m) · 3 계단식(>3m: 바닥 = 최저+3m, 오르막 쪽 지형을 바닥까지 깎음).
/// 몸체는 밑단(둘레 지형 최저 −0.3m)부터 지붕까지 닫힌 상자라 아래 틈이 안 생긴다.
/// 길 겹침: 윤곽 ↔ 길 중심선 최소 거리로 차로(≤3m)·보도(≤HalfSection)·어깨(≤HalfShoulder)·골목(≤골목 반폭) 구분. 기본은 차로·보도·골목에 걸친 윤곽을 뺀다(dropOnRoad).
/// </summary>
public static class TownGenOsmBuildings
{
    public const double LevelHeight = 3.0;   // 가설
    public class B
    {
        public int id; public string osm, type, zone; public Vec2[] ring; public double area; public double? levels, height; public Vec2 c;
        public double minX, maxX, minZ, maxZ;
        public string road = "none"; public double roadDist = double.MaxValue; public string drop; public bool overUnderRoad;   // drop = null 이면 놓음
        public int seat = -1; public double floorY, bottomY, topY, terrainMin, terrainMax, cutDepth; public bool levelsGuessed;
    }

    public static List<B> Load(string path)
    {
        var doc = JObject.Parse(File.ReadAllText(path));
        var list = new List<B>();
        foreach (var j in doc["buildings"])
        {
            var ring = j["ring"].Select(p => new Vec2(p[0].Value<double>(), p[1].Value<double>())).ToArray();
            var b = new B
            {
                id = j["id"].Value<int>(), osm = j["osm"].ToString(), type = j["building"].ToString(), zone = j["zone"].ToString(), ring = ring, area = j["area_m2"].Value<double>(),
                levels = j["levels"].Type == JTokenType.Null ? (double?)null : j["levels"].Value<double>(), height = j["height"].Type == JTokenType.Null ? (double?)null : j["height"].Value<double>(),
                c = new Vec2(j["centroid"][0].Value<double>(), j["centroid"][1].Value<double>()),
            };
            b.minX = ring.Min(p => p.x); b.maxX = ring.Max(p => p.x); b.minZ = ring.Min(p => p.z); b.maxZ = ring.Max(p => p.z);
            list.Add(b);
        }
        return list;
    }

    // ---- 기하
    public static bool Inside(Vec2 p, Vec2[] r)
    {
        bool c = false;
        for (int i = 0, j = r.Length - 1; i < r.Length; j = i++)
            if ((r[i].z > p.z) != (r[j].z > p.z) && p.x < (r[j].x - r[i].x) * (p.z - r[i].z) / (r[j].z - r[i].z) + r[i].x) c = !c;
        return c;
    }
    static double SegPt(Vec2 p, Vec2 a, Vec2 b)
    {
        double dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz;
        double u = L2 > 1e-12 ? Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / L2)) : 0;
        return SplineEdge.Dist(p, new Vec2(a.x + dx * u, a.z + dz * u));
    }
    static bool SegX(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
    {
        double O(Vec2 p, Vec2 q, Vec2 r) => (q.x - p.x) * (r.z - p.z) - (q.z - p.z) * (r.x - p.x);
        return O(a, b, c) * O(a, b, d) < 0 && O(c, d, a) * O(c, d, b) < 0;
    }
    static double SegSeg(Vec2 a, Vec2 b, Vec2 c, Vec2 d) => SegX(a, b, c, d) ? 0 : Math.Min(Math.Min(SegPt(a, c, d), SegPt(b, c, d)), Math.Min(SegPt(c, a, b), SegPt(d, a, b)));
    /// <summary>윤곽(닫힌 고리) ↔ 선분 최소 거리. 선분 끝이 윤곽 안이면 0.</summary>
    static double RingSeg(Vec2[] r, Vec2 a, Vec2 b)
    {
        if (Inside(a, r) || Inside(b, r)) return 0;
        double m = double.MaxValue;
        for (int i = 0; i < r.Length; i++) m = Math.Min(m, SegSeg(r[i], r[(i + 1) % r.Length], a, b));
        return m;
    }
    public static double DistToRing(Vec2 p, Vec2[] r)
    {
        if (Inside(p, r)) return 0;
        double m = double.MaxValue; for (int i = 0; i < r.Length; i++) m = Math.Min(m, SegPt(p, r[i], r[(i + 1) % r.Length])); return m;
    }

    /// <summary>길 겹침 분류 + 바다·광장·작은 광장 겹침이면 뺀다. dropOnRoad = 차로·보도·골목에 걸친 윤곽도 뺀다.
    /// segIgnore(a, b) = true 인 길 선분은 겹침 판정에서 뺀다(조각 11: OSM tunnel/covered 차도 — 건물 아래로 지나감, b.overUnderRoad 로만 표시).
    /// lenient(b) = true 인 건물은 차로(≤HalfCarriageway) 겹침일 때만 뺀다(조각 11 중심: 보도·골목 겹침은 건물을 살리고 길이 밑으로 숨음).</summary>
    public static JObject Classify(List<B> bs, SplineTownResult res, SplineTownSettings s, bool dropOnRoad, Vec2[] villagePlaza, Func<Vec2, Vec2, bool> segIgnore = null, Func<B, bool> lenient = null)
    {
        double reachMax = s.HalfShoulder + 1;
        var segs = new List<(Vec2 a, Vec2 b, bool alley, double h)>(); var under = new List<(Vec2 a, Vec2 b)>();
        foreach (var e in res.edges) for (int k = 0; k + 1 < e.pts.Count; k++) { if (segIgnore != null && segIgnore(e.pts[k], e.pts[k + 1])) under.Add((e.pts[k], e.pts[k + 1])); else segs.Add((e.pts[k], e.pts[k + 1], e.alley, e.alley ? Math.Max(e.AlleyHalf(k, s.alleyHalfWidthMeters), e.AlleyHalf(k + 1, s.alleyHalfWidthMeters)) : 0)); }
        const double hc = 32; var hash = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < segs.Count; i++)
        {
            var (a, b, _, _) = segs[i];
            for (int cx = (int)Math.Floor((Math.Min(a.x, b.x) - reachMax) / hc); cx <= (int)Math.Floor((Math.Max(a.x, b.x) + reachMax) / hc); cx++)
                for (int cz = (int)Math.Floor((Math.Min(a.z, b.z) - reachMax) / hc); cz <= (int)Math.Floor((Math.Max(a.z, b.z) + reachMax) / hc); cz++)
                { if (!hash.TryGetValue((cx, cz), out var l)) hash[(cx, cz)] = l = new List<int>(); l.Add(i); }
        }
        var plazaNo = res.plaza?.Corners(0.5);
        int nCarr = 0, nSide = 0, nShoulder = 0, nAlley = 0;
        foreach (var b in bs)
        {
            b.drop = null; b.road = "none"; b.roadDist = double.MaxValue; b.overUnderRoad = false;
            foreach (var (ua, ub) in under) { if (Math.Max(ua.x, ub.x) < b.minX - 4 || Math.Min(ua.x, ub.x) > b.maxX + 4 || Math.Max(ua.z, ub.z) < b.minZ - 4 || Math.Min(ua.z, ub.z) > b.maxZ + 4) continue; if (RingSeg(b.ring, ua, ub) <= s.HalfCarriageway) { b.overUnderRoad = true; break; } }
            var cand = new HashSet<int>();
            for (int cx = (int)Math.Floor(b.minX / hc); cx <= (int)Math.Floor(b.maxX / hc); cx++)
                for (int cz = (int)Math.Floor(b.minZ / hc); cz <= (int)Math.Floor(b.maxZ / hc); cz++)
                    if (hash.TryGetValue((cx, cz), out var l)) foreach (var i in l) cand.Add(i);
            double dMain = double.MaxValue, dAlley = double.MaxValue, alleyIn = double.MaxValue;   // alleyIn = 골목 선까지 거리 − 그 자리 반폭(≤0 = 골목 돌에 걸침, 조각 13 점마다 반폭)
            foreach (var i in cand) { var (a, bb, al, hh) = segs[i]; double d = RingSeg(b.ring, a, bb); if (al) { dAlley = Math.Min(dAlley, d); alleyIn = Math.Min(alleyIn, d - hh); } else dMain = Math.Min(dMain, d); }
            b.roadDist = Math.Min(dMain, dAlley);
            if (dMain <= s.HalfCarriageway) { b.road = "carriageway"; nCarr++; }
            else if (dMain <= s.HalfSection) { b.road = "sidewalk"; nSide++; }
            else if (alleyIn <= 0) { b.road = "alley"; nAlley++; }
            else if (dMain <= s.HalfShoulder) { b.road = "shoulder"; nShoulder++; }
            if (IsSea(b.c, res, s)) b.drop = "sea";
            else if (dropOnRoad && (b.road == "carriageway" || ((lenient == null || !lenient(b)) && (b.road == "sidewalk" || b.road == "alley")))) b.drop = "road_" + b.road;
            else if (plazaNo != null && (b.ring.Any(p => Inside(p, plazaNo)) || plazaNo.Any(p => Inside(p, b.ring)))) b.drop = "plaza";
            else if (villagePlaza != null && (b.ring.Any(p => Inside(p, villagePlaza)) || villagePlaza.Any(p => Inside(p, b.ring)))) b.drop = "village_plaza";
        }
        var byZone = new JObject();
        foreach (var z in new[] { "centre", "edge", "coast", "conca" })
        {
            var zb = bs.Where(x => x.zone == z).ToList();
            byZone[z] = new JObject { ["count"] = zb.Count, ["carriageway"] = zb.Count(x => x.road == "carriageway"), ["sidewalk"] = zb.Count(x => x.road == "sidewalk"), ["alley"] = zb.Count(x => x.road == "alley"), ["shoulder"] = zb.Count(x => x.road == "shoulder") };
        }
        var drops = new JObject(); foreach (var g in bs.Where(x => x.drop != null).GroupBy(x => x.drop)) drops[g.Key] = g.Count();
        return new JObject
        {
            ["rule"] = $"outline↔road centreline min distance: carriageway ≤{s.HalfCarriageway}m · sidewalk ≤{s.HalfSection}m · alley ≤{s.alleyHalfWidthMeters}m · shoulder ≤{s.HalfShoulder}m (generated road section widths, not OSM widths)",
            ["overlapRoad"] = nCarr + nSide + nAlley + nShoulder, ["carriageway"] = nCarr, ["sidewalk"] = nSide, ["alley"] = nAlley, ["shoulderOnly"] = nShoulder,
            ["byZone"] = byZone, ["dropOnRoad"] = dropOnRoad, ["dropped"] = drops, ["overUnderRoadKept"] = bs.Count(x => x.overUnderRoad && x.drop == null),
        };
    }

    static bool IsSea(Vec2 p, SplineTownResult res, SplineTownSettings s)
    {
        int c = (int)Math.Floor(p.x / s.seaCellMeters), r = (int)Math.Floor(p.z / s.seaCellMeters);
        return res.sea.InBounds(c, r) && res.sea.sea[res.sea.Index(c, r)];
    }

    /// <summary>앉히기(4 모드) + 지형 정점 깎기/돋우기. 길 단면 영향권 정점은 안 건드린다.</summary>
    public static JObject Seat(List<B> bs, SplineTownResult res, SplineTownSettings s)
    {
        var t = res.terrain; int[] count = new int[4]; int cut = 0, fill = 0, roadKept = 0; double diag = t.step * Math.Sqrt(2) + 0.05;
        var plazaNo = res.plaza?.Corners(0.5);
        var ebox = res.edges.Select(e => (x0: e.pts.Min(q => q.x), x1: e.pts.Max(q => q.x), z0: e.pts.Min(q => q.z), z1: e.pts.Max(q => q.z))).ToArray();
        bool NearRoad(Vec2 v)
        {
            if (plazaNo != null && Inside(v, plazaNo)) return true;
            for (int ei = 0; ei < res.edges.Count; ei++)
            {
                var e = res.edges[ei]; var bx = ebox[ei];
                double reach = (e.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder) + diag;
                if (v.x < bx.x0 - reach || v.x > bx.x1 + reach || v.z < bx.z0 - reach || v.z > bx.z1 + reach) continue;
                for (int k = 0; k + 1 < e.pts.Count; k++) if (SegPt(v, e.pts[k], e.pts[k + 1]) < reach) return true;
            }
            return false;
        }
        int guessed = 0;
        foreach (var b in bs.Where(x => x.drop == null))
        {
            // 윤곽 안 1m 격자 + 꼭짓점에서 지형 최저·최고
            double mn = double.MaxValue, mx = double.MinValue;
            void Smp(double x, double z) { double y = t.Surface(x, z); mn = Math.Min(mn, y); mx = Math.Max(mx, y); }
            foreach (var p in b.ring) Smp(p.x, p.z);
            for (double x = Math.Floor(b.minX) + 0.5; x < b.maxX; x += 1) for (double z = Math.Floor(b.minZ) + 0.5; z < b.maxZ; z += 1) if (Inside(new Vec2(x, z), b.ring)) Smp(x, z);
            b.terrainMin = mn; b.terrainMax = mx;
            double exposed = mx - mn;
            b.seat = exposed <= s.seatGroundMeters ? 0 : exposed <= s.seatWallMeters ? 1 : exposed <= s.seatTerraceMaxMeters ? 2 : 3;
            b.floorY = b.seat == 3 ? mn + s.seatTerraceMaxMeters : mx;
            b.cutDepth = Math.Max(0, mx - b.floorY);
            double h = b.height ?? (b.levels.HasValue ? Math.Max(1, b.levels.Value) * LevelHeight : 0);
            if (h <= 0) { uint hh = (uint)(b.osm.GetHashCode()) * 2654435761u; h = (2 + (int)(hh >> 16) % 3) * LevelHeight; b.levelsGuessed = true; guessed++; }
            b.topY = b.floorY + h;
            // 지형: 윤곽(+0.3m) 안 2m 정점 — 바닥보다 높으면 바닥 −0.02 로 깎고, 땅에 바로(0)면 낮은 정점도 돋움
            int i0 = Math.Max(0, (int)Math.Floor((b.minX - 0.3) / t.step)), i1 = Math.Min(t.nx - 1, (int)Math.Ceiling((b.maxX + 0.3) / t.step));
            int j0 = Math.Max(0, (int)Math.Floor((b.minZ - 0.3) / t.step)), j1 = Math.Min(t.nz - 1, (int)Math.Ceiling((b.maxZ + 0.3) / t.step));
            double lowest = mn;
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    var v = new Vec2(i * t.step, j * t.step); if (DistToRing(v, b.ring) > 0.3) continue;
                    int idx = j * t.nx + i; double target = b.floorY - 0.02;
                    bool near = NearRoad(v);
                    if (t.h[idx] > target) { if (near) { roadKept++; continue; } t.h[idx] = target; cut++; }
                    else if (b.seat == 0 && t.h[idx] < target - 0.03 && !near) { t.h[idx] = target; fill++; }
                    lowest = Math.Min(lowest, t.h[idx]);
                }
            b.bottomY = Math.Min(mn, lowest) - 0.3;
            count[b.seat]++;
        }
        var placed = bs.Where(x => x.drop == null).ToList();
        return new JObject
        {
            ["rule"] = $"exposed = terrain max − min under outline: ≤{s.seatGroundMeters} ground(fill) · ≤{s.seatWallMeters} wall_down · ≤{s.seatTerraceMaxMeters} half_basement · more = terrace(floor = min+{s.seatTerraceMaxMeters}, cut uphill). floor = terrain max except terrace. body = closed box from (lowest ground −0.3) to floor + height",
            ["ground"] = count[0], ["wall_down"] = count[1], ["half_basement"] = count[2], ["terrace_over_3m"] = count[3],
            ["maxCut_m"] = placed.Count == 0 ? 0 : Math.Round(placed.Max(x => x.cutDepth), 2), ["maxExposed_m"] = placed.Count == 0 ? 0 : Math.Round(placed.Max(x => x.floorY - x.terrainMin), 2),
            ["terrainVertsCut"] = cut, ["terrainVertsFilled"] = fill, ["vertsKeptForRoad"] = roadKept,
            ["heightFrom"] = new JObject { ["heightTag"] = placed.Count(x => x.height.HasValue), ["levelsTag"] = placed.Count(x => !x.height.HasValue && x.levels.HasValue), ["guessed_2to4"] = placed.Count(x => x.levelsGuessed) },
            ["levelHeight_m"] = LevelHeight,
        };
    }

    static Vector2 Tile(int col, int rowFromTop) => new Vector2((col + 0.5f) / 8f, 1f - (rowFromTop + 0.5f) / 8f);
    static readonly Vector2[] WallTiles = { Tile(0, 7), Tile(3, 6), Tile(6, 3) };   // 흰 회반죽 · 베이지 · 모래색
    static readonly Vector2 RoofTile = Tile(2, 4);                                  // 테라코타 빨강

    /// <summary>압출 상자 메시 — 재질 3종(해시로 나눔)마다 MeshBuilder 하나.</summary>
    public static int Build(List<B> bs, Transform parent, Material[] mats)
    {
        var mbs = mats.Select(_ => new TownGenSpline.MeshBuilder()).ToArray();
        int tris = 0, badTri = 0;
        foreach (var b in bs.Where(x => x.drop == null))
        {
            var mb = mbs[(int)((uint)b.osm.GetHashCode() % (uint)mbs.Length)];
            var r = b.ring; float y0 = (float)b.bottomY, y1 = (float)b.topY;
            // LofiAtlas(8×8 칸) 한 칸 가운데 고정 UV = 단색: 벽 = 회반죽 3종 중 하나, 지붕 = 테라코타
            var wallUv = WallTiles[(int)((uint)(b.osm.GetHashCode() * 31) % (uint)WallTiles.Length)];
            for (int i = 0; i < r.Length; i++)
            {
                var a = r[i]; var c = r[(i + 1) % r.Length];
                var outN = new Vector3((float)(c.z - a.z), 0, (float)-(c.x - a.x));   // 반시계 고리 → 바깥 = (dz, −dx)
                mb.Quad(new Vector3((float)a.x, y0, (float)a.z), new Vector3((float)c.x, y0, (float)c.z), new Vector3((float)c.x, y1, (float)c.z), new Vector3((float)a.x, y1, (float)a.z), outN);
                for (int q = mb.uv.Count - 4; q < mb.uv.Count; q++) mb.uv[q] = wallUv;
            }
            var tri = EarClip(r);
            if (tri == null) { badTri++; tri = Enumerable.Range(1, r.Length - 2).SelectMany(k => new[] { 0, k, k + 1 }).ToList(); }
            for (int k = 0; k < tri.Count; k += 3)
                mb.Tri(new Vector3((float)r[tri[k]].x, y1, (float)r[tri[k]].z), new Vector3((float)r[tri[k + 1]].x, y1, (float)r[tri[k + 1]].z), new Vector3((float)r[tri[k + 2]].x, y1, (float)r[tri[k + 2]].z));
            for (int q = mb.uv.Count - tri.Count; q < mb.uv.Count; q++) mb.uv[q] = RoofTile;
        }
        var root = new GameObject("OsmBuildings"); root.transform.SetParent(parent, false);
        for (int m = 0; m < mbs.Length; m++)
            if (mbs[m].t.Count > 0) tris += TownGenSpline.Obj("OsmBuildings_" + m, root.transform, mbs[m].Build("OsmBuildings_" + m), mats[m]).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        if (badTri > 0) Debug.LogWarning($"[TownGen] OSM 건물 지붕 귀자르기 실패 {badTri} — 부채꼴로 대신");
        return tris;
    }

    /// <summary>반시계 단순 다각형 귀 자르기. 실패하면 null.</summary>
    internal static List<int> EarClip(Vec2[] r)
    {
        var idx = Enumerable.Range(0, r.Length).ToList(); var outT = new List<int>(); int guard = 0;
        double Cross(Vec2 a, Vec2 b, Vec2 c) => (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
        while (idx.Count > 3 && guard++ < 10000)
        {
            bool found = false;
            for (int k = 0; k < idx.Count; k++)
            {
                int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                if (Cross(r[ia], r[ib], r[ic]) <= 1e-9) continue;
                bool any = false;
                foreach (var q in idx)
                {
                    if (q == ia || q == ib || q == ic) continue;
                    var p = r[q];
                    if (Cross(r[ia], r[ib], p) >= 0 && Cross(r[ib], r[ic], p) >= 0 && Cross(r[ic], r[ia], p) >= 0) { any = true; break; }
                }
                if (any) continue;
                outT.Add(ia); outT.Add(ib); outT.Add(ic); idx.RemoveAt(k); found = true; break;
            }
            if (!found) return null;
        }
        if (idx.Count == 3) { outT.Add(idx[0]); outT.Add(idx[1]); outT.Add(idx[2]); }
        return outT;
    }

    /// <summary>규칙 집 채우기 스위치용: 놓인 실제 건물 윤곽에서 gap m 안이면 false(규칙 집 금지).</summary>
    public static Func<Vec2, bool> EmptyAreaFilter(List<B> bs, double gap)
    {
        var kept = bs.Where(x => x.drop == null).ToList();
        const double hc = 50; var hash = new Dictionary<(int, int), List<B>>();
        foreach (var b in kept)
            for (int cx = (int)Math.Floor((b.minX - gap) / hc); cx <= (int)Math.Floor((b.maxX + gap) / hc); cx++)
                for (int cz = (int)Math.Floor((b.minZ - gap) / hc); cz <= (int)Math.Floor((b.maxZ + gap) / hc); cz++)
                { if (!hash.TryGetValue((cx, cz), out var l)) hash[(cx, cz)] = l = new List<B>(); l.Add(b); }
        return p =>
        {
            if (!hash.TryGetValue(((int)Math.Floor(p.x / hc), (int)Math.Floor(p.z / hc)), out var l)) return true;
            foreach (var b in l) { if (p.x < b.minX - gap || p.x > b.maxX + gap || p.z < b.minZ - gap || p.z > b.maxZ + gap) continue; if (DistToRing(p, b.ring) < gap) return false; }
            return true;
        };
    }
}
