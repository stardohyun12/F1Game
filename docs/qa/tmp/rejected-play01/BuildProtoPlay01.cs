using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 과제 2329 (D-155, docs/design/PLAY.md): 회색 박스 배달 놀이터 Proto_Play01.
/// Proto_CoastCourse 를 복사해 차·카메라·화물·주문·배달 배선을 그대로 쓰고, 코스(ProtoCourse)만 걷어내고 회색 길망을 새로 놓는다.
/// 길 = 노드(x,y,z) 꺾은선 + 모서리 둥글리기. 경로 8개(배달 4 × 큰길/지름길)를 따로 굽는다 — 겹치는 구간은 같은 자리·같은 색이라 안 보인다.
/// 지형은 길 높이에 맞춰 깎고 메운 단색 비탈. 봇 검증용 경로 중심선 = Routes/ 의 RoadSurface(kind "route").
/// </summary>
public static class BuildProtoPlay01
{
    const string SrcScene = "Assets/Scenes/Proto_CoastCourse.unity";
    public const string ScenePath = "Assets/Scenes/Proto_Play01.unity";
    const string Dir = "Assets/Scenes/Proto_Play01_Assets";

    // 길 종류: 폭·색·높이 덧(다른 색끼리 겹칠 때 깜빡임 방지)
    static readonly Dictionary<string, (float w, Color c, float dy)> Kinds = new()
    {
        { "hw",     (9f,   new Color(.62f, .62f, .62f), 0f) },     // 해안 큰길(고속 직선)
        { "main",   (7f,   new Color(.52f, .52f, .52f), 0f) },     // 큰길
        { "dirt",   (5f,   new Color(.78f, .60f, .32f), .03f) },   // 거친 지름길(황토, 턱·급경사)
        { "cobble", (5f,   new Color(.55f, .38f, .24f), .03f) },   // 돌길 요철(갈색 + 짙은 줄무늬 범프)
        { "alley",  (4.5f, new Color(.70f, .56f, .36f), .03f) },   // 좁은 골목(바닥 황토 계열, 벽 진회색)
    };

    static Transform root;
    static readonly Dictionary<string, Material> mats = new();
    static Material M(string n, Color c)
    {
        if (mats.TryGetValue(n, out var m)) return m;
        var path = Dir + "/PP_" + n + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = c; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        EditorUtility.SetDirty(m); mats[n] = m; return m;
    }

    // ── 노드 ──
    static readonly Dictionary<string, Vector3> N = new()
    {
        { "G", new(0, 2, -210) }, { "GN", new(0, 2, -178) },
        { "HW1", new(-240, 2, -210) }, { "W0", new(-240, 3, -150) },
        { "H1", new(-160, 8.5f, -150) }, { "H2", new(-160, 9.5f, -120) }, { "H3", new(-240, 15, -120) }, { "H4", new(-240, 16, -90) },
        { "H5", new(-160, 21.5f, -90) }, { "H6", new(-160, 22.5f, -60) }, { "H7", new(-240, 28, -60) }, { "H8", new(-240, 29, -30) }, { "H9", new(-160, 33, -30) },
        { "D1", new(-160, 33, 40) },
        { "HE1", new(240, 2, -210) }, { "HE2", new(240, 4, -130) }, { "D2", new(130, 5, -105) },
        { "M3a", new(100, 10, -75) }, { "M3b", new(70, 14, -55) }, { "D3", new(40, 17, -40) },
        { "SW1", new(-100, 18, -70) },
        { "AL1", new(35, 3, -165) }, { "AL2", new(60, 3, -140) }, { "AL3", new(95, 4, -130) },
        { "CB1", new(-20, 6, -130) }, { "CB2", new(10, 11, -90) },
        { "UA1", new(20, 25, 30) }, { "PL1", new(-60, 34, 60) }, { "CR", new(60, 33, 95) }, { "HUMP", new(100, 38, 120) }, { "D4", new(190, 26, 185) },
    };
    // 경로: "노드 종류 노드 종류 노드 ..." (종류 = 다음 노드까지 길 종류)
    const string HairpinChain = "W0 main H1 main H2 main H3 main H4 main H5 main H6 main H7 main H8 main H9 main D1";
    static readonly (string id, string spec)[] Routes =
    {
        ("D1_main",  "G hw HW1 main " + HairpinChain),
        ("D1_short", "G main GN dirt SW1 dirt D1"),
        ("D2_main",  "G hw HE1 main HE2 main D2"),
        ("D2_short", "G main GN alley AL1 alley AL2 alley AL3 alley D2"),
        ("D3_main",  "G hw HE1 main HE2 main D2 main M3a main M3b main D3"),
        ("D3_short", "G main GN cobble CB1 cobble CB2 cobble D3"),
        ("D4_main",  "G hw HW1 main " + HairpinChain + " main PL1 main CR main HUMP main D4"),
        ("D4_short", "G main GN cobble CB1 cobble CB2 cobble D3 alley UA1 alley CR main HUMP main D4"),
    };
    // 배달점: 라벨은 기존 주문 배선(OrderJob.destinationLabels)이 쓰는 이름 그대로 — DeliveryPoint 4개를 옮겨 쓴다
    static readonly (string node, string label, string name, CargoType cargo)[] Deliveries =
    {
        ("D1", "HairpinOrchard", "1 언덕 위 과수원 — 유리병", CargoType.Glass),
        ("D2", "CoastCafe",      "2 바닷가 카페 — 상자",     CargoType.Box),
        ("D3", "AlleyEnd",       "3 비탈 중턱 집 — 계란",     CargoType.Eggs),
        ("D4", "LoneHouse",      "4 언덕 너머 외딴집 — 상자", CargoType.Box),
    };

    public struct PP { public Vector3 p; public string kind; }

    [MenuItem("PitCrew/Map/Build Proto_Play01 (회색 박스 배달 놀이터)")]
    public static void Menu() { Debug.Log("[Play01] " + Build()); }

    public static string Build()
    {
        mats.Clear();
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Scenes", "Proto_Play01_Assets");
        AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SrcScene, ScenePath)) return "copy failed";
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects()) if (g.name == "ProtoCourse") Object.DestroyImmediate(g);
        root = new GameObject("Play01").transform;
        var log = new List<string>();

        // ── 경로 → 1m 점 ──
        var paths = new Dictionary<string, List<PP>>();
        foreach (var (id, spec) in Routes) paths[id] = Path(spec);

        // ── 길 메시(경로마다 같은 종류 연속 구간 하나씩) ──
        var gR = new GameObject("Roads").transform; gR.SetParent(root);
        var skirt = M("Skirt", new Color(.45f, .44f, .42f));
        var built = new HashSet<string>(); int meshes = 0;
        foreach (var (id, _) in Routes)
        {
            var P = paths[id]; int s = 0;
            for (int i = 1; i <= P.Count; i++)
            {
                if (i < P.Count && P[i].kind == P[s].kind) continue;
                var seg = P.GetRange(Mathf.Max(0, s - 1), Mathf.Min(P.Count, i + 1) - Mathf.Max(0, s - 1));
                string key = P[s].kind + Hash(seg);
                if (built.Add(key)) { RoadMesh(gR, id + "_" + P[s].kind + "_" + meshes, seg, P[s].kind, skirt); meshes++; }
                s = i;
            }
        }
        log.Add($"길 메시 {meshes}");

        // ── 돌길 범프·지름길 턱·골목 벽 ──
        log.Add(Features(paths));

        // ── 지형(길에 맞춰 깎고 메운 단색 비탈) + 바다 ──
        Terrain(paths);
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane); sea.name = "Sea"; sea.transform.SetParent(root);
        sea.transform.position = new Vector3(0, .4f, -420); sea.transform.localScale = new Vector3(80, 1, 36);
        Object.DestroyImmediate(sea.GetComponent<Collider>()); sea.GetComponent<Renderer>().sharedMaterial = M("Sea", new Color(.36f, .58f, .72f));

        // ── 봇 경로(RoadSurface kind=route) + 경사 ──
        var gRt = new GameObject("Routes").transform; gRt.SetParent(root);
        var sb = new StringBuilder();
        foreach (var (id, _) in Routes)
        {
            var P = paths[id]; var rs = new GameObject("Route_" + id).AddComponent<RoadSurface>(); rs.transform.SetParent(gRt);
            rs.roadId = id; rs.kind = "route"; rs.width = 4.5f; rs.centerline = P.Select(q => q.p).ToArray();
            float len = 0, maxG = 0; for (int i = 1; i < P.Count; i++) { var d = P[i].p - P[i - 1].p; float h = new Vector2(d.x, d.z).magnitude; len += h; if (h > .3f) maxG = Mathf.Max(maxG, Mathf.Abs(d.y) / h); }
            sb.Append($"{id} {len:F0}m 최대경사{maxG * 100:F1}% · ");
        }
        log.Add(sb.ToString());

        // ── 차고·배달점·주문 ──
        log.Add(Garage(paths));
        log.Add(DeliveriesAndOrders());

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        return "built " + ScenePath + " | " + string.Join(" | ", log);
    }

    static string Hash(List<PP> seg) => $"{seg[0].p.x:F1},{seg[0].p.z:F1}>{seg[seg.Count - 1].p.x:F1},{seg[seg.Count - 1].p.z:F1}#{seg.Count}";

    // ── 꺾은선 + 모서리 둥글리기 → 1m 점. 모서리 반경 12m(헤어핀 노드 간격 30m 는 15m → 반원) ──
    public static List<PP> Path(string spec)
    {
        var tok = spec.Split(' ');
        var nodes = new List<Vector3>(); var kinds = new List<string>();
        for (int i = 0; i < tok.Length; i += 2) { nodes.Add(N[tok[i]]); if (i + 1 < tok.Length) kinds.Add(tok[i + 1]); }
        var outp = new List<PP>();
        void Add(Vector3 p, string k) { if (outp.Count > 0 && (new Vector2(p.x - outp[^1].p.x, p.z - outp[^1].p.z)).magnitude < .5f) return; outp.Add(new PP { p = p, kind = k }); }
        Vector3 YOn(int seg, Vector2 q) { var a = nodes[seg]; var b = nodes[seg + 1]; var ab = new Vector2(b.x - a.x, b.z - a.z); float t = Mathf.Clamp01(Vector2.Dot(q - new Vector2(a.x, a.z), ab) / ab.sqrMagnitude); return new Vector3(q.x, Mathf.Lerp(a.y, b.y, t), q.y); }
        Vector2 cur = new(nodes[0].x, nodes[0].z);
        for (int i = 1; i < nodes.Count; i++)
        {
            var a = new Vector2(nodes[i - 1].x, nodes[i - 1].z); var b = new Vector2(nodes[i].x, nodes[i].z);
            Vector2 endStraight = b; Vector2 t2 = b, cen = default; float r = 0, ang = 0; bool arc = false;
            if (i < nodes.Count - 1)
            {
                var c = new Vector2(nodes[i + 1].x, nodes[i + 1].z);
                var din = (b - a).normalized; var dout = (c - b).normalized;
                ang = Vector2.SignedAngle(din, dout);
                if (Mathf.Abs(ang) > 2f)
                {
                    r = (b - a).magnitude <= 31f && (c - b).magnitude <= 31f || Mathf.Abs(Mathf.Abs(ang) - 90f) < 1f && ((b - a).magnitude <= 31f || (c - b).magnitude <= 31f) ? 15f : 12f;
                    float d = r * Mathf.Tan(Mathf.Abs(ang) * .5f * Mathf.Deg2Rad);
                    float dmax = .5f * Mathf.Min((b - a).magnitude, (c - b).magnitude); if (d > dmax) { d = dmax; r = d / Mathf.Tan(Mathf.Abs(ang) * .5f * Mathf.Deg2Rad); }
                    endStraight = b - din * d; t2 = b + dout * d; arc = true;
                    var nrm = new Vector2(-din.y, din.x) * Mathf.Sign(ang); cen = endStraight + nrm * r;
                }
            }
            int n = Mathf.Max(1, Mathf.CeilToInt((endStraight - cur).magnitude));
            for (int k = 0; k <= n; k++) Add(YOn(i - 1, Vector2.Lerp(cur, endStraight, k / (float)n)), kinds[i - 1]);
            if (arc)
            {
                float y1 = YOn(i - 1, endStraight).y, y2 = YOn(i, t2).y;
                var v0 = endStraight - cen; int m = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(ang) * Mathf.Deg2Rad * r));
                for (int k = 1; k <= m; k++)
                {
                    float u = k / (float)m; var v = (Vector2)(Quaternion.Euler(0, 0, ang * u) * v0); var q = cen + v;
                    Add(new Vector3(q.x, Mathf.Lerp(y1, y2, Mathf.SmoothStep(0, 1, u)), q.y), u < .5f ? kinds[i - 1] : kinds[i]);
                }
            }
            cur = arc ? t2 : b;
        }
        return outp;
    }

    static Vector3 Fw(List<PP> P, int i) { var f = P[Mathf.Min(i + 1, P.Count - 1)].p - P[Mathf.Max(i - 1, 0)].p; f.y = 0; return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized; }

    // ── 길 메시: 윗면 + 양옆 치마(4m 아래로) ──
    static void RoadMesh(Transform parent, string name, List<PP> P, string kind, Material skirtM)
    {
        var (w, col, dy) = Kinds[kind]; float h = w * .5f;
        var v = new List<Vector3>(); var t = new List<int>(); var ts = new List<int>();
        for (int i = 0; i < P.Count; i++)
        {
            var r = Vector3.Cross(Vector3.up, Fw(P, i)); var c = P[i].p + Vector3.up * dy;
            v.Add(c - r * h); v.Add(c + r * h); v.Add(c - r * h + Vector3.down * 4f); v.Add(c + r * h + Vector3.down * 4f);
            if (i == 0) continue; int a = (i - 1) * 4, b = i * 4;
            t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            ts.AddRange(new[] { a, a + 2, b, b, a + 2, b + 2 });        // 왼쪽 치마(바깥을 봄)
            ts.AddRange(new[] { a + 1, b + 1, a + 3, a + 3, b + 1, b + 3 }); // 오른쪽 치마
        }
        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(v); mesh.subMeshCount = 2; mesh.SetTriangles(t, 0); mesh.SetTriangles(ts, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Dir + "/" + name + ".asset");
        var go = new GameObject("PP_" + kind + "_" + name); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { M("Road_" + kind, col), skirtM };
        var colMesh = new Mesh { name = name + "_col" }; colMesh.SetVertices(v); colMesh.SetTriangles(t, 0); colMesh.RecalculateBounds();
        AssetDatabase.CreateAsset(colMesh, Dir + "/" + name + "_col.asset");
        go.AddComponent<MeshCollider>().sharedMesh = colMesh;
        var rs = go.AddComponent<RoadSurface>(); rs.roadId = name; rs.kind = kind; rs.width = w; rs.roughness = kind == "cobble" ? 1f : kind == "dirt" ? .6f : kind == "alley" ? .3f : 0f;
    }

    // ── 돌길 범프(실제 콜라이더, 1.6m 마다 9cm), 흙 지름길 턱(15cm 둔덕 3개), 골목 벽(진회색, 높이 5~9m) ──
    static string Features(Dictionary<string, List<PP>> paths)
    {
        var g = new GameObject("Features").transform; g.SetParent(root);
        var bumpM = M("CobbleStripe", new Color(.36f, .24f, .15f)); var wallM = M("AlleyWall", new Color(.26f, .26f, .28f)); var humpM = M("DirtHump", new Color(.62f, .45f, .22f));
        var done = new HashSet<string>(); int bumps = 0, humps = 0, walls = 0;
        foreach (var (id, _) in Routes)
        {
            var P = paths[id]; float acc = 0; int dirtSeen = 0;
            for (int i = 1; i < P.Count; i++)
            {
                acc += (P[i].p - P[i - 1].p).magnitude;
                string key = $"{P[i].p.x:F0},{P[i].p.z:F0}"; var k = P[i].kind;
                var f = Fw(P, i); var r = Vector3.Cross(Vector3.up, f); var (w, _, dy) = Kinds[k];
                if (k == "cobble" && acc >= 1.6f && done.Add("c" + key))
                {
                    acc = 0; var go = new GameObject("Bump"); go.transform.SetParent(g);
                    go.transform.position = P[i].p + Vector3.up * (dy + .09f - .3f); go.transform.rotation = Quaternion.LookRotation(r, Vector3.up);
                    var cc = go.AddComponent<CapsuleCollider>(); cc.direction = 2; cc.radius = .3f; cc.height = w;
                    var vis = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(vis.GetComponent<Collider>()); vis.transform.SetParent(go.transform, false);
                    vis.transform.localPosition = new Vector3(0, .2f, 0); vis.transform.localScale = new Vector3(.55f, .2f, w); vis.GetComponent<Renderer>().sharedMaterial = bumpM; bumps++;
                }
                else if (k == "dirt" && done.Add("d" + key)) { dirtSeen++; if (dirtSeen % 45 == 20) { Hump(g, P[i].p + Vector3.up * dy, r, w, .15f, humpM); humps++; } }
                else if (k == "alley" && i % 3 == 0 && done.Add("a" + key))
                {
                    foreach (int sd in new[] { -1, 1 })
                    {
                        // 다른 종류 길(갈림목·합류점)에 걸치는 벽은 안 세운다 — 봇 1차: D2 합류점·GN 갈림목에서 벽에 막혀 멈춤
                        var wp = P[i].p + r * sd * (w * .5f + .6f);
                        if (paths.Values.Any(Q => Q.Any(q => q.kind != "alley" && (new Vector2(q.p.x - wp.x, q.p.z - wp.z)).magnitude < Kinds[q.kind].w * .5f + 1.5f))) continue;
                        float hh = 5f + Mathf.Abs(Mathf.Sin(P[i].p.x * 1.7f + P[i].p.z * 0.9f + sd)) * 4f;
                        var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = "AlleyWall"; b.transform.SetParent(g);
                        b.transform.position = P[i].p + r * sd * (w * .5f + .6f) + Vector3.up * (hh * .5f - 1f); b.transform.rotation = Quaternion.LookRotation(f);
                        b.transform.localScale = new Vector3(1.2f, hh, 3.4f); b.GetComponent<Renderer>().sharedMaterial = wallM; walls++;
                    }
                    if (id == "D4_short" && i % 30 == 0) { Hump(g, P[i].p + Vector3.up * dy, r, w, .10f, humpM); humps++; }   // 오르막 골목 턱
                }
            }
        }
        return $"돌길 범프 {bumps}(9cm) 턱 {humps} 골목 벽 {walls}";
    }

    static void Hump(Transform g, Vector3 at, Vector3 right, float w, float hgt, Material m)
    {
        var go = new GameObject("Hump"); go.transform.SetParent(g); go.transform.position = at + Vector3.up * (hgt - .8f); go.transform.rotation = Quaternion.LookRotation(right, Vector3.up);
        var cc = go.AddComponent<CapsuleCollider>(); cc.direction = 2; cc.radius = .8f; cc.height = w + 1.6f;
        var vis = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(vis.GetComponent<Collider>()); vis.transform.SetParent(go.transform, false);
        vis.transform.localPosition = new Vector3(0, .8f - hgt * .5f, 0); vis.transform.localScale = new Vector3(1.2f, hgt, w); vis.GetComponent<Renderer>().sharedMaterial = m;
    }

    // ── 지형: 기본 비탈(해안 2m → 북쪽 32m, 언덕 혹) + 길 주변 15m 안은 길 높이로 깎고 메움 ──
    static float Base(float x, float z)
    {
        if (z < -222f) return Mathf.Lerp(2f, -3f, Mathf.Clamp01((-222f - z) / 12f));
        float s = Mathf.SmoothStep(0, 1, Mathf.Clamp01((z + 185f) / 255f));
        return 2f + 30f * s + 6f * Mathf.Exp(-((x - 100f) * (x - 100f) + (z - 120f) * (z - 120f)) / 1800f);
    }

    static void Terrain(Dictionary<string, List<PP>> paths)
    {
        const float step = 4f, x0 = -300, x1 = 300, z0 = -250, z1 = 260;
        var grid = new Dictionary<(int, int), List<(Vector3 p, float h)>>();
        foreach (var P in paths.Values) foreach (var q in P) { var key = ((int)Mathf.Floor(q.p.x / 8f), (int)Mathf.Floor(q.p.z / 8f)); if (!grid.TryGetValue(key, out var l)) grid[key] = l = new(); l.Add((q.p, Kinds[q.kind].w * .5f)); }
        int nx = (int)((x1 - x0) / step) + 1, nz = (int)((z1 - z0) / step) + 1;
        var v = new Vector3[nx * nz]; var tri = new List<int>();
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float x = x0 + i * step, z = z0 + j * step, y = Base(x, z);
                float bd = 1e9f, by = 0, bh = 0; int cx = (int)Mathf.Floor(x / 8f), cz = (int)Mathf.Floor(z / 8f);
                for (int a = -3; a <= 3; a++) for (int b = -3; b <= 3; b++)
                        if (grid.TryGetValue((cx + a, cz + b), out var l)) foreach (var (p, h) in l) { float d = (p.x - x) * (p.x - x) + (p.z - z) * (p.z - z); if (d < bd) { bd = d; by = p.y; bh = h; } }
                bd = Mathf.Sqrt(bd);
                if (bd < bh + 16f) y = Mathf.Lerp(by - .35f, y, Mathf.SmoothStep(0, 1, Mathf.Clamp01((bd - bh - 1.5f) / 14.5f)));
                v[j * nx + i] = new Vector3(x, y, z);
                if (i > 0 && j > 0) { int p00 = (j - 1) * nx + i - 1, p10 = p00 + 1, p01 = j * nx + i - 1, p11 = p01 + 1; tri.AddRange(new[] { p00, p01, p10, p10, p01, p11 }); }
            }
        var mesh = new Mesh { name = "PP_Terrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.vertices = v; mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Dir + "/PP_Terrain.asset");
        var go = new GameObject("Terrain"); go.transform.SetParent(root);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = M("Ground", new Color(.50f, .58f, .42f));
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // ── 차고 + 차·카메라 ──
    static string Garage(Dictionary<string, List<PP>> paths)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = "Garage"; g.transform.SetParent(root);
        g.transform.position = N["G"] + new Vector3(-16, 2.5f, 12); g.transform.localScale = new Vector3(12, 5, 10); g.GetComponent<Renderer>().sharedMaterial = M("Garage", new Color(.40f, .48f, .62f));
        var car = Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
        car.transform.position = N["G"] + new Vector3(0, .6f, 4); car.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        var cso = new SerializedObject(car);
        cso.FindProperty("fallRespawnY").floatValue = -6f; cso.FindProperty("safePointAboveFall").floatValue = 4f; cso.ApplyModifiedPropertiesWithoutUndo();
        var cl = new GameObject("CourseCenterline").AddComponent<CourseCenterline>(); cl.transform.SetParent(root);
        var pts = new List<Vector3>(); var fws = new List<Vector3>();
        foreach (var P in paths.Values) for (int i = 0; i < P.Count; i += 2) { pts.Add(P[i].p); fws.Add(Fw(P, i)); }
        cl.points = pts.ToArray(); cl.forwards = fws.ToArray(); EditorUtility.SetDirty(cl);
        string camInfo = "카메라 없음";
        var fc = Object.FindFirstObjectByType<FollowCamera>(FindObjectsInactive.Include);
        if (fc != null)
        {
            fc.transform.position = car.transform.position - Vector3.forward * 5.6f + Vector3.up * 2.8f; fc.transform.LookAt(car.transform.position + Vector3.forward * 8f);
            var cam = fc.GetComponent<Camera>(); if (cam != null) cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1200f);
            CameraFeelSwitcher.BuildPresets(false);
            var pr = AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.B); fc.Preset = pr; EditorUtility.SetDirty(fc);
            camInfo = "카메라 B " + (pr != null ? JsonUtility.ToJson(pr) : "null");
        }
        return camInfo;
    }

    // ── 배달점 4(기존 DeliveryPoint 옮김) + 이 씬 전용 주문 4건(화물 섞기) ──
    static string DeliveriesAndOrders()
    {
        var dps = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        var jobs = new List<OrderJob>(); var sb = new StringBuilder();
        var pillarM = M("DeliveryPillar", new Color(.95f, .80f, .20f));
        foreach (var (node, label, name, cargo) in Deliveries)
        {
            var dp = dps.FirstOrDefault(d => d.Label == label || d.name == label);
            if (dp == null) { sb.Append($"[{label} 없음] "); continue; }
            dp.transform.position = N[node] + Vector3.up * .3f;
            // 눈에 보이는 기둥(콜라이더 없음) — 배달점 위치만 알려줌(지름길 표시는 없음)
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder); p.name = "Pillar_" + node; Object.DestroyImmediate(p.GetComponent<Collider>()); p.transform.SetParent(root);
            p.transform.position = N[node] + new Vector3(Kinds["main"].w * .5f + 2.5f, 3f, 0); p.transform.localScale = new Vector3(1.2f, 3f, 1.2f); p.GetComponent<Renderer>().sharedMaterial = pillarM;
            var path = Dir + "/Order_" + node + ".asset";
            var job = AssetDatabase.LoadAssetAtPath<OrderJob>(path); if (job == null) { job = ScriptableObject.CreateInstance<OrderJob>(); AssetDatabase.CreateAsset(job, path); }
            job.displayName = name; job.destinationLabels = new List<string> { label }; job.cargoType = cargo; job.timeLimitSec = 0f; job.baseReward = 500;
            job.color = cargo == CargoType.Box ? new Color(.4f, .7f, 1f) : new Color(1f, .5f, .4f); EditorUtility.SetDirty(job); jobs.Add(job);
            sb.Append($"{node}={label}({cargo}) ");
        }
        var catPath = Dir + "/OrderCatalog_Play01.asset";
        var cat = AssetDatabase.LoadAssetAtPath<OrderCatalog>(catPath); if (cat == null) { cat = ScriptableObject.CreateInstance<OrderCatalog>(); AssetDatabase.CreateAsset(cat, catPath); }
        cat.jobs = jobs; EditorUtility.SetDirty(cat);
        var board = Object.FindFirstObjectByType<OrderBoard>(FindObjectsInactive.Include);
        if (board != null)
        {
            var so = new SerializedObject(board);
            so.FindProperty("catalog").objectReferenceValue = cat; so.FindProperty("offerCount").intValue = 0; so.FindProperty("urgentCallChance").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // 해금 구역 풀이 있으면 catalog 대신 그걸 쓴다 → 이 씬에선 끔
        int zu = 0; foreach (var z in Object.FindObjectsByType<ZoneUnlockSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { Object.DestroyImmediate(z); zu++; }
        var dm = Object.FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        var ret = new GameObject("GarageReturn").transform; ret.SetParent(root); ret.position = N["G"] + Vector3.up * .3f;
        if (dm != null) { var so = new SerializedObject(dm); so.FindProperty("startReturn").objectReferenceValue = ret; so.ApplyModifiedPropertiesWithoutUndo(); }
        return "배달 " + sb + $"· 주문판 {(board != null ? "연결" : "없음")} · 해금시스템 제거 {zu}";
    }
}
