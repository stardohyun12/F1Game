using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 과제 2300(D-216 길 그리기 2판): 길 단면 슬롯 표(docs/map/road-sections.json) → 길가 소품. AI·랜덤 생성 아님 — 표의 규칙 + 고정 seed(같은 입력 = 같은 결과).
//  입력 = 빌더 결과 길(RoadsV2 차길 사슬 · 걷는 길 원본 노선 · 갈림목 삼각형). 연석·보도·축대는 TerraceBlock 결과를 땅으로 읽고 그 위에 얹는다(새로 안 만듦).
//  결과 = 씬 루트 Roadside/<길 이름>. 묶음마다 입력 해시(_in=…) — 다시 깔 때 해시가 다르거나 검사에 걸린 길만 지우고 다시.
public static class RoadsideBuild
{
    // ── 표(JsonUtility) ───────────────────────────────────────
    [System.Serializable] public class RsZone { public string id; public float mult = 1, flower = 1; }
    [System.Serializable] public class RsAvoid { public float junction, walkMouth, door, between, roadClear, maxGap, maxTilt, reach, dropH, sightMain, sightVillage, sightAlley, tallH, search; }
    [System.Serializable] public class RsPrefab { public string id, path, kind; public float footW, footD, h; }
    [System.Serializable] public class RsSlot { public string name, by; public float w; }
    [System.Serializable] public class RsRule { public string id, at, sides; public string[] prefabs; public float spacing, jitter, prob, approach, mountH; public bool decor, flower; }
    [System.Serializable] public class RsSection { public string id, label; public string[] kinds; public float minW, maxW; public bool walk; public RsSlot[] slots; public RsRule[] rules; }
    [System.Serializable] public class RsFile { public int version, seed; public float[] centers; public float radius, ridgeRadius; public string[] ridgeCore; public RsZone[] zones; public RsAvoid avoid; public RsPrefab[] prefabs; public RsSection[] sections; }

    public static string TablePath => System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/map/road-sections.json");
    const string RootName = "Roadside", TownScene = "Assets/Scenes/Map_TownV6.unity";
    const float Cell = .2f, Step = .1f;

    [MenuItem("PitCrew/길가 소품 다시 깔기")] static void MenuAll() { var r = Run(false); Debug.Log("[Roadside] " + r); }

    // ── 문맥(한 번 실행 동안) ─────────────────────────────────
    enum K { None, Ground, Walk, Road, Wall, Building, Other }
    class Road { public string group, kind, section; public float w; public bool walk; public List<Vector2> c = new List<Vector2>(); public List<float> s = new List<float>(); public float L; public string[] tokens; public bool off; }
    class Junc { public Vector2 c; public List<Vector2> tri = new List<Vector2>(); public Vector2 mn, mx; public List<(Vector2 d, float L)> arms = new List<(Vector2, float)>(); }
    class Placed { public Vector2 c, ax, ay; public float hx, hy; }   // 2D OBB(겹침 검사)

    static RsFile T; static Dictionary<string, RsPrefab> P; static string tableHash;
    static Vector2 g0; static int gw, gh; static bool[] roadG;
    static List<Junc> juncs; static List<Vector2> mouths, doors; static List<Rect> ridge;
    static Dictionary<Collider, K> kcache; static readonly RaycastHit[] buf = new RaycastHit[48];
    static Dictionary<(int, int), List<Placed>> occ;

    // ── 실행 ───────────────────────────────────────────────────
    // onlyChanged = true: 입력 해시가 같고 검사도 통과한 길 묶음은 그대로 둔다(1판 Enter·소품 끄기 단추)
    public static string Run(bool onlyChanged)
    {
        if (Application.isPlaying) return "Play 중엔 안 됨";
        if (EditorSceneManager.GetActiveScene().path != TownScene) return "Map_TownV6 아님";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var raw = System.IO.File.ReadAllText(TablePath, Encoding.UTF8); T = JsonUtility.FromJson<RsFile>(raw); tableHash = Fnv(raw).ToString("x8");
        P = T.prefabs.ToDictionary(p => p.id);
        Physics.SyncTransforms(); kcache = new Dictionary<Collider, K>();
        var top = GameObject.Find("TownV6_Gray").transform;
        var roads = Roads(top); Grid(top); Juncs(top, roads); Marks(top, roads);
        var root = GameObject.Find(RootName); if (root == null) root = new GameObject(RootName);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var have = new Dictionary<string, Transform>(); foreach (Transform t in root.transform) have[t.name] = t;
        var bad = onlyChanged ? Check(root, null).badGroups : new HashSet<string>();
        int kept = 0, redone = 0, removed = 0;
        var want = new HashSet<string>(roads.Select(r => r.group));
        foreach (var kv in have.ToList()) if (!want.Contains(kv.Key) || !onlyChanged) { Object.DestroyImmediate(kv.Value.gameObject); have.Remove(kv.Key); removed++; }
        occ = new Dictionary<(int, int), List<Placed>>();
        var todo = new List<(Road r, string h)>();
        foreach (var r in roads.OrderBy(r => r.group, System.StringComparer.Ordinal))
        {
            string h = InHash(r);
            if (have.TryGetValue(r.group, out var g))
            {
                if (g.Find("_in=" + h) != null && !bad.Contains(r.group)) { kept++; foreach (Transform pr in g) if (!pr.name.StartsWith("_")) Occupy(Obb(pr)); continue; }
                Object.DestroyImmediate(g.gameObject);
            }
            todo.Add((r, h));
        }
        var made = new Dictionary<string, int>();
        foreach (var (r, h) in todo)
        {
            var g = new GameObject(r.group); g.transform.SetParent(root.transform, false);
            var mk = new GameObject("_in=" + h); mk.transform.SetParent(g.transform, false); mk.SetActive(false);
            if (!r.off) Place(r, g.transform, made);
            g.transform.SetSiblingIndex(SortedIndex(root.transform, r.group));
            redone++;
        }
        var chk = Check(root, roads);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        var msg = $"길가 소품 {sw.Elapsed.TotalSeconds:F1}s · 길 {roads.Count}(그대로 {kept} · 다시 {redone} · 지움 {removed}) · 새로 놓음 {made.Values.Sum()} · " + chk.summary;
        System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/roadside_last.json"), chk.json);
        return msg;
    }
    static int SortedIndex(Transform root, string name) { int i = 0; foreach (Transform t in root) { if (string.CompareOrdinal(t.name, name) < 0) i++; } return i; }

    // ── 길 모으기 ─────────────────────────────────────────────
    static List<Road> Roads(Transform top)
    {
        var res = new List<Road>(); var rv = top.Find("RoadsV2");
        var hand = RoadPaintData.Load(); var off = new HashSet<string>(hand?.noProps ?? new List<string>());
        foreach (Transform ch in rv)
        {
            if (!ch.gameObject.activeInHierarchy || !ch.name.StartsWith("ROAD_V2e")) continue;
            var rs = ch.GetComponent<RoadSurface>(); if (rs == null || rs.centerline == null || rs.centerline.Length < 2) continue;
            var r = new Road { group = ch.name, kind = rs.kind, w = rs.width, walk = false };
            r.tokens = ch.name.Substring(9).Split('+').Select(t => t.StartsWith("v2_") ? t.Substring(3) : t).ToArray();
            Fill(r, rs.centerline.Select(q => new Vector2(q.x, q.z)));
            if (InRange(r)) res.Add(r);
        }
        // 걷는 길: 메시엔 중심선이 없어서 원본 노선(town-v6b + 손 길) — 빌더가 실제로 깐 것(ROAD_V2p_<id>_*)만
        var walkMesh = new HashSet<string>(); foreach (Transform ch in rv) if (ch.gameObject.activeInHierarchy && ch.name.StartsWith("ROAD_V2p_")) walkMesh.Add(ch.name);
        var hBy = new Dictionary<string, RoadPaintHand>(); if (hand != null) foreach (var h in hand.roads) if (h.walk) hBy[h.id] = h;
        var del = new HashSet<string>(hand?.deleted ?? new List<string>());
        var srcs = BuildBayLoop.RoadPaintSources().Where(s => s.walk).Select(s => (s.id, s.kind, s.w, pts: s.pts)).ToList();
        foreach (var h in hBy.Values) if (!srcs.Any(s => s.id == h.id)) srcs.Add((h.id, h.kind, h.w, RoadPaintData.Pts(h)));
        var seen = new HashSet<string>();
        foreach (var s in srcs)
        {
            if (del.Contains(s.id) || !seen.Add(s.id) || !walkMesh.Any(n => n.StartsWith("ROAD_V2p_" + s.id + "_"))) continue;
            var pts = hBy.TryGetValue(s.id, out var hh) ? RoadPaintData.Pts(hh) : s.pts;
            var r = new Road { group = "ROAD_V2p_" + s.id, kind = s.kind, w = s.w, walk = true, tokens = new[] { s.id } };
            Fill(r, pts.Select(q => new Vector2(q.x, q.z)));
            if (InRange(r)) res.Add(r);
        }
        // 단면 고르기 + 소품 끄기(원본 id 토큰 → 안 맞으면 겹침 50%)
        var offPts = new List<(string id, List<Vector2> p)>();
        if (off.Count > 0) foreach (var s in BuildBayLoop.RoadPaintSources()) if (off.Contains(s.id)) offPts.Add((s.id, s.pts.Select(q => new Vector2(q.x, q.z)).ToList()));
        if (hand != null) foreach (var h in hand.roads) if (off.Contains(h.id)) offPts.Add((h.id, RoadPaintData.Pts(h).Select(q => new Vector2(q.x, q.z)).ToList()));
        foreach (var r in res)
        {
            var sec = T.sections.FirstOrDefault(x => x.walk == r.walk && x.kinds.Contains(r.kind) && r.w >= x.minW && r.w < x.maxW);
            r.section = sec?.id;
            r.off = off.Count > 0 && (r.tokens.Any(t => off.Any(o => t == o || t.StartsWith(o + "_"))) || offPts.Any(o => Covered(r, o.p)));
        }
        return res.Where(r => r.section != null).ToList();
    }
    static void Fill(Road r, IEnumerable<Vector2> src)
    {
        var a = src.ToList(); r.c.Add(a[0]); r.s.Add(0);
        for (int i = 1; i < a.Count; i++) { float d = (a[i] - r.c[r.c.Count - 1]).magnitude; if (d < .01f) continue; r.c.Add(a[i]); r.s.Add(r.s[r.s.Count - 1] + d); }
        r.L = r.s[r.s.Count - 1];
    }
    static bool InRange(Road r) => r.L > 1f && r.c.Any(InR);
    static bool InR(Vector2 q) { for (int i = 0; i + 1 < T.centers.Length; i += 2) if ((q - new Vector2(T.centers[i], T.centers[i + 1])).magnitude < T.radius) return true; return false; }
    static bool Covered(Road r, List<Vector2> src)
    {
        if (src.Count < 2) return false; int n = 0;
        foreach (var q in r.c) { float d = 1e9f; for (int i = 0; i + 1 < src.Count; i++) d = Mathf.Min(d, DistSeg(q, src[i], src[i + 1])); if (d < r.w * .5f + .5f) n++; }
        return n >= r.c.Count * .5f;
    }

    // 입력 해시: 길 모양(cm)·종류·폭·표·끄기 + 근처 갈림목(dm) — 이게 같으면 결과도 같다
    static string InHash(Road r)
    {
        var sb = new StringBuilder(); sb.Append(tableHash).Append(r.kind).Append(r.w.ToString("F2")).Append(r.off ? "off" : "on");
        foreach (var q in r.c) sb.Append(Mathf.RoundToInt(q.x * 100)).Append(',').Append(Mathf.RoundToInt(q.y * 100)).Append(';');
        foreach (var j in juncs) if (r.c.Any(q => (q - j.c).sqrMagnitude < 25 * 25)) sb.Append('J').Append(Mathf.RoundToInt(j.c.x * 10)).Append(',').Append(Mathf.RoundToInt(j.c.y * 10));
        return Fnv(sb.ToString()).ToString("x8");
    }

    // ── 길 면 격자(0.2 m) — 걷는 길 메시는 콜라이더가 없어서 삼각형으로 직접 ─────
    static void Grid(Transform top)
    {
        float mnx = 1e9f, mnz = 1e9f, mxx = -1e9f, mxz = -1e9f;
        for (int i = 0; i + 1 < T.centers.Length; i += 2) { mnx = Mathf.Min(mnx, T.centers[i]); mxx = Mathf.Max(mxx, T.centers[i]); mnz = Mathf.Min(mnz, T.centers[i + 1]); mxz = Mathf.Max(mxz, T.centers[i + 1]); }
        float pad = T.radius + 20; g0 = new Vector2(mnx - pad, mnz - pad); gw = Mathf.CeilToInt((mxx - mnx + 2 * pad) / Cell); gh = Mathf.CeilToInt((mxz - mnz + 2 * pad) / Cell);
        roadG = new bool[gw * gh];
        var meshes = new List<Transform>(); foreach (Transform ch in top.Find("RoadsV2")) if (ch.gameObject.activeInHierarchy) meshes.Add(ch);
        var dr = top.Find("TerraceBlock/Driveway"); if (dr != null) meshes.Add(dr);
        foreach (var ch in meshes)
        {
            var mf = ch.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var v = mf.sharedMesh.vertices; var tr = mf.sharedMesh.triangles; var w = new Vector2[v.Length];
            for (int k = 0; k < v.Length; k++) { var p = ch.TransformPoint(v[k]); w[k] = new Vector2(p.x, p.z); }
            for (int t = 0; t < tr.Length; t += 3) Raster(w[tr[t]], w[tr[t + 1]], w[tr[t + 2]], roadG, 0f);
        }
    }
    static void Raster(Vector2 a, Vector2 b, Vector2 c, bool[] G, float grow)
    {
        float m = Cell * .71f + grow;
        int i0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - m - g0.x) / Cell)), i1 = Mathf.Min(gw - 1, Mathf.CeilToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) + m - g0.x) / Cell));
        int j0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, Mathf.Min(b.y, c.y)) - m - g0.y) / Cell)), j1 = Mathf.Min(gh - 1, Mathf.CeilToInt((Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + m - g0.y) / Cell));
        for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++)
        {
            var p = g0 + new Vector2((i + .5f) * Cell, (j + .5f) * Cell);
            if (InTri(p, a, b, c) || DistSeg(p, a, b) <= m || DistSeg(p, b, c) <= m || DistSeg(p, c, a) <= m) G[j * gw + i] = true;
        }
    }
    static bool OnRoad(Vector2 p) { int i = Mathf.FloorToInt((p.x - g0.x) / Cell), j = Mathf.FloorToInt((p.y - g0.y) / Cell); return i >= 0 && j >= 0 && i < gw && j < gh && roadG[j * gw + i]; }

    static void Juncs(Transform top, List<Road> roads)
    {
        juncs = new List<Junc>();
        foreach (Transform ch in top.Find("RoadsV2"))
        {
            if (!ch.gameObject.activeInHierarchy || !ch.name.StartsWith("ROAD_V2j")) continue;
            var mf = ch.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var j = new Junc(); var v = mf.sharedMesh.vertices; var tr = mf.sharedMesh.triangles;
            foreach (var t in tr) { var p = ch.TransformPoint(v[t]); j.tri.Add(new Vector2(p.x, p.z)); }
            if (j.tri.Count < 3) continue;
            j.mn = new Vector2(j.tri.Min(q => q.x), j.tri.Min(q => q.y)); j.mx = new Vector2(j.tri.Max(q => q.x), j.tri.Max(q => q.y)); j.c = (j.mn + j.mx) * .5f;
            if (!InR(j.c) && !roads.Any(r => r.c.Any(q => (q - j.c).sqrMagnitude < 400))) continue;
            juncs.Add(j);
        }
        // 갈래(시야 삼각형용): 차길 끝이 갈림목 1.5 m 안이면 그 끝에서 길 안쪽으로 3 m 방향
        foreach (var r in roads) if (!r.walk)
            foreach (bool end in new[] { false, true })
            {
                var e = end ? r.c[r.c.Count - 1] : r.c[0]; var j = juncs.FirstOrDefault(x => JDist(x, e) < 1.5f); if (j == null) continue;
                var q = At(r, end ? r.L - 3 : 3, out _); var d = (q - j.c); if (d.sqrMagnitude < 1e-4f) continue;
                var sec = T.sections.First(x => x.id == r.section);
                j.arms.Add((d.normalized, sec.id == "main" ? T.avoid.sightMain : sec.id == "village" ? T.avoid.sightVillage : T.avoid.sightAlley));
            }
        foreach (var j in juncs) j.arms.Sort((a, b) => Mathf.Atan2(a.d.y, a.d.x).CompareTo(Mathf.Atan2(b.d.y, b.d.x)));
    }
    static float JDist(Junc j, Vector2 p)
    {
        if (p.x < j.mn.x - 30 || p.x > j.mx.x + 30 || p.y < j.mn.y - 30 || p.y > j.mx.y + 30) return 1e9f;
        float d = 1e9f; for (int t = 0; t + 2 < j.tri.Count; t += 3) { var a = j.tri[t]; var b = j.tri[t + 1]; var c = j.tri[t + 2]; if (InTri(p, a, b, c)) return 0; d = Mathf.Min(d, Mathf.Min(DistSeg(p, a, b), Mathf.Min(DistSeg(p, b, c), DistSeg(p, c, a)))); }
        return d;
    }
    static bool InSight(Vector2 p)
    {
        foreach (var j in juncs)
        {
            int n = j.arms.Count; if (n < 2) continue;
            for (int k = 0; k < n; k++)
            {
                var a = j.arms[k]; var b = j.arms[(k + 1) % n];
                float ang = Vector2.SignedAngle(a.d, b.d); if (ang < 0) ang += 360; if (ang > 175) continue;   // 맞은편끼리(직선)는 삼각형 아님
                if (InTri(p, j.c, j.c + a.d * a.L, j.c + b.d * b.L)) return true;
            }
        }
        return false;
    }
    static void Marks(Transform top, List<Road> roads)
    {
        mouths = new List<Vector2>(); foreach (var r in roads) if (r.walk) { mouths.Add(r.c[0]); mouths.Add(r.c[r.c.Count - 1]); }
        doors = new List<Vector2>(); ridge = new List<Rect>();
        var d198 = top.Find("D198_Buildings");
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
        {
            var n = t.name.ToLowerInvariant(); if (!(n.Contains("door") || n.Contains("gate")) || n.StartsWith("pobox") || n.StartsWith("studio_")) continue;
            var b = t.root; float baseY = d198 != null && t.IsChildOf(d198) ? BuildingOf(t, d198).position.y : t.position.y;
            if (t.position.y - baseY < 1.2f) doors.Add(new Vector2(t.position.x, t.position.z));
        }
        if (d198 != null) foreach (var id in T.ridgeCore)
        {
            var b = d198.Find(id); if (b == null) continue; var rs = b.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var bb = rs[0].bounds; foreach (var x in rs) bb.Encapsulate(x.bounds);
            ridge.Add(Rect.MinMaxRect(bb.min.x - T.ridgeRadius, bb.min.z - T.ridgeRadius, bb.max.x + T.ridgeRadius, bb.max.z + T.ridgeRadius));
        }
    }
    static Transform BuildingOf(Transform t, Transform d198) { while (t.parent != null && t.parent != d198) t = t.parent; return t; }
    static RsZone Zone(Road r, Vector2 p)
    {
        string z = ridge.Any(x => x.Contains(p)) || r.group.Contains("ridge") ? "ridge" : r.group.Contains("tomo") ? "tomo" : r.group.Contains("camo") ? "camo" : r.group.Contains("posi") ? "posi" : "outer";
        if (z == "outer" && (r.group.Contains("hand_") || r.walk)) z = "camo";   // 손 길·걷는 길은 이름에 구역이 없음 — 보통 밀도
        return T.zones.First(x => x.id == z);
    }

    // ── 땅 읽기: 위에서 아래로, Roadside 는 건너뛰고 맨 위 것 ─────────
    static K Probe(Vector2 p, out float y)
    {
        y = float.NaN; if (OnRoad(p)) { y = 0; return K.Road; }
        int n = Physics.RaycastNonAlloc(new Ray(new Vector3(p.x, 400, p.y), Vector3.down), buf, 800, ~0, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity; Collider bc = null;
        for (int i = 0; i < n; i++) { var h = buf[i]; if (h.point.y <= best || h.collider.transform.root.name == RootName) continue; best = h.point.y; bc = h.collider; }
        if (bc == null) return K.None; y = best; return Kind(bc);
    }
    static K Kind(Collider c)
    {
        if (kcache.TryGetValue(c, out var k)) return k;
        k = K.Other;
        for (var t = c.transform; t != null; t = t.parent)
        {
            var n = t.name;
            if (n == "RoadsV2" || n == "Driveway") { k = K.Road; break; }
            if (n == "Walkways") { k = K.Walk; break; }
            if (n == "RetainingWalls") { k = K.Wall; break; }
            if (n == "TownV6_Terrain" || n.Contains("Terrain")) { k = K.Ground; break; }
            if (n == "D198_Buildings" || n == "Buildings" || n == "FieldPatch") { k = K.Building; break; }
        }
        kcache[c] = k; return k;
    }
    static bool Floor(K k) => k == K.Ground || k == K.Walk;

    // 단면 재기: 길 끝에서 바깥으로 0.1 m 씩 — 길이 끝나는 곳(연석 시작), TerraceBlock 보도 끝, 벽(축대·건물) 앞, 낙차
    struct Prof { public float off, walkEnd, wall, roadY; public bool drop, walk; }
    static Prof Profile(Vector2 E, Vector2 N, float reach)
    {
        var pr = new Prof { off = -1, walkEnd = -1, wall = 99 };
        Probe(E - N * .3f, out _); pr.roadY = RoadY(E - N * .3f);
        float lastY = float.NaN;
        for (float t = 0; t <= reach + 1e-3f; t += Step)
        {
            var k = Probe(E + N * t, out float y);
            if (pr.off < 0) { if (k == K.Road) continue; pr.off = t; if (!Floor(k)) { pr.wall = t; break; } }
            if (k == K.Walk) { pr.walk = true; pr.walkEnd = t; }
            if (k == K.Wall || k == K.Building || k == K.Other || (k == K.Ground && !float.IsNaN(lastY) && y - lastY > .3f)) { pr.wall = t; break; }
            if (k == K.Road || k == K.None) { pr.wall = t; break; }
            lastY = y;
        }
        if (pr.off < 0) pr.off = 99;
        if (!float.IsNaN(pr.roadY) && pr.wall > pr.off + 2.6f) { var k = Probe(E + N * (pr.off + 2.5f), out float y2); pr.drop = Floor(k) && pr.roadY - y2 > T.avoid.dropH; }
        return pr;
    }
    static float RoadY(Vector2 p)
    {
        int n = Physics.RaycastNonAlloc(new Ray(new Vector3(p.x, 400, p.y), Vector3.down), buf, 800, ~0, QueryTriggerInteraction.Ignore);
        float best = float.NaN; for (int i = 0; i < n; i++) if (Kind(buf[i].collider) == K.Road && (float.IsNaN(best) || buf[i].point.y > best)) best = buf[i].point.y;
        if (float.IsNaN(best)) { int i = 0; for (; i < n; i++) if (Kind(buf[i].collider) == K.Ground) { best = buf[i].point.y; break; } }
        return best;
    }

    // ── 놓기 ───────────────────────────────────────────────────
    static void Place(Road r, Transform g, Dictionary<string, int> made)
    {
        var sec = T.sections.First(x => x.id == r.section);
        float curbW = SlotW(sec, "연석"), walkW = SlotW(sec, "보도"), rowW = SlotW(sec, "소품 줄");
        int n = 0;
        foreach (var rule in sec.rules)
        {
            var sides = rule.sides == "both" || rule.sides == "stagger" || rule.sides == "ends" ? new[] { 1, -1 } : rule.sides == "left" ? new[] { 1 } : rule.sides == "right" ? new[] { -1 } : new int[0];
            if (rule.sides.StartsWith("approach"))
            {
                // 갈림목으로 들어가는 쪽 오른편(표지판) / 왼편(볼록거울) — 끝에서 approach m 앞
                foreach (bool end in new[] { false, true })
                {
                    var e = end ? r.c[r.c.Count - 1] : r.c[0]; if (!juncs.Any(j => JDist(j, e) < 1.5f) || r.L < rule.approach * 2 + 2) continue;
                    var rng = new Rng(Seed(r.group, rule.id, end ? 2 : 3)); if (rng.F() > rule.prob) continue;
                    int side = (end ? -1 : 1) * (rule.sides == "approachRight" ? 1 : -1);
                    float s = end ? r.L - rule.approach : rule.approach;
                    if (TryAt(r, g, sec, rule, side, s, curbW, walkW, rowW, rng, true, ref n)) Count(made, rule.id);
                }
                continue;
            }
            foreach (int side in sides)
            {
                var rng = new Rng(Seed(r.group, rule.id, side));
                if (rule.sides == "ends")
                {
                    foreach (float s in new[] { rule.approach, r.L - rule.approach }) { if (r.L < rule.approach * 2 + 1 || rng.F() > rule.prob) continue; if (TryAt(r, g, sec, rule, side, s, curbW, walkW, rowW, rng, true, ref n)) Count(made, rule.id); }
                    continue;
                }
                float sp0 = Mathf.Max(.5f, rule.spacing); float s0 = rng.F() * sp0; if (rule.sides == "stagger" && side < 0) s0 += sp0 * .5f;
                for (float s = s0; s < r.L; )
                {
                    var p = At(r, s, out _); var z = Zone(r, p);
                    float mult = rule.decor ? z.mult * (rule.flower ? z.flower : 1f) : 1f;
                    float step = sp0 / Mathf.Max(.05f, mult) * (1 + rule.jitter * (rng.F() * 2 - 1));
                    bool go = rng.F() <= rule.prob && InR(p);
                    if (go && TryAt(r, g, sec, rule, side, s, curbW, walkW, rowW, rng, rule.at != "drop", ref n)) Count(made, rule.id);
                    s += Mathf.Max(.5f, step);
                }
            }
        }
    }
    static void Count(Dictionary<string, int> m, string k) => m[k] = (m.TryGetValue(k, out var v) ? v : 0) + 1;
    static float SlotW(RsSection sec, string name) { var s = sec.slots.FirstOrDefault(x => x.name == name); return s == null ? 0 : Mathf.Max(0, s.w); }

    static bool TryAt(Road r, Transform g, RsSection sec, RsRule rule, int side, float s, float curbW, float walkW, float rowW, Rng rng, bool search, ref int n)
    {
        var pf = P[rule.prefabs[Mathf.Min(rule.prefabs.Length - 1, (int)(rng.F() * rule.prefabs.Length))]];
        var A = T.avoid; float sr = search ? A.search : 0;
        for (int tryI = 0; tryI <= Mathf.RoundToInt(sr / .5f) * 2; tryI++)
        {
            float ds = tryI == 0 ? 0 : ((tryI + 1) / 2) * .5f * (tryI % 2 == 1 ? 1 : -1);
            float ss = s + ds; if (ss < .5f || ss > r.L - .5f) continue;
            var P0 = At(r, ss, out var Tg); var N = new Vector2(-Tg.y, Tg.x) * side;
            if (!InR(P0)) continue;
            var E = P0 + N * (r.w * .5f);
            var pr = Profile(E, N, A.reach); if (pr.off > 3f) continue;
            float d; bool attached = false; Vector3 pos; Quaternion rot; Vector2 C;
            float yawDeg = Mathf.Atan2(Tg.x, Tg.y) * Mathf.Rad2Deg;   // 프리팹 +x = 길 방향, +z = 길 쪽(가로등 팔·간판 앞)
            var face = Quaternion.Euler(0, Mathf.Atan2(-N.x, -N.y) * Mathf.Rad2Deg, 0);
            if (rule.at == "wall")
            {
                if (pr.wall > A.reach || pr.wall < pr.off) continue;
                if (!Mount(E, N, pr, rule.mountH, pf, out pos)) continue;
                rot = face; C = new Vector2(pos.x, pos.z);
                if (Avoided(C, pf, rule, false) || Overlaps(Obb(C, face, pf), true)) continue;
                Spawn(g, pf, rule, pos, rot, ref n); return true;
            }
            if (rule.at == "curb") d = pr.off + curbW + pf.footD * .5f + .15f;
            else if (rule.at == "drop") { if (!pr.drop || pr.walk) continue; d = pr.off + .1f + pf.footD * .5f; }
            else
            {
                float back = pr.walk ? pr.walkEnd + Step : pr.off + curbW + walkW;   // 보도(TerraceBlock) 끝 또는 표의 보도 폭만큼 비움
                d = back + pf.footD * .5f + .05f;
                if (d + pf.footD * .5f > pr.wall)
                {
                    // 벽이 먼저 나옴 → 벽에 붙이되 사람 지날 0.9 m 는 남긴다
                    float wd = pr.wall - pf.footD * .5f - .03f;
                    if (wd - pf.footD * .5f - pr.off < curbW + .9f) continue;
                    d = wd; attached = true;
                }
                if (d - pf.footD * .5f - pr.off > rowW + walkW + curbW + .6f && !pr.walk) continue;   // 슬롯보다 너무 멀면(땅이 들쭉날쭉) 안 놓음
            }
            if (d + pf.footD * .5f > pr.wall && rule.at != "row") continue;
            C = E + N * d;
            if (attached) { var hp = new Vector3(C.x, 0, C.y); if (WallFace(E, N, pr, out float tw)) C = E + N * (tw - pf.footD * .5f - .02f); }
            var yaw = rule.at == "drop" || pf.kind == "box" ? Quaternion.Euler(0, yawDeg + 90, 0) * Quaternion.Euler(0, -90, 0) : face;
            if (pf.kind == "box") yaw = Quaternion.LookRotation(new Vector3(-N.x, 0, -N.y), Vector3.up);   // 앞(+z) = 길 쪽, 긴 변(x) = 길 방향
            if (!Ground(C, yaw, pf, attached, out pos, out rot)) continue;
            if (Avoided(C, pf, rule, rule.sides.StartsWith("approach")) || Overlaps(Obb(C, yaw, pf), true)) continue;
            Spawn(g, pf, rule, pos, rot, ref n); return true;
        }
        return false;
    }
    static bool WallFace(Vector2 E, Vector2 N, Prof pr, out float t)
    {
        t = pr.wall; var o = E + N * (pr.wall - .4f); float y = RoadY(o); if (float.IsNaN(y)) return false;
        Probe(o, out float gy); if (!float.IsNaN(gy)) y = gy;
        var org = new Vector3(o.x, y + .35f, o.y); var dir = new Vector3(N.x, 0, N.y);
        int n = Physics.RaycastNonAlloc(new Ray(org, dir), buf, 1.2f, ~0, QueryTriggerInteraction.Ignore); float best = 9;
        for (int i = 0; i < n; i++) { var k = Kind(buf[i].collider); if (buf[i].collider.transform.root.name == RootName || !(k == K.Wall || k == K.Building || k == K.Other)) continue; best = Mathf.Min(best, buf[i].distance); }
        if (best > 1.2f) return false; t = pr.wall - .4f + best; return true;
    }
    // 벽에 붙이기(벽등·작은 간판): 벽 앞 땅 높이 + mountH, 벽면까지 수평 레이로 정확히
    static bool Mount(Vector2 E, Vector2 N, Prof pr, float mountH, RsPrefab pf, out Vector3 pos)
    {
        pos = default; var foot = E + N * Mathf.Max(pr.off, pr.wall - .3f); var k = Probe(foot, out float fy); if (!Floor(k)) return false;
        var org = new Vector3(foot.x, fy + mountH, foot.y); var dir = new Vector3(N.x, 0, N.y);
        int n = Physics.RaycastNonAlloc(new Ray(org, dir), buf, 1.5f, ~0, QueryTriggerInteraction.Ignore); float best = 9; Vector3 hp = default;
        for (int i = 0; i < n; i++) { var kk = Kind(buf[i].collider); if (buf[i].collider.transform.root.name == RootName || !(kk == K.Wall || kk == K.Building)) continue; if (buf[i].distance < best) { best = buf[i].distance; hp = buf[i].point; } }
        if (best > 1.5f) return false;
        // 벽이 mountH+0.3 보다 높아야(낮은 축대 머리에 매달린 등 X)
        int n2 = Physics.RaycastNonAlloc(new Ray(hp + Vector3.up * .3f - dir * .05f, dir), buf, .3f, ~0, QueryTriggerInteraction.Ignore); bool tall = false;
        for (int i = 0; i < n2; i++) { var kk = Kind(buf[i].collider); if (kk == K.Wall || kk == K.Building) tall = true; }
        if (!tall) return false;
        float back = pf.kind == "panel" ? 0f : pf.footD * .5f;   // 간판은 뒷면이 원점(z=0), 벽등은 가운데
        pos = hp - dir * (back + .01f); return true;
    }
    // 땅에 앉히기: 기둥 = 곧게(모서리 높이차 절반 ≤ maxGap), 상자 = 네 모서리 평면에 맞춰 기울임(≤ maxTilt, 남는 틈 ≤ maxGap)
    static bool Ground(Vector2 C, Quaternion yaw, RsPrefab pf, bool attached, out Vector3 pos, out Quaternion rot)
    {
        pos = default; rot = yaw; var A = T.avoid;
        var ax = yaw * Vector3.right; var az = yaw * Vector3.forward; float hx = pf.footW * .5f, hz = pf.footD * .5f;
        var pts = new List<Vector3>(); float rc = A.roadClear;
        foreach (var (u, v) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 0f), (0f, -1f), (0f, 1f), (-1f, 0f), (1f, 0f) })
        {
            var q3 = new Vector3(C.x, 0, C.y) + ax * (u * hx) + az * (v * hz); var q = new Vector2(q3.x, q3.z);
            // 길 면 여유: 모서리에서 roadClear 만큼 바깥도 길 아니어야
            var qq = new Vector2((ax * (u * (hx + rc)) + az * (v * (hz + rc))).x, (ax * (u * (hx + rc)) + az * (v * (hz + rc))).z) + C;
            if (OnRoad(qq)) return false;
            var k = Probe(q, out float y); if (!Floor(k)) return false;
            pts.Add(new Vector3(q.x, y, q.y));
        }
        if (pf.kind == "pole")
        {
            float mn = pts.Min(p => p.y), mx = pts.Max(p => p.y); if (mx - mn > 2 * A.maxGap) return false;
            pos = new Vector3(C.x, (mn + mx) * .5f, C.y); rot = yaw; return true;
        }
        // 최소제곱 평면 y = a + b·x + c·z (중심 기준)
        double sxx = 0, sxz = 0, szz = 0, sx = 0, sz = 0, sy = 0, sxy = 0, szy = 0; int n = pts.Count;
        foreach (var p in pts) { double x = p.x - C.x, z = p.z - C.y; sx += x; sz += z; sy += p.y; sxx += x * x; szz += z * z; sxz += x * z; sxy += x * p.y; szy += z * p.y; }
        var M = new double[3, 3] { { n, sx, sz }, { sx, sxx, sxz }, { sz, sxz, szz } }; var R = new[] { sy, sxy, szy };
        if (!Solve3(M, R, out var abc)) return false;
        float a0 = (float)abc[0], b = (float)abc[1], c = (float)abc[2];
        foreach (var p in pts) if (Mathf.Abs(p.y - (a0 + b * (p.x - C.x) + c * (p.z - C.y))) > A.maxGap) return false;
        var nrm = new Vector3(-b, 1, -c).normalized; if (Vector3.Angle(nrm, Vector3.up) > A.maxTilt) return false;
        pos = new Vector3(C.x, a0, C.y); rot = Quaternion.FromToRotation(Vector3.up, nrm) * yaw; return true;
    }
    static bool Avoided(Vector2 C, RsPrefab pf, RsRule rule, bool approach)
    {
        var A = T.avoid; float rad = Mathf.Max(pf.footW, pf.footD) * .5f;
        if (!approach) foreach (var j in juncs) if (JDist(j, C) < A.junction + rad) return true;
        foreach (var m in mouths) if ((m - C).magnitude < A.walkMouth + rad && rule.sides != "ends") return true;
        foreach (var d in doors) if ((d - C).magnitude < A.door + rad) return true;
        if (pf.h > A.tallH && pf.kind != "mount" && pf.kind != "panel" && InSight(C)) return true;
        return false;
    }

    // ── 겹침(2D OBB, 2 m 칸 해시) ─────────────────────────────
    static Placed Obb(Vector2 C, Quaternion yaw, RsPrefab pf)
    {
        var ax = yaw * Vector3.right; var az = yaw * Vector3.forward; float m = T.avoid.between * .5f;
        return new Placed { c = C, ax = new Vector2(ax.x, ax.z).normalized, ay = new Vector2(az.x, az.z).normalized, hx = pf.footW * .5f + m, hy = pf.footD * .5f + m };
    }
    static Placed Obb(Transform pr) { var pf = PrefabOf(pr.name); return pf == null ? null : Obb(new Vector2(pr.position.x, pr.position.z), Quaternion.Euler(0, pr.eulerAngles.y, 0), pf); }
    static RsPrefab PrefabOf(string name) { int i = name.IndexOf('@'); return i > 0 && P.TryGetValue(name.Substring(0, i), out var p) ? p : null; }
    static bool Overlaps(Placed o, bool _)
    {
        int ci = Mathf.FloorToInt(o.c.x / 2), cj = Mathf.FloorToInt(o.c.y / 2);
        for (int j = cj - 2; j <= cj + 2; j++) for (int i = ci - 2; i <= ci + 2; i++)
            if (occ.TryGetValue((i, j), out var l)) foreach (var p in l) if (Sat(o, p)) return true;
        return false;
    }
    static void Occupy(Placed o) { if (o == null) return; var k = (Mathf.FloorToInt(o.c.x / 2), Mathf.FloorToInt(o.c.y / 2)); if (!occ.TryGetValue(k, out var l)) occ[k] = l = new List<Placed>(); l.Add(o); }
    static bool Sat(Placed a, Placed b)
    {
        var d = b.c - a.c;
        foreach (var ax in new[] { a.ax, a.ay, b.ax, b.ay })
        {
            float ra = a.hx * Mathf.Abs(Vector2.Dot(a.ax, ax)) + a.hy * Mathf.Abs(Vector2.Dot(a.ay, ax));
            float rb = b.hx * Mathf.Abs(Vector2.Dot(b.ax, ax)) + b.hy * Mathf.Abs(Vector2.Dot(b.ay, ax));
            if (Mathf.Abs(Vector2.Dot(d, ax)) > ra + rb) return false;
        }
        return true;
    }
    static void Spawn(Transform g, RsPrefab pf, RsRule rule, Vector3 pos, Quaternion rot, ref int n)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(pf.path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, g.gameObject.scene); go.transform.SetParent(g, false);
        go.transform.SetPositionAndRotation(pos, rot); go.name = $"{pf.id}@{rule.id}#{n++:000}";
        Occupy(Obb(new Vector2(pos.x, pos.z), Quaternion.Euler(0, rot.eulerAngles.y, 0), pf));
        Physics.SyncTransforms();
    }

    // ── 검사: 뜸·묻힘·길 침범·겹침 + 종류별 개수·100 m 당 + 결과 해시 ────────
    public class Report { public string summary, json; public HashSet<string> badGroups = new HashSet<string>(); }
    static Report Check(GameObject root, List<Road> roads)
    {
        var rep = new Report(); var A = T.avoid; float worstGap = 0; int nGap = 0, nRoad = 0, nOver = 0, nWall = 0, total = 0;
        var all = new List<(string grp, Transform t, RsPrefab pf, Placed o)>();
        foreach (Transform g in root.transform) foreach (Transform t in g) { if (t.name.StartsWith("_")) continue; var pf = PrefabOf(t.name); if (pf == null) continue; all.Add((g.name, t, pf, Obb(t))); }
        var byKind = new SortedDictionary<string, int>(System.StringComparer.Ordinal); var bySec = new Dictionary<string, SortedDictionary<string, int>>();
        var secOf = roads?.ToDictionary(r => r.group, r => r.section) ?? new Dictionary<string, string>();
        var hashSb = new StringBuilder();
        foreach (var (grp, t, pf, o) in all)
        {
            total++; byKind[pf.id] = (byKind.TryGetValue(pf.id, out var v) ? v : 0) + 1;
            if (secOf.TryGetValue(grp, out var sid)) { if (!bySec.TryGetValue(sid, out var d)) bySec[sid] = d = new SortedDictionary<string, int>(System.StringComparer.Ordinal); d[pf.id] = (d.TryGetValue(pf.id, out var v2) ? v2 : 0) + 1; }
            var p = t.position; var e = t.eulerAngles;
            hashSb.Append(grp).Append('/').Append(t.name).Append(Mathf.RoundToInt(p.x * 1000)).Append(',').Append(Mathf.RoundToInt(p.y * 1000)).Append(',').Append(Mathf.RoundToInt(p.z * 1000)).Append(',').Append(Mathf.RoundToInt(e.x * 100)).Append(',').Append(Mathf.RoundToInt(e.y * 100)).Append(',').Append(Mathf.RoundToInt(e.z * 100)).Append(';');
            bool bad = false;
            if (pf.kind == "mount" || pf.kind == "panel")
            {
                // 벽까지 틈: 앞(+z 반대 = 뒤)으로 수평 레이
                var back = -(t.rotation * Vector3.forward); float off = pf.kind == "panel" ? 0 : pf.footD * .5f;
                int n = Physics.RaycastNonAlloc(new Ray(p - back * .05f, back), buf, 1f, ~0, QueryTriggerInteraction.Ignore); float best = 9;
                for (int i = 0; i < n; i++) { if (buf[i].collider.transform.root.name == RootName) continue; var k = Kind(buf[i].collider); if (k == K.Wall || k == K.Building) best = Mathf.Min(best, buf[i].distance - .05f - off); }
                if (best > A.maxGap + .01f) { nWall++; bad = true; }
            }
            else
            {
                // 밑면 9점: 그 자리 땅과 밑면 평면의 차
                var up = t.rotation * Vector3.up; var ax = t.rotation * Vector3.right; var az = t.rotation * Vector3.forward; float g = 0;
                foreach (var (u, w) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 0f), (0f, -1f), (0f, 1f), (-1f, 0f), (1f, 0f) })
                {
                    var q = p + ax * (u * pf.footW * .5f) + az * (w * pf.footD * .5f); var q2 = new Vector2(q.x, q.z);
                    if (OnRoad(q2)) { nRoad++; bad = true; break; }
                    var k = Probe(q2, out float y); if (float.IsNaN(y)) continue;
                    g = Mathf.Max(g, Mathf.Abs(y - q.y));
                }
                worstGap = Mathf.Max(worstGap, g); if (g > A.maxGap + .002f) { nGap++; bad = true; }
            }
            if (bad) rep.badGroups.Add(grp);
        }
        for (int i = 0; i < all.Count; i++) for (int j = i + 1; j < all.Count; j++)
        {
            var a = all[i].o; var b = all[j].o; if (a == null || b == null || (a.c - b.c).sqrMagnitude > 25) continue;
            var a2 = new Placed { c = a.c, ax = a.ax, ay = a.ay, hx = a.hx - A.between * .5f, hy = a.hy - A.between * .5f }; var b2 = new Placed { c = b.c, ax = b.ax, ay = b.ay, hx = b.hx - A.between * .5f, hy = b.hy - A.between * .5f };
            if (Sat(a2, b2)) { nOver++; rep.badGroups.Add(all[i].grp); rep.badGroups.Add(all[j].grp); }
        }
        // 100 m 당: 단면별 범위 안 중심선 길이(양쪽 합쳐 한 번)
        var lenSec = new Dictionary<string, float>();
        if (roads != null) foreach (var r in roads) { float l = 0; for (int i = 1; i < r.c.Count; i++) if (InR(r.c[i])) l += (r.c[i] - r.c[i - 1]).magnitude; lenSec[r.section] = (lenSec.TryGetValue(r.section, out var x) ? x : 0) + l; }
        string hash = Fnv(hashSb.ToString()).ToString("x8");
        var js = new StringBuilder("{");
        js.Append($"\"total\":{total},\"hash\":\"{hash}\",\"worstGapM\":{worstGap:F4},\"gapOver\":{nGap},\"roadHits\":{nRoad},\"overlapPairs\":{nOver},\"wallGapOver\":{nWall},");
        js.Append("\"byKind\":{" + string.Join(",", byKind.Select(k => $"\"{k.Key}\":{k.Value}")) + "},");
        js.Append("\"bySection\":{" + string.Join(",", bySec.Select(s => $"\"{s.Key}\":{{\"lenM\":{(lenSec.TryGetValue(s.Key, out var L) ? L : 0):F1}," + string.Join(",", s.Value.Select(k => $"\"{k.Key}\":{k.Value}")) + "}")) + "},");
        js.Append("\"groups\":{" + string.Join(",", root.transform.Cast<Transform>().Select(g => $"\"{g.name}\":\"{GroupHash(g)}\"")) + "}}");
        rep.json = js.ToString();
        rep.summary = $"소품 {total} · 해시 {hash} · 뜸/묻힘 최대 {worstGap * 100:F1} cm(초과 {nGap}) · 길 침범 {nRoad} · 겹침 {nOver} · 벽 틈 초과 {nWall}";
        return rep;
    }
    static string GroupHash(Transform g)
    {
        var sb = new StringBuilder(); foreach (Transform t in g) { var p = t.position; sb.Append(t.name).Append(Mathf.RoundToInt(p.x * 1000)).Append(',').Append(Mathf.RoundToInt(p.y * 1000)).Append(',').Append(Mathf.RoundToInt(p.z * 1000)).Append(',').Append(Mathf.RoundToInt(t.eulerAngles.y * 100)).Append(';'); }
        return Fnv(sb.ToString()).ToString("x8");
    }

    // ── 작은 도구 ─────────────────────────────────────────────
    static Vector2 At(Road r, float s, out Vector2 tg)
    {
        s = Mathf.Clamp(s, 0, r.L); int i = 1; while (i < r.s.Count - 1 && r.s[i] < s) i++;
        var a = r.c[i - 1]; var b = r.c[i]; float t = (s - r.s[i - 1]) / Mathf.Max(1e-4f, r.s[i] - r.s[i - 1]);
        // 접선 = ±1.5 m 앞뒤 점 차(꺾은선 모서리에서 튀지 않게)
        var f = AtRaw(r, Mathf.Min(r.L, s + 1.5f)); var bk = AtRaw(r, Mathf.Max(0, s - 1.5f)); tg = (f - bk).sqrMagnitude > 1e-6f ? (f - bk).normalized : (b - a).normalized;
        return Vector2.Lerp(a, b, t);
    }
    static Vector2 AtRaw(Road r, float s) { int i = 1; while (i < r.s.Count - 1 && r.s[i] < s) i++; float t = (s - r.s[i - 1]) / Mathf.Max(1e-4f, r.s[i] - r.s[i - 1]); return Vector2.Lerp(r.c[i - 1], r.c[i], Mathf.Clamp01(t)); }
    static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y), d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y), d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        return !(((d1 < 0) || (d2 < 0) || (d3 < 0)) && ((d1 > 0) || (d2 > 0) || (d3 > 0)));
    }
    static float DistSeg(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float l = ab.sqrMagnitude; float t = l < 1e-8f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l); return (p - (a + ab * t)).magnitude; }
    static bool Solve3(double[,] M, double[] R, out double[] x)
    {
        x = new double[3]; var a = (double[,])M.Clone(); var b = (double[])R.Clone();
        for (int c = 0; c < 3; c++)
        {
            int piv = c; for (int r = c + 1; r < 3; r++) if (System.Math.Abs(a[r, c]) > System.Math.Abs(a[piv, c])) piv = r;
            if (System.Math.Abs(a[piv, c]) < 1e-9) return false;
            if (piv != c) { for (int k = 0; k < 3; k++) { var tmp = a[c, k]; a[c, k] = a[piv, k]; a[piv, k] = tmp; } var tb = b[c]; b[c] = b[piv]; b[piv] = tb; }
            for (int r = 0; r < 3; r++) { if (r == c) continue; double f = a[r, c] / a[c, c]; for (int k = 0; k < 3; k++) a[r, k] -= f * a[c, k]; b[r] -= f * b[c]; }
        }
        for (int c = 0; c < 3; c++) x[c] = b[c] / a[c, c]; return true;
    }
    static uint Fnv(string s) { uint h = 2166136261; foreach (char ch in s) { h ^= ch; h *= 16777619; } return h; }
    static uint Seed(string group, string rule, int side) => Fnv($"{T.seed}|{group}|{rule}|{side}");
    class Rng { uint x; public Rng(uint s) { x = s == 0 ? 0x9E3779B9u : s; } public float F() { x ^= x << 13; x ^= x >> 17; x ^= x << 5; return (x & 0xFFFFFF) / 16777216f; } }
}
