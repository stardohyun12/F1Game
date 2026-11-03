#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// D-295 지형 감사 — 「뚫려 있거나 울퉁불퉁한」 땅을 사람 눈 대신 숫자로 찾는다. 에디트 모드에서 `return TerrainAudit.Report();`
/// (→ docs/world/TERRAIN-AUDIT.md + docs/qa/evidence/terrain-audit/map.png), 불변 검사는 WorldInvariants 가 Lines() 로 부른다(WARN).
///  ① 틈(grid): 2 m 격자로 위에서 쏜 레이가 땅 콜라이더(지형·길·보도·축대·부두)를 하나도 못 맞히는데 사방 6 m 안에 땅이 있는 칸 — 바다·맵 밖은 사방이 비어 빠진다.
///  ① 가는 틈(edge): 지형 메시 열린 변(삼각형 하나만 쓰는 변, 같은 자리 정점은 합쳐 셈) 중 맵 둘레 4 m·바다 밖 — 원본 지형엔 0 이라 전부 틈(T자 이음매 포함).
///  ② 뜬/묻힌 조각: 길·보도 메시의 위를 보는 정점이 지형보다 0.3 m 넘게 떠 있거나(가장자리 = 밑이 보이는 턱) 0.3 m 넘게 묻힘(지형이 길을 뚫고 올라옴).
///  ③ 울퉁불퉁: RoadSurface 중심선 1 m 간격 실제 노면 높이의 2차 차분 > 0.25 m · 길 폭 80% 안 좌우 기울기 > 15% · 노면 위로 지형이 덮인 점.
///  ④ 흰 조각: 켜진 렌더러에 재질 없음·Default-Material·텍스처 없는 흰 Lit·에러 셰이더.
///  ⑤ 봇 끼임: docs/qa/tmp/townv6-bot-cover.txt(TownPathBot cover) 의 끼임·못오름·추락·이탈 좌표(있으면).
/// 임계값은 전부 가설(D-295) — 도현 판정 전까지 WARN.
/// </summary>
public static class TerrainAudit
{
    public const float Cell = 2f, FloatTol = 0.3f, D2Tol = 0.25f, CrossTol = 0.15f, SeaY = 0.3f;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public struct Mark { public string kind, owner; public Vector3 p; public float v; }
    public class Result
    {
        public List<Mark> marks = new List<Mark>();
        public Bounds area; public int nx, nz; public float cell = Cell; public float[] top; public byte[] cls;   // cls 0 없음·1 지형·2 길·3 그 밖 땅
        public long ms; public int cells, roadVerts, clPts; public string stages = "";
        public int Count(string k) => marks.Count(m => m.kind == k);
    }
    public static readonly string[] Kinds = { "틈", "가는 틈", "뜬 길", "묻힌 길", "길 굴곡", "길 옆기울기", "길 위 지형", "흰 조각", "봇 멈춤" };

    /// <summary>변·정점 키(long) 해시 섞기 — long 기본 해시는 (hi ^ lo) 라 변 (a, a+1) 이 해시 20여 개로 몰려 사전이 사실상 n² 가 된다(2026-11-04 에디터 멈춤 원인).</summary>
    public sealed class MixLong : IEqualityComparer<long>
    {
        public static readonly MixLong I = new MixLong();
        public bool Equals(long a, long b) => a == b;
        public int GetHashCode(long k) { ulong x = (ulong)k * 0x9E3779B97F4A7C15UL; return (int)(x >> 32) ^ (int)x; }
    }
    /// <summary>1 cm 격자 정점 키 — 축마다 21비트(±10 km).</summary>
    static long PosKey(Vector3 p, float scale) => ((long)(Mathf.RoundToInt(p.x * scale) + (1 << 20)) << 42) | ((long)(Mathf.RoundToInt(p.y * scale) + (1 << 20)) << 21) | (long)(Mathf.RoundToInt(p.z * scale) + (1 << 20));

    static Collider terrainCol;
    static Rect? Reg;   // 감사 범위(xz) — null 이면 맵 전체
    static bool In(Vector3 p) => !Reg.HasValue || Reg.Value.Contains(new Vector2(p.x, p.z));
    static bool In(Bounds b) => !Reg.HasValue || Reg.Value.Overlaps(new Rect(b.min.x, b.min.z, b.size.x, b.size.z));
    static readonly RaycastHit[] Buf = new RaycastHit[24];
    static readonly Dictionary<Collider, byte> ClsCache = new Dictionary<Collider, byte>();

    /// <summary>땅 종류: 1 지형 · 2 길/보도(RoadsV2·TerraceBlock·DeadEnds·RoadSurface) · 3 부두·건물 바닥 등 그 밖(TownV6_Gray 아래) · 0 땅 아님(소품·차·트리거).</summary>
    static byte ClsOf(Collider c)
    {
        if (ClsCache.TryGetValue(c, out var k)) return k;
        k = 0;
        if (c == terrainCol) k = 1;
        else if (c.GetComponentInParent<ArcadeCarController>() == null)
        {
            for (var t = c.transform; t != null; t = t.parent)
            {
                string n = t.name;
                if (n == "RoadsV2" || n == "TerraceBlock" || n == "DeadEnds" || t.GetComponent<RoadSurface>() != null) { k = 2; break; }
                if (n == "Piers" || n == "PierRails" || n == "D198_Buildings" || n == "OnFoot_HomeGarage") { k = 3; break; }
                if (n == "Delivery" || n == "Roadside" || n == "Greens") break;
                if (n == "TownV6_Gray") { k = 3; break; }
            }
        }
        ClsCache[c] = k; return k;
    }

    /// <summary>위에서 아래로 쏴 맞은 땅 중 가장 높은 것(종류 포함). 소품·트리거는 건너뜀.</summary>
    static bool TopGround(Vector3 xz, float yTop, float len, out float y, out byte cls, out Collider col)
    {
        y = float.NegativeInfinity; cls = 0; col = null;
        int n = Physics.RaycastNonAlloc(new Vector3(xz.x, yTop, xz.z), Vector3.down, Buf, len, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            byte k = ClsOf(Buf[i].collider); if (k == 0) continue;
            if (Buf[i].point.y > y) { y = Buf[i].point.y; cls = k; col = Buf[i].collider; }
        }
        return cls != 0;
    }

    static bool TerrainY(Vector3 p, out float y)
    {
        y = 0f;
        if (terrainCol == null || !terrainCol.Raycast(new Ray(new Vector3(p.x, 200f, p.z), Vector3.down), out var h, 400f)) return false;
        y = h.point.y; return true;
    }

    /// <summary>region = 감사할 xz 사각형(null = 맵 전체), cell = 격자 칸(m). 작은 범위로 먼저 시간을 재고 전체를 돈다.</summary>
    public static Result Run(Rect? region = null, float cell = Cell)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var R = new Result { cell = cell }; ClsCache.Clear(); Reg = region;
        var tgo = GameObject.Find("TownV6_Gray/TownV6_Terrain");
        if (tgo == null) { R.marks.Add(new Mark { kind = "틈", owner = "TownV6_Terrain 없음" }); return R; }
        terrainCol = tgo.GetComponent<MeshCollider>();
        Physics.SyncTransforms();
        var full = tgo.GetComponent<Renderer>().bounds; var tb = full;
        if (region.HasValue) { var rg = region.Value; tb = new Bounds(new Vector3(rg.center.x, full.center.y, rg.center.y), new Vector3(rg.width, full.size.y, rg.height)); }
        R.area = tb;
        float yTop = tb.max.y + 20f, len = tb.size.y + 60f;

        // ① 격자
        R.nx = Mathf.CeilToInt(tb.size.x / cell); R.nz = Mathf.CeilToInt(tb.size.z / cell); R.cells = R.nx * R.nz;
        R.top = new float[R.cells]; R.cls = new byte[R.cells];
        for (int iz = 0; iz < R.nz; iz++) for (int ix = 0; ix < R.nx; ix++)
        {
            var p = new Vector3(tb.min.x + (ix + .5f) * cell, 0, tb.min.z + (iz + .5f) * cell);
            int i = iz * R.nx + ix;
            if (TopGround(p, yTop, len, out float y, out byte k, out _)) { R.top[i] = y; R.cls[i] = k; } else R.top[i] = float.NaN;
        }
        int reach = Mathf.CeilToInt(6f / cell);
        for (int iz = 0; iz < R.nz; iz++) for (int ix = 0; ix < R.nx; ix++)
        {
            int i = iz * R.nx + ix; if (R.cls[i] != 0) continue;
            // 사방(동서남북) 모두 6 m 안에 바다 위 땅이 있어야 「안쪽 구멍」 — 해안선·맵 밖은 한쪽 이상이 비어 빠진다
            bool all = true;
            foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                bool f = false;
                for (int s = 1; s <= reach && !f; s++) { int jx = ix + dx * s, jz = iz + dz * s; if (jx < 0 || jz < 0 || jx >= R.nx || jz >= R.nz) break; int j = jz * R.nx + jx; if (R.cls[j] != 0 && R.top[j] > SeaY) f = true; }
                if (!f) { all = false; break; }
            }
            if (all) R.marks.Add(new Mark { kind = "틈", owner = "격자", p = new Vector3(tb.min.x + (ix + .5f) * cell, NeighborY(R, ix, iz), tb.min.z + (iz + .5f) * cell) });
        }

        R.stages += "격자 " + sw.ElapsedMilliseconds + " · ";
        // ① 가는 틈 — 지형 메시 가장자리 변
        var tm = tgo.GetComponent<MeshFilter>().sharedMesh; var tt = tgo.transform;
        if (tm != null && tm.isReadable)
        {
            var v = tm.vertices; var tri = tm.triangles;
            var weld = Weld(v);   // UV 이음매로 갈라진 같은 자리 정점은 한 점 — 안 합치면 모든 이음매가 가장자리로 잡힌다
            var edges = new Dictionary<long, int>(tri.Length, MixLong.I);
            for (int t = 0; t < tri.Length; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = weld[tri[t + e]], b = weld[tri[t + (e + 1) % 3]]; if (a == b) continue; long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    edges[key] = edges.TryGetValue(key, out int c) ? c + 1 : 1;
                }
            R.stages += "가장자리 변 " + edges.Count(kv => kv.Value == 1) + "/" + edges.Count + " · ";
            // 원본 지형(0433)은 안쪽 열린 변 0 — 안쪽에 열린 변이 있으면 그 자체가 틈(T자 이음매는 양옆에 땅이 있어 광선으론 안 잡힌다)
            float edgeMargin = 4f;
            foreach (var kv in edges)
            {
                if (kv.Value != 1) continue;
                var a = tt.TransformPoint(v[(int)(kv.Key >> 32)]); var b = tt.TransformPoint(v[(int)(kv.Key & 0xffffffff)]);
                var m = (a + b) * .5f;
                if (m.y <= SeaY || m.x < full.min.x + edgeMargin || m.x > full.max.x - edgeMargin || m.z < full.min.z + edgeMargin || m.z > full.max.z - edgeMargin || !In(m)) continue;
                R.marks.Add(new Mark { kind = "가는 틈", owner = "TownV6_Terrain 열린 변(T자)", p = m, v = (b - a).magnitude });
            }
        }

        R.stages += "가장자리 " + sw.ElapsedMilliseconds + " · ";
        // ② 길·보도 정점 vs 지형
        var roadRenderers = new List<MeshFilter>();
        foreach (var n in new[] { "TownV6_Gray/RoadsV2", "TownV6_Gray/TerraceBlock/Walkways", "TownV6_Gray/TerraceBlock/Driveway", "TownV6_Gray/TerraceBlock/DirtPath_door_garden", "TownV6_Gray/DeadEnds" })
        {
            var g = GameObject.Find(n); if (g == null) continue;
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>(false)) if (mf.sharedMesh != null && mf.sharedMesh.isReadable && !mf.name.StartsWith("DEADEND_wall") && (mf.GetComponent<Renderer>() == null || In(mf.GetComponent<Renderer>().bounds))) roadRenderers.Add(mf);
        }
        foreach (var mf in roadRenderers)
        {
            var mesh = mf.sharedMesh; var vs = mesh.vertices; var ns = mesh.normals; var tr = mf.transform;
            var bnd = BoundaryVerts(mesh);
            int fl = 0, bu = 0; Vector3 fp = default, bp = default; float fmax = 0, bmax = 0;
            for (int i = 0; i < vs.Length; i++)
            {
                if (ns.Length == vs.Length && tr.TransformDirection(ns[i]).y < .7f) continue;
                var w = tr.TransformPoint(vs[i]); if (!In(w)) continue; R.roadVerts++;
                if (!TerrainY(w, out float ty)) continue;
                float dy = w.y - ty;
                // 떠 있음은 가장자리 정점만(가운데가 떠도 밑이 안 보인다), 묻힘은 전부(지형이 길을 뚫고 올라온다)
                if (dy > FloatTol && bnd.Contains(i)) { fl++; if (dy > fmax) { fmax = dy; fp = w; } AddThin(R, "뜬 길", Path(tr), w, dy); }
                else if (dy < -FloatTol) { bu++; if (-dy > bmax) { bmax = -dy; bp = w; } AddThin(R, "묻힌 길", Path(tr), w, dy); }
            }
        }

        R.stages += "정점 " + sw.ElapsedMilliseconds + " · ";
        // ③ 중심선 따라 노면
        foreach (var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
        {
            if (rs.centerline == null || rs.centerline.Length < 3 || rs.kind == "stairs") continue;
            var cl = Resample(rs.centerline, 1f); var hs = new float[cl.Count]; var ok = new bool[cl.Count];
            string own = Path(rs.transform);
            for (int i = 0; i < cl.Count; i++)
            {
                var c = cl[i]; if (!In(c)) { ok[i] = false; continue; } R.clPts++;
                ok[i] = SurfaceY(c, out hs[i], out bool terrOver);
                if (terrOver) AddThin(R, "길 위 지형", own, new Vector3(c.x, hs[i], c.z), 0);
                // 옆기울기
                if (i > 0 && i < cl.Count - 1)
                {
                    var f = cl[i + 1] - cl[i - 1]; f.y = 0; if (f.sqrMagnitude < 1e-4f) continue;
                    var side = new Vector3(-f.z, 0, f.x).normalized;
                    float w = (rs.widths != null && rs.widths.Length == rs.centerline.Length ? rs.widths[Mathf.Clamp(Mathf.RoundToInt((float)i / cl.Count * rs.widths.Length), 0, rs.widths.Length - 1)] : rs.width);
                    if (w < 1.5f) continue;
                    float half = w * .4f;
                    if (SurfaceY(c + side * half, out float hl, out _) && SurfaceY(c - side * half, out float hr, out _))
                    {
                        float s = Mathf.Abs(hl - hr) / (2f * half);
                        if (s > CrossTol) AddThin(R, "길 옆기울기", own, new Vector3(c.x, hs[i], c.z), s);
                    }
                }
            }
            for (int i = 1; i < cl.Count - 1; i++)
            {
                if (!ok[i - 1] || !ok[i] || !ok[i + 1]) continue;
                float d2 = hs[i - 1] - 2f * hs[i] + hs[i + 1];
                if (Mathf.Abs(d2) > D2Tol) AddThin(R, "길 굴곡", own, new Vector3(cl[i].x, hs[i], cl[i].z), d2);
            }
        }

        R.stages += "중심선 " + sw.ElapsedMilliseconds + " · ";
        // ④ 흰 조각
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r.gameObject.layer == 5 || !In(r.bounds)) continue;
            string why = null;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) { why = "재질 없음"; break; }
                if (m.name.StartsWith("Default-Material") || m.name == "Default-Diffuse") { why = m.name; break; }
                if (m.shader == null || m.shader.name.Contains("InternalErrorShader")) { why = "에러 셰이더"; break; }
                if (m.name == "Lit" || m.name == "Lit (Instance)") { why = "기본 Lit(" + m.shader.name + ")"; break; }
            }
            if (why != null) R.marks.Add(new Mark { kind = "흰 조각", owner = Path(r.transform) + " · " + why, p = r.bounds.center, v = r.bounds.size.magnitude });
        }

        // ⑤ 봇
        string botPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/townv6-bot-cover.txt"));
        if (System.IO.File.Exists(botPath) && System.IO.File.GetLastWriteTime(botPath) > DateTime.Now.AddDays(-2))
            foreach (var line in System.IO.File.ReadAllLines(botPath))
            {
                var f = line.Split('\t'); if (f.Length < 9 || f[4] == "ok" || f[4] == "result") continue;
                var xyz = f[7].Split(','); if (xyz.Length != 3) continue;
                if (float.TryParse(xyz[0], NumberStyles.Float, Inv, out float x) && float.TryParse(xyz[1], NumberStyles.Float, Inv, out float y) && float.TryParse(xyz[2], NumberStyles.Float, Inv, out float z))
                    if (In(new Vector3(x, y, z))) R.marks.Add(new Mark { kind = "봇 멈춤", owner = f[0] + " " + f[1] + " " + f[4], p = new Vector3(x, y, z), v = 0 });
            }

        R.ms = sw.ElapsedMilliseconds; Reg = null;
        return R;
    }

    static float NeighborY(Result R, int ix, int iz)
    {
        for (int s = 1; s < 4; s++) foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        { int jx = ix + dx * s, jz = iz + dz * s; if (jx < 0 || jz < 0 || jx >= R.nx || jz >= R.nz) continue; int j = jz * R.nx + jx; if (R.cls[j] != 0) return R.top[j]; }
        return 0f;
    }

    /// <summary>점 표식은 같은 종류·주인 0.9 m 안에 이미 있으면 안 더한다(정점이 겹친 이음매가 몇 배로 세지지 않게).</summary>
    static void AddThin(Result R, string kind, string owner, Vector3 p, float v)
    {
        for (int i = R.marks.Count - 1; i >= 0 && i >= R.marks.Count - 6; i--)
        { var m = R.marks[i]; if (m.kind == kind && m.owner == owner && (m.p - p).sqrMagnitude < .81f) return; }
        R.marks.Add(new Mark { kind = kind, owner = owner, p = p, v = v });
    }

    /// <summary>중심선 점의 실제 노면: 길 콜라이더(종류 2) 가운데 점 높이 ±3 m 안 가장 높은 것. 그 위에 지형이 5 cm 넘게 덮였으면 terrOver.</summary>
    static bool SurfaceY(Vector3 c, out float y, out bool terrOver)
    {
        y = float.NegativeInfinity; terrOver = false; float terr = float.NegativeInfinity;
        int n = Physics.RaycastNonAlloc(new Vector3(c.x, c.y + 3f, c.z), Vector3.down, Buf, 6f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            byte k = ClsOf(Buf[i].collider);
            if (k == 2 || k == 3) y = Mathf.Max(y, Buf[i].point.y); else if (k == 1) terr = Mathf.Max(terr, Buf[i].point.y);
        }
        if (float.IsNegativeInfinity(y)) { if (float.IsNegativeInfinity(terr)) return false; y = terr; return true; }   // 길 메시가 없는 자리는 지형이 노면
        if (terr > y + .05f) { terrOver = true; y = terr; }
        return true;
    }

    /// <summary>같은 자리(1 cm) 정점을 처음 나온 정점 번호로 합친다 — 돌려준 번호로 v[] 를 그대로 쓸 수 있다.</summary>
    static int[] Weld(Vector3[] v)
    {
        var first = new Dictionary<long, int>(v.Length, MixLong.I); var map = new int[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            var k = PosKey(v[i], 100f);
            if (!first.TryGetValue(k, out int id)) { id = i; first[k] = i; }
            map[i] = id;
        }
        return map;
    }

    static HashSet<int> BoundaryVerts(Mesh m)
    {
        var tri = m.triangles; var v = m.vertices;
        // 같은 자리 정점(UV 이음매)을 한 점으로 — 위치 키
        var idOf = new Dictionary<long, int>(v.Length, MixLong.I); var map = new int[v.Length];
        for (int i = 0; i < v.Length; i++) { var k = PosKey(v[i], 100f); if (!idOf.TryGetValue(k, out int id)) { id = idOf.Count; idOf[k] = id; } map[i] = id; }
        var cnt = new Dictionary<long, int>(tri.Length, MixLong.I);
        for (int t = 0; t < tri.Length; t += 3) for (int e = 0; e < 3; e++)
        { int a = map[tri[t + e]], b = map[tri[t + (e + 1) % 3]]; if (a == b) continue; long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; cnt[key] = cnt.TryGetValue(key, out int c) ? c + 1 : 1; }
        var bid = new HashSet<int>();
        foreach (var kv in cnt) if (kv.Value == 1) { bid.Add((int)(kv.Key >> 32)); bid.Add((int)(kv.Key & 0xffffffff)); }
        var res = new HashSet<int>(); for (int i = 0; i < v.Length; i++) if (bid.Contains(map[i])) res.Add(i);
        return res;
    }

    static List<Vector3> Resample(Vector3[] p, float step)
    {
        var o = new List<Vector3> { p[0] }; float carry = 0f;
        for (int i = 1; i < p.Length; i++)
        {
            var a = p[i - 1]; var b = p[i]; float L = Vector3.Distance(a, b); float t = step - carry;
            while (t <= L) { o.Add(Vector3.Lerp(a, b, t / L)); t += step; }
            carry = L - (t - step);
        }
        return o;
    }

    static string Path(Transform t) { var s = t.name; for (var p = t.parent; p != null && p.name != "TownV6_Gray"; p = p.parent) s = p.name + "/" + s; return s; }

    // ───────── 묶기 ─────────
    public class Cluster { public string kind, owner; public Vector3 c; public int n; public float vmax; }
    /// <summary>같은 종류·같은 주인(오브젝트)·같은 12 m 칸 표식을 한 덩이로(칸 해시 — 표식이 수십만이어도 한 번 훑기).</summary>
    public static List<Cluster> Clusters(Result R)
    {
        var map = new Dictionary<(string, string, int, int), Cluster>();
        foreach (var m in R.marks)
        {
            var key = (m.kind, m.owner, Mathf.FloorToInt(m.p.x / 12f), Mathf.FloorToInt(m.p.z / 12f));
            if (!map.TryGetValue(key, out var hit)) map[key] = new Cluster { kind = m.kind, owner = m.owner, c = m.p, n = 1, vmax = Mathf.Abs(m.v) };
            else { hit.c = (hit.c * hit.n + m.p) / (hit.n + 1); hit.n++; hit.vmax = Mathf.Max(hit.vmax, Mathf.Abs(m.v)); }
        }
        return map.Values.OrderByDescending(c => c.n).ToList();
    }

    /// <summary>불변(FAIL): 지형 메시 안쪽(맵 둘레 4 m·바다 밖) 열린 변 0 — T자 이음매 틈(D-295, TerrainSeal 이 막는다).</summary>
    public static List<string> OpenEdgeLines()
    {
        var l = new List<string>();
        var tgo = GameObject.Find("TownV6_Gray/TownV6_Terrain"); if (tgo == null) { l.Add("TownV6_Terrain 없음"); return l; }
        var tm = tgo.GetComponent<MeshFilter>().sharedMesh; if (tm == null || !tm.isReadable) { l.Add("지형 메시 못 읽음"); return l; }
        var tb = tgo.GetComponent<Renderer>().bounds; var tt = tgo.transform;
        var v = tm.vertices; var tri = tm.triangles; var weld = Weld(v);
        var edges = new Dictionary<long, int>(tri.Length, MixLong.I);
        for (int t = 0; t < tri.Length; t += 3) for (int e = 0; e < 3; e++)
        { int a = weld[tri[t + e]], b = weld[tri[t + (e + 1) % 3]]; if (a == b) continue; long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; edges[key] = edges.TryGetValue(key, out int c) ? c + 1 : 1; }
        int n = 0; Vector3 first = default;
        foreach (var kv in edges)
        {
            if (kv.Value != 1) continue;
            var m = (tt.TransformPoint(v[(int)(kv.Key >> 32)]) + tt.TransformPoint(v[(int)(kv.Key & 0xffffffff)])) * .5f;
            if (m.y <= SeaY || m.x < tb.min.x + 4 || m.x > tb.max.x - 4 || m.z < tb.min.z + 4 || m.z > tb.max.z - 4) continue;
            if (n++ == 0) first = m;
        }
        if (n > 0) l.Add(string.Format(Inv, "{0} 안쪽 열린 변 {1}개(첫 ({2:0.0},{3:0.0},{4:0.0})) — PitCrew/지형 T자 틈 꿰매기", tm.name, n, first.x, first.y, first.z));
        return l;
    }

    /// <summary>불변 검사 줄(WARN) — 종류별 개수, 0 이 아닌 것만.</summary>
    public static List<string> Lines()
    {
        var R = Run(); var l = new List<string>();
        foreach (var k in Kinds) { int n = R.Count(k); if (n > 0) l.Add(k + " " + n + "곳 — bash: TerrainAudit.Report() → docs/world/TERRAIN-AUDIT.md"); }
        // 2026-11-04: 변 사전 해시가 몰려 감사 한 번이 에디터를 1시간 넘게 붙잡았다 — 다시 느려지면 여기서 먼저 보인다
        if (R.ms > 10000) l.Add("감사 " + R.ms + " ms — 10 s 넘음(" + R.stages + ") · 사전 키 해시(MixLong) 확인");
        return l;
    }

    /// <summary>감사 + 보고서 + 지도. tag 가 있으면 map_{tag}.png 로(전/후 비교용).</summary>
    public static string Report(string tag = "") => Report(tag, null, Cell);
    /// <summary>범위 감사(시간 재기용): x0,z0 ~ x1,z1 사각형, 격자 cell m.</summary>
    public static string Report(string tag, float x0, float z0, float x1, float z1, float cell) => Report(tag, Rect.MinMaxRect(x0, z0, x1, z1), cell);
    public static string Report(string tag, Rect? region, float cell)
    {
        var R = Run(region, cell); var cs = Clusters(R);
        string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../.."));
        string dir = System.IO.Path.Combine(root, "docs/qa/evidence/terrain-audit"); System.IO.Directory.CreateDirectory(dir);
        string png = "map" + (tag != "" ? "_" + tag : "") + ".png";
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, png), MapPng(R));
        var sb = new StringBuilder();
        sb.Append("<!-- 자동 생성: TerrainAudit.Report(\"" + tag + "\") — 손으로 고치지 말 것 -->\n");
        sb.Append("# 지형 감사 (D-295)" + (tag != "" ? " — " + tag : "") + "\n\n");
        sb.Append(string.Format(Inv, "생성 {0:yyyy-MM-dd HH:mm} · 씬 {1} · 지형 메시 {2} · 격자 {3}×{4}({5} m) · 길 정점 {6} · 중심선 점 {7} · {8} ms\n\n",
            DateTime.Now, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, GameObject.Find("TownV6_Gray/TownV6_Terrain")?.GetComponent<MeshFilter>()?.sharedMesh?.name, R.nx, R.nz, R.cell, R.roadVerts, R.clPts, R.ms));
        sb.Append("임계(가설): 뜬/묻힘 ±" + FloatTol + " m · 굴곡 2차 차분 " + D2Tol + " m(1 m 간격) · 옆기울기 " + (CrossTol * 100) + "% · 틈 = 사방 6 m 안 땅\n\n");
        sb.Append("## 개수\n");
        foreach (var k in Kinds) sb.Append("- ").Append(k).Append(": ").Append(R.Count(k)).Append("점 · ").Append(cs.Count(c => c.kind == k)).Append("덩이\n");
        sb.Append("\n지도: `docs/qa/evidence/terrain-audit/" + png + "` — 위가 북(+z). 빨강 틈 · 주황 뜬 길 · 노랑 묻힌 길/길 위 지형 · 자홍 굴곡 · 보라 옆기울기 · 흰 테 흰 조각 · 청록 봇 멈춤\n\n");
        foreach (var k in Kinds)
        {
            var ks = cs.Where(c => c.kind == k).ToList(); if (ks.Count == 0) continue;
            sb.Append("## ").Append(k).Append(" — 큰 덩이 ").Append(Mathf.Min(25, ks.Count)).Append('/').Append(ks.Count).Append('\n');
            foreach (var c in ks.Take(25)) sb.Append(string.Format(Inv, "- ({0:0}, {1:0.0}, {2:0}) ×{3} 최대 {4:0.00} · {5}\n", c.c.x, c.c.y, c.c.z, c.n, c.vmax, c.owner));
            // 주인별 합
            var by = ks.GroupBy(c => Group(c.owner)).Select(g => (g.Key, g.Sum(c => c.n))).OrderByDescending(x => x.Item2).Take(8);
            sb.Append("  - 주인별: ").Append(string.Join(" · ", by.Select(x => x.Key + " " + x.Item2))).Append('\n');
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(root, "docs/world/TERRAIN-AUDIT" + (tag != "" ? "-" + tag : "") + ".md"), sb.ToString(), new UTF8Encoding(false));
        var head = new StringBuilder(); foreach (var k in Kinds) head.Append(k).Append('=').Append(R.Count(k)).Append(' ');
        return head.ToString().TrimEnd() + " | " + R.stages + R.ms + "ms →docs/world/TERRAIN-AUDIT" + (tag != "" ? "-" + tag : "") + ".md · " + png;
    }

    /// <summary>주인 이름을 생성기 단계로 묶는 키 — ROAD_V2e_tomo_123_4 → RoadsV2:tomo, Walkways → TerraceBlock/Walkways.</summary>
    public static string Group(string owner)
    {
        if (owner.StartsWith("RoadsV2/")) { var n = owner.Substring(8); var parts = n.Split('_'); return "RoadsV2:" + (parts.Length > 2 ? parts[1] + "_" + parts[2] : n); }
        int sl = owner.IndexOf(" · "); if (sl > 0) owner = owner.Substring(0, sl);
        return owner;
    }

    static byte[] MapPng(Result R)
    {
        int S = 2, W = R.nx * S, H = R.nz * S; var px = new Color32[W * H];
        float lo = 0f, hi = Mathf.Max(1f, R.area.max.y);
        for (int iz = 0; iz < R.nz; iz++) for (int ix = 0; ix < R.nx; ix++)
        {
            int i = iz * R.nx + ix; Color32 c;
            if (R.cls[i] == 0 || R.top[i] < SeaY) c = new Color32(38, 58, 92, 255);
            else
            {
                float t = Mathf.InverseLerp(lo, hi, R.top[i]); byte g = (byte)(70 + 150 * t);
                c = R.cls[i] == 1 ? new Color32((byte)(g * .55f), g, (byte)(g * .45f), 255) : R.cls[i] == 2 ? new Color32((byte)(g * .8f), (byte)(g * .8f), (byte)(g * .8f), 255) : new Color32(g, (byte)(g * .8f), (byte)(g * .6f), 255);
            }
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) px[(iz * S + y) * W + ix * S + x] = c;
        }
        var col = new Dictionary<string, Color32>
        {
            { "틈", new Color32(255, 0, 0, 255) }, { "가는 틈", new Color32(255, 40, 40, 255) }, { "뜬 길", new Color32(255, 140, 0, 255) }, { "묻힌 길", new Color32(255, 235, 0, 255) },
            { "길 위 지형", new Color32(255, 235, 0, 255) }, { "길 굴곡", new Color32(255, 0, 255, 255) }, { "길 옆기울기", new Color32(150, 60, 255, 255) }, { "흰 조각", new Color32(255, 255, 255, 255) }, { "봇 멈춤", new Color32(0, 255, 255, 255) },
        };
        foreach (var k in Kinds.Reverse())
            foreach (var m in R.marks)
            {
                if (m.kind != k) continue;
                int cx = Mathf.RoundToInt((m.p.x - R.area.min.x) / R.cell * S), cz = Mathf.RoundToInt((m.p.z - R.area.min.z) / R.cell * S);
                int rad = k == "흰 조각" || k == "봇 멈춤" ? 3 : 1;
                for (int dz = -rad; dz <= rad; dz++) for (int dx = -rad; dx <= rad; dx++)
                {
                    int x = cx + dx, y = cz + dz; if (x < 0 || y < 0 || x >= W || y >= H) continue;
                    bool rim = Mathf.Abs(dx) == rad || Mathf.Abs(dz) == rad;
                    px[y * W + x] = (k == "흰 조각" && !rim) ? new Color32(0, 0, 0, 255) : col[k];
                }
            }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply();
        var bytes = tex.EncodeToPNG(); UnityEngine.Object.DestroyImmediate(tex); return bytes;
    }
}
#endif
