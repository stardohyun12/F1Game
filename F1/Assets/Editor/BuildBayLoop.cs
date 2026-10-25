using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 평면도 v4(docs/map/bayloop-plan.json) → 운전 가능한 씬 Map_BayLoop.
///  1) Proto_CoastCourse 를 복사(차·카메라·HUD·시스템과 씬 오버라이드 그대로), 코스 루트(ProtoCourse)만 지운다.
///  2) 지형 = 메시(2m 정밀 + 16m 먼 판). 높이 = 제어점 IDW → ② 구간은 BuildHilltopSlice.HilltopHeight(+30) 이식 → 바다 윤곽 절벽·해변 → 길 깎기/메우기
///     → 열린 땅(광장·부두·빈터) 옆 차길 높이로 평탄화(D-138).
///  3) 길 = Splines(AutoSmooth) 1m 샘플 → 띠 메시. 점마다 폭(widthProfile) + 양쪽 단면 띠(crossSection: 연석·흰선·도랑·공유 포장, D-139),
///     낭떠러지 쪽 난간. RoadSurface 에 roughness 태그(연석 0.6). 볼라드 없음 — 계단 입구에만 화분.
///  4) 필지 → 구역(D-140) type 가중치 → Art/BayLoop/Buildings/&lt;type&gt;_*.prefab, 없으면 building-types.md 폴백 열(LofiPilot), 그것도 없으면 블록아웃.
///     같은 프리팹 반경 30m 안 2채 이하. facing(front/back/side), 앞마당 소품(가벼운 것만 리지드바디).
///  5) features: jump 둔턱 메시, gap 돌담, landmark 블록아웃. 전봇대·가로등·인도 가구·나무·밭·배달지·MART·원경(산 고리·먼 섬).
/// 모든 수치는 가설 — 보고서 programmer-20261025-1645, programmer-20261025-2023(v4).
/// </summary>
public static partial class BuildBayLoop
{
    const string SrcScene = "Assets/Scenes/Proto_CoastCourse.unity";
    public const string ScenePath = "Assets/Scenes/Map_BayLoop.unity";
    const string MatDir = "Assets/Scenes/Map_BayLoop_Mats";
    const string BldDir = "Assets/Art/BayLoop/Buildings";
    const string Lofi = "Assets/Art/LofiPilot/";
    static string JsonPath => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/map/bayloop-plan.json"));

    // 지형 격자: 정밀 2m(놀이 영역 + 24m), 먼 판 16m. 경계는 16 의 배수.
    const float X0 = -208f, Z0 = -240f, X1 = 192f, Z1 = 288f, Step = 2f;
    const float FarX0 = -960f, FarZ0 = -960f, FarX1 = 960f, FarZ1 = 1120f, FarStep = 16f;
    const float Blend = 6f;

    class Road
    {
        public string id, kind; public float width, rough; public bool closed, follows = true, overWater;
        public List<Vector3> knots = new List<Vector3>(); public List<Dictionary<string, object>> features = new List<Dictionary<string, object>>();
        public bool startJoin, endJoin; public string startJoinId, endJoinId;
        public List<Vector3> pts; public float len;
        // v4(D-139): 점마다 폭(widthProfile, 경계 ±12m 녹임)·단면(crossSection). 단면이 있으면 옛 어깨 대신 인도 띠가 붙는다.
        public List<object> prof; public Dictionary<string, object> xs0; public float[] w; public Dictionary<string, object>[] xs; public bool[] coast;
        public bool Car => kind == "main" || kind == "village" || kind == "shortcut";
        public float Shoulder => xs0 != null || prof != null ? 0f : kind == "main" ? 0.8f : kind == "village" ? 0.4f : 0f;
        public float Half => width * .5f + Shoulder;
        public float LaneHalf(int i) => (w != null ? w[i % w.Length] : width) * .5f + Shoulder;
        public Dictionary<string, object> Side(int i, int side) => xs == null || xs[i % xs.Length] == null ? null : (Dictionary<string, object>)xs[i % xs.Length][side > 0 ? "left" : "right"];
        public float Walk(int i, int side) { var s = Side(i, side); return s == null ? 0f : F(s["walk"]); }
        public string Edge(int i, int side) { var s = Side(i, side); return s == null ? "none" : (string)s["edge"]; }
    }

    static Dictionary<string, object> J;
    static List<Road> roads;
    static List<Vector4> cps;            // (x, z, y, 0)
    static List<Vector2> landPoly;
    static List<Vector4> beaches;
    static float seaFloor, cliffW, idwPow, idwSoft;
    static float[,] terr;
    static Transform root;
    static readonly List<string> log = new List<string>();
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    [MenuItem("PitCrew/Map/Build BayLoop")]
    public static void Menu() { Debug.Log("[BayLoop] " + BuildScene()); }

    public static string BuildScene()
    {
        log.Clear(); mats.Clear();
        bandCount = 0; curbM = railM = 0f; propCache.Clear(); artPools = null; fbPools = null;
        Load();
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Map_BayLoop_Mats");
        AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SrcScene, ScenePath)) return "copy failed";
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects()) if (g.name == "ProtoCourse") Object.DestroyImmediate(g);
        root = new GameObject("BayLoop").transform;

        // ── 길 스플라인 ──
        var splGo = new GameObject("RoadSplines"); splGo.transform.SetParent(root);
        var container = splGo.AddComponent<SplineContainer>();
        for (int ri = 0; ri < roads.Count; ri++)
        {
            var r = roads[ri];
            var sp = ri == 0 ? container.Spline : container.AddSpline(); sp.Clear();
            foreach (var k in r.knots) sp.Add(new BezierKnot(new float3(k.x, k.y, k.z)), r.knots.Count > 2 ? TangentMode.AutoSmooth : TangentMode.Linear);
            sp.Closed = r.closed;
            r.pts = Sample(sp, 1f, r.closed); r.len = Len(r.pts, r.closed);
            Profile(r);
        }
        // 갈림목·교차 높이 맞춤: 아래 위계 길의 점이 윗 위계 길 띠와 겹치는 동안(평면 거리 < 두 반폭 합)은 윗 길 높이 그대로,
        // 그 앞뒤 12m 에 원래 높이로 녹인다. 비스듬히 갈라지며 바로 오르는 갈래가 본선 가장자리 위로 1m 떠 있던 것(검사 높이+1.04) 방지.
        float maxJoin = 0f;
        for (int ri = 0; ri < roads.Count; ri++)
        {
            var r = roads[ri]; if (r.overWater) continue;
            int cnt = r.pts.Count; var delta = new float[cnt]; var snap = new bool[cnt];
            for (int pj = 0; pj < roads.Count; pj++)
            {
                var par = roads[pj];
                if (par == r || !par.Car || (Rank(par), pj).CompareTo((Rank(r), ri)) >= 0) continue;
                for (int i = 0; i < cnt; i++)
                {
                    if (snap[i]) continue;
                    var q = r.pts[i]; var pp = par.pts[(int)Project(par, q.x, q.z)];
                    if (new Vector2(q.x - pp.x, q.z - pp.z).magnitude > par.Half + r.Half + 0.5f) continue;
                    snap[i] = true; delta[i] = pp.y - q.y; maxJoin = Mathf.Max(maxJoin, Mathf.Abs(delta[i]));
                }
            }
            var ny = new float[cnt];
            for (int i = 0; i < cnt; i++)
            {
                if (snap[i]) { ny[i] = r.pts[i].y + delta[i]; continue; }
                float best = 0f, wBest = 0f;
                for (int k = 1; k <= 12; k++)
                    foreach (int j in new[] { i - k, i + k })
                    {
                        int jj = r.closed ? (j + cnt) % cnt : j;
                        if (jj < 0 || jj >= cnt || !snap[jj]) continue;
                        float w = 1f - S(k / 12f); if (w > wBest) { wBest = w; best = delta[jj]; }
                    }
                ny[i] = r.pts[i].y + best * wBest;
            }
            for (int i = 0; i < cnt; i++) { var q = r.pts[i]; q.y = ny[i]; r.pts[i] = q; }
        }
        var mainR = roads.First(r => r.id == "main");
        log.Add($"갈림목·교차 높이차 최대 {maxJoin:F2}m → 윗 길 높이로 맞추고 앞뒤 12m 에 녹임");
        log.Add($"길 {roads.Count}개, 본선 {mainR.len:F0}m, 차길 합 {roads.Where(r => r.Car).Sum(r => r.len):F0}m, 본선 최대경사 {MaxGrade(mainR.pts):P0}");

        // ── 지형 ──
        terr = CutFill(out float maxCut);
        log.Add($"지형 {terr.GetLength(0)}x{terr.GetLength(1)}@{Step}m, 길 밑 최대 깎기/메우기 {maxCut:F1}m");
        log.Add(FlattenOpenGround());
        InitCarPts(); InitParcelRects(); InitCarGrid();   // v5: 지면 포장 판정에 필지·차길이 필요
        gaps.Clear(); sparseYards.Clear(); frontSlots.Clear(); shopFronts.Clear(); pfH.Clear();
        pavedCells = islandCells = sameSb3 = hPairs = hPairsDiff = yawJit = natGaps = cutGaps = 0;
        var gT = Group("Terrain");
        TerrainMesh(gT, M("Ground"));
        FarTerrainMesh(gT, M("Ground"));
        var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane); seaGo.name = "Sea"; seaGo.transform.SetParent(root);
        seaGo.transform.position = new Vector3(0f, F(Get(J, "sea", "level")), 0f); seaGo.transform.localScale = new Vector3(420f, 1f, 420f);
        Object.DestroyImmediate(seaGo.GetComponent<Collider>()); seaGo.GetComponent<Renderer>().sharedMaterial = M("Sea");

        // ── 길 메시 ──
        var gR = Group("Roads");
        foreach (var r in roads) RoadMesh(gR, r);
        log.Add($"인도 띠 {bandCount}장(연석 {curbM:F0}m), 난간 {railM:F0}m");
        log.Add(OpenGroundMeshes(Group("OpenGround")));

        // ── 건물·지물 ── (v4: 볼라드 없음 — 계단 입구에만 화분, D-138 은근히 자유로운 동네)
        string bInfo = Buildings(Group("Buildings"));
        log.Add(bInfo);
        StairPlanters(Group("StairPlanters"));
        log.Add(Furniture(Group("StreetFurniture")));
        log.Add(Trees(Group("Trees")));
        log.Add(Features(Group("Features")));
        log.Add(Poles(Group("Poles")));
        Fields(Group("Fields"));
        log.Add(Stops(Group("Stops")));
        log.Add(LivedIn(Group("LivedIn")));   // v5 D-141 생활 층
        log.Add(BuildWindowGlow.Run());   // D-137 초저녁 창 불(기본 꺼짐, 무드가 켠다)

        // ── 차·카메라·주행 ──
        Car(mainR);
        RemapNothing();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        return "built " + ScenePath + " | " + string.Join(" | ", log);
    }

    static void RemapNothing() { }

    // ------------------------------------------------------------------ json
    static void Load()
    {
        J = (Dictionary<string, object>)Json.Parse(System.IO.File.ReadAllText(JsonPath, Encoding.UTF8));
        var t = (Dictionary<string, object>)J["terrain"];
        seaFloor = F(t["seaFloor"]); cliffW = F(t["cliffWidth"]); idwPow = F(t["power"]); idwSoft = F(t["softening"]);
        cps = new List<Vector4>();
        foreach (List<object> c in (List<object>)t["controlPoints"]) cps.Add(new Vector4(F(c[0]), F(c[1]), F(c[2]), 0));
        beaches = new List<Vector4>();
        foreach (List<object> b in (List<object>)t["beaches"]) beaches.Add(new Vector4(F(b[0]), F(b[1]), F(b[2]), F(b[3])));
        landPoly = new List<Vector2>();
        foreach (List<object> p in (List<object>)Get(J, "sea", "landPolygon")) landPoly.Add(new Vector2(F(p[0]), F(p[1])));
        roads = new List<Road>();
        foreach (Dictionary<string, object> rd in (List<object>)J["roads"])
        {
            var r = new Road { id = (string)rd["id"], kind = (string)rd["kind"], width = F(rd["width"]) };
            r.rough = rd.TryGetValue("roughness", out var ro) && ro != null ? F(ro) : (r.kind == "stairs" ? 0f : 0.3f);
            r.closed = rd.TryGetValue("closed", out var cl) && cl is bool cb && cb;
            r.follows = !(rd.TryGetValue("terrainFollows", out var tf) && tf is bool tb && !tb);
            r.overWater = rd.TryGetValue("overWater", out var ow) && ow is bool ob && ob;
            foreach (List<object> k in (List<object>)rd["knots"]) r.knots.Add(new Vector3(F(k[0]), F(k[2]), F(k[1])));
            if (rd.TryGetValue("features", out var fe) && fe is List<object> fl) foreach (Dictionary<string, object> f in fl) r.features.Add(f);
            r.startJoin = rd.TryGetValue("start", out var st) && st is Dictionary<string, object> sd && sd.ContainsKey("join");
            r.endJoin = rd.TryGetValue("end", out var en) && en is Dictionary<string, object> ed && ed.ContainsKey("join");
            if (r.startJoin) r.startJoinId = ((Dictionary<string, object>)st)["join"] as string;
            if (r.endJoin) r.endJoinId = ((Dictionary<string, object>)en)["join"] as string;
            r.prof = rd.TryGetValue("widthProfile", out var wp) ? wp as List<object> : null;
            r.xs0 = rd.TryGetValue("crossSection", out var xo) ? xo as Dictionary<string, object> : null;
            roads.Add(r);
            if (r.follows && !r.overWater) foreach (var k in r.knots) cps.Add(new Vector4(k.x, k.z, k.y, 1));
        }
        log.Add($"json v{Get(J, "meta", "version")} 제어점 {cps.Count}(길 점 포함)");
    }

    /// <summary>planlib.road_widths / road_xsecs 와 같은 규칙: widthProfile 구간(본선 제어점 번호) 폭, 경계 ±12점 이동평균. 단면은 구간 값 그대로.</summary>
    static void Profile(Road r)
    {
        int n = r.pts.Count; r.w = Enumerable.Repeat(r.width, n).ToArray();
        r.xs = Enumerable.Repeat(r.xs0, n).ToArray();
        if (r.prof == null) return;
        var kidx = new List<int>(); int j = 0;
        foreach (var kp in r.knots)
        {
            float best = float.MaxValue; int bi = j;
            for (int i = j; i < Mathf.Min(n, j + 400); i++) { float d = new Vector2(r.pts[i].x - kp.x, r.pts[i].z - kp.z).magnitude; if (d < best) { best = d; bi = i; } }
            kidx.Add(bi); j = bi;
        }
        kidx.Add(n - 1);
        var raw = (float[])r.w.Clone(); r.coast = new bool[n];
        if (r.xs0 == null) for (int i = 0; i < n; i++) r.xs[i] = (Dictionary<string, object>)((Dictionary<string, object>)r.prof[0])["crossSection"];
        foreach (Dictionary<string, object> seg in r.prof)
        {
            int a = kidx[System.Convert.ToInt32(seg["from"])], b = kidx[Mathf.Min(System.Convert.ToInt32(seg["to"]), kidx.Count - 1)];
            bool cst = seg.TryGetValue("name", out var sn) && sn is string sns && sns.StartsWith("해안도로");
            for (int i = a; i <= b; i++) { raw[i] = F(seg["width"]); r.coast[i] = cst; if (seg.TryGetValue("crossSection", out var cs) && cs is Dictionary<string, object> cd) r.xs[i] = cd; }
        }
        for (int i = 0; i < n; i++)
        {
            float acc = 0; int c = 0;
            for (int k = -12; k <= 12; k++) { int q = r.closed ? ((i + k) % n + n) % n : Mathf.Clamp(i + k, 0, n - 1); acc += raw[q]; c++; }
            r.w[i] = acc / c;
        }
        r.width = r.w.Max();   // 갈림목 높이 맞춤·여유 계산은 가장 넓은 폭으로(보수적)
    }

    static object Get(Dictionary<string, object> d, params string[] keys)
    {
        object o = d; foreach (var k in keys) o = ((Dictionary<string, object>)o)[k]; return o;
    }
    static float F(object o) => System.Convert.ToSingle(o, CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ 높이
    static float S(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

    static float Idw(float x, float z)
    {
        double ws = 0, hs = 0;
        foreach (var c in cps)
        {
            double dx = x - c.x, dz = z - c.y, d2 = dx * dx + dz * dz + idwSoft * idwSoft;
            double w = 1.0 / System.Math.Pow(d2, idwPow * .5);
            ws += w; hs += w * c.z;
        }
        return (float)(hs / ws);
    }

    /// <summary>② 슬라이스 이식 범위(1=완전히 HilltopHeight+30). 서쪽 계단식 단(x&lt;−14)·남쪽 막이(z&lt;−10)는 범위 밖이라 자연히 빠진다.</summary>
    static float Mask2(float x, float z)
    {
        const float x0 = -12f, x1 = 140f, z0 = -4f, z1 = 228f, e = 12f;
        return S((x - x0) / e) * S((x1 - x) / e) * S((z - z0) / e) * S((z1 - z) / e);
    }

    static bool InPoly(float x, float z)
    {
        bool c = false;
        for (int i = 0, j = landPoly.Count - 1; i < landPoly.Count; j = i++)
        {
            var a = landPoly[i]; var b = landPoly[j];
            if ((a.y > z) != (b.y > z) && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x) c = !c;
        }
        return c;
    }

    /// <summary>바다 윤곽까지 거리(땅 안 +). 윤곽 동쪽·남쪽 끝(x 200, z −240)은 판을 닫는 선일 뿐이라 그 너머는 땅으로 본다.</summary>
    static float LandDist(float x, float z)
    {
        if ((x >= 195f && z < 268f) || (z <= -236f && x > -168f)) return 999f;
        float best = float.MaxValue;
        for (int i = 0; i < landPoly.Count; i++)
        {
            var a = landPoly[i]; var b = landPoly[(i + 1) % landPoly.Count];
            if ((a.x >= 199f && b.x >= 199f) || (a.y <= -239f && b.y <= -239f)) continue;   // 판 닫는 변
            best = Mathf.Min(best, DistSeg(new Vector2(x, z), a, b, out _));
        }
        return InPoly(x, z) ? best : -best;
    }

    static float BaseH(float x, float z)
    {
        float h = Idw(x, z);
        float m = Mask2(x, z);
        if (m > 0f) h = Mathf.Lerp(h, BuildHilltopSlice.HilltopHeight(x, z) + 30f, m);
        float d = LandDist(x, z);
        if (d < cliffW)
        {
            float sea = seaFloor + Mathf.Min(0f, d) * 0.03f;
            h = d <= 0f ? sea : Mathf.Lerp(sea, h, S(d / cliffW));
        }
        foreach (var b in beaches)
        {
            float ex = (x - b.x) / b.z, ez = (z - b.y) / b.w, e = ex * ex + ez * ez;
            if (e >= 1f) continue;
            float beachH = d >= 0f ? Mathf.Min(h, 0.5f + 0.08f * d) : Mathf.Max(seaFloor, 0.5f + 0.06f * d);
            h = Mathf.Lerp(h, beachH, S((1f - e) / 0.4f));
        }
        return h;
    }

    static float FarH(float x, float z)
    {
        float h = BaseH(x, z);
        // 먼 산 고리(지도 중심 (0,180) 반경 520) — 땅 쪽(동·남)에만. 서·북은 바다 + 먼 섬.
        var fm = (List<object>)J["farMasses"];
        foreach (Dictionary<string, object> m in fm)
        {
            var c = (List<object>)m["center"]; float cx = F(c[0]), cz = F(c[1]), H = F(m["height"]);
            float r = new Vector2(x - cx, z - cz).magnitude;
            if (m.TryGetValue("ringRadius", out var rr))
            {
                float R0 = F(rr) - 100f, land = Mathf.Max(S((x - 120f) / 140f), S((-z - 180f) / 90f));
                if (r > R0 && land > 0f)
                {
                    float th = Mathf.Atan2(z - cz, x - cx);
                    float n = 0.5f + 0.3f * Mathf.Sin(3f * th + 1f) + 0.2f * Mathf.Sin(7f * th + 2f) * Mathf.Sin(r * 0.02f);
                    h += (H * 0.6f + H * 0.5f * n) * S((r - R0) / 160f) * land;
                }
            }
            else if (r < F(m["radius"]) * 1.6f)
            {
                float u = r / F(m["radius"]);
                h = Mathf.Max(h, seaFloor + (H - seaFloor) * S(1f - u) * (0.85f + 0.15f * Mathf.Sin(x * 0.05f + z * 0.03f)));
            }
        }
        return h;
    }

    // ------------------------------------------------------------------ 깎기/메우기
    struct Seg { public Vector2 a, b; public float ya, yb, half, flat; public bool car; }

    static float[,] CutFill(out float maxCut)
    {
        int nx = Mathf.RoundToInt((X1 - X0) / Step) + 1, nz = Mathf.RoundToInt((Z1 - Z0) / Step) + 1;
        var t = new float[nx, nz]; var orig = new float[nx, nz]; roadMask = new bool[nx, nz];
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) t[i, j] = orig[i, j] = BaseH(X0 + i * Step, Z0 + j * Step);

        var segs = new List<Seg>();
        foreach (var r in roads)
        {
            if (r.overWater) continue;
            int n = r.pts.Count, m = r.closed ? n : n - 1;
            for (int i = 0; i < m; i++)
            {
                var p = r.pts[i]; var q = r.pts[(i + 1) % n];
                // v4: 반폭 = 차로 반폭 + 넓은 쪽 인도(점마다). 인도 띠 밑까지 −12cm 로 깎는다
                float hf = r.LaneHalf(i) + Mathf.Max(r.Walk(i, 1), r.Walk(i, -1));
                segs.Add(new Seg { a = new Vector2(p.x, p.z), b = new Vector2(q.x, q.z), ya = p.y, yb = q.y, half = hf, flat = hf + (r.Car ? 0.8f : 0.4f), car = r.Car });
            }
        }
        const float Cell = 8f;
        var buckets = new Dictionary<long, List<int>>();
        for (int si = 0; si < segs.Count; si++)
        {
            var sg = segs[si]; float reach = sg.flat + Blend;
            int ix0 = Mathf.FloorToInt((Mathf.Min(sg.a.x, sg.b.x) - reach - X0) / Cell), ix1 = Mathf.FloorToInt((Mathf.Max(sg.a.x, sg.b.x) + reach - X0) / Cell);
            int iz0 = Mathf.FloorToInt((Mathf.Min(sg.a.y, sg.b.y) - reach - Z0) / Cell), iz1 = Mathf.FloorToInt((Mathf.Max(sg.a.y, sg.b.y) + reach - Z0) / Cell);
            for (int a = ix0; a <= ix1; a++) for (int b = iz0; b <= iz1; b++)
                { long key = a * 100000L + b; if (!buckets.TryGetValue(key, out var l)) buckets[key] = l = new List<int>(); l.Add(si); }
        }
        maxCut = 0f;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                var v = new Vector2(X0 + i * Step, Z0 + j * Step);
                long key = Mathf.FloorToInt((v.x - X0) / Cell) * 100000L + Mathf.FloorToInt((v.y - Z0) / Cell);
                if (!buckets.TryGetValue(key, out var list)) continue;
                float best = float.MaxValue, bd = 0, by = 0; Seg bs = default; bool found = false;
                foreach (int si in list)
                {
                    var sg = segs[si];
                    float d = DistSeg(v, sg.a, sg.b, out float tt);
                    if (d > sg.flat + Blend) continue;
                    float score = d - sg.flat - (sg.car ? 0.5f : 0f);   // 차길이 골목보다 우선
                    if (score < best) { best = score; bd = d; by = Mathf.Lerp(sg.ya, sg.yb, tt); bs = sg; found = true; }
                }
                if (!found) continue;
                float o = orig[i, j], nh;
                if (bd <= bs.half + 0.6f) nh = by - 0.12f;
                else if (bd <= bs.flat) nh = by - 0.03f;
                else { float w = S((bd - bs.flat) / Blend); nh = by - 0.03f + (o - by + 0.03f) * w; }
                t[i, j] = nh;
                if (bd <= bs.flat) { maxCut = Mathf.Max(maxCut, Mathf.Abs(nh - o)); roadMask[i, j] = true; }
            }
        // 잔교(overWater) 밑 땅은 상판 −0.4m 아래로 눌러 둔다 — 해안 경사가 잔교 머리를 뚫고 올라와 차를 막았다(V_pier)
        foreach (var r in roads.Where(r => r.overWater))
            foreach (var p in r.pts)
            {
                float hr = r.LaneHalf(0) + 1.5f;
                int i0 = Mathf.Max(0, Mathf.FloorToInt((p.x - hr - X0) / Step)), i1 = Mathf.Min(nx - 1, Mathf.CeilToInt((p.x + hr - X0) / Step));
                int j0 = Mathf.Max(0, Mathf.FloorToInt((p.z - hr - Z0) / Step)), j1 = Mathf.Min(nz - 1, Mathf.CeilToInt((p.z + hr - Z0) / Step));
                for (int i = i0; i <= i1; i++) for (int j = j0; j <= j1; j++)
                    if (!roadMask[i, j] && new Vector2(X0 + i * Step - p.x, Z0 + j * Step - p.z).magnitude <= hr) t[i, j] = Mathf.Min(t[i, j], p.y - 0.4f);
            }
        return t;
    }
    static bool[,] roadMask;

    // ------------------------------------------------------------------ 열린 땅(D-138): 광장·부두·빈터는 옆 차길 높이로 녹인다
    static List<(Dictionary<string, object> g, List<Vector2> poly)> OG()
    {
        var o = new List<(Dictionary<string, object>, List<Vector2>)>();
        foreach (Dictionary<string, object> g in (List<object>)J["openGround"])
            o.Add((g, ((List<object>)g["poly"]).Cast<List<object>>().Select(p => new Vector2(F(p[0]), F(p[1]))).ToList()));
        return o;
    }
    static bool InP(List<Vector2> poly, float x, float z)
    {
        bool c = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            if ((poly[i].y > z) != (poly[j].y > z) && x < (poly[j].x - poly[i].x) * (z - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
        return c;
    }
    static float PolyDist(List<Vector2> poly, Vector2 v)
    {
        float d = float.MaxValue; for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++) d = Mathf.Min(d, DistSeg(v, poly[j], poly[i], out _)); return d;
    }

    /// <summary>flatten 열린 땅: 안쪽 격자 = 가장 가까운 차길 노면 −3cm(30m 안), 바깥 6m 에 원래 땅으로 녹임. 길 띠 칸은 안 건드린다.</summary>
    static string FlattenOpenGround()
    {
        int nx = terr.GetLength(0), nz = terr.GetLength(1), cells = 0; float maxD = 0;
        var cp = roads.Where(r => r.Car && !r.overWater).SelectMany(r => r.pts).ToList();
        var wp = roads.Where(r => r.overWater).SelectMany(r => r.pts.Select(p => (p, h: r.LaneHalf(0) + 2f))).ToList();   // 잔교 밑은 건드리지 않음(땅이 솟아 잔교를 막았다)
        foreach (var (g, poly) in OG())
        {
            if (!(g.TryGetValue("flatten", out var fl) && fl is bool fb && fb)) continue;
            float minX = poly.Min(p => p.x) - Blend, maxX = poly.Max(p => p.x) + Blend, minZ = poly.Min(p => p.y) - Blend, maxZ = poly.Max(p => p.y) + Blend;
            var near = cp.Where(p => p.x > minX - 30 && p.x < maxX + 30 && p.z > minZ - 30 && p.z < maxZ + 30).ToList();
            if (near.Count == 0) continue;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = X0 + i * Step, z = Z0 + j * Step;
                    if (x < minX || x > maxX || z < minZ || z > maxZ || roadMask[i, j]) continue;
                    var v = new Vector2(x, z); bool inside = InP(poly, x, z); float pd = inside ? 0f : PolyDist(poly, v);
                    if (pd > Blend) continue;
                    if (wp.Any(q => (q.p.x - x) * (q.p.x - x) + (q.p.z - z) * (q.p.z - z) < q.h * q.h)) continue;
                    var nr = near.OrderBy(p => (p.x - x) * (p.x - x) + (p.z - z) * (p.z - z)).First();
                    if (new Vector2(nr.x - x, nr.z - z).magnitude > 30f) continue;
                    float target = nr.y - 0.03f, wgt = inside ? 1f : 1f - S(pd / Blend);
                    float nh = Mathf.Lerp(terr[i, j], target, wgt); maxD = Mathf.Max(maxD, Mathf.Abs(nh - terr[i, j])); terr[i, j] = nh; cells++;
                }
        }
        return $"열린 땅 평탄화 {cells}칸, 최대 {maxD:F1}m";
    }

    /// <summary>열린 땅 겉면: 2m 칸 중심이 다각형 안이면 지형+2cm(평탄한 곳은 콜라이더·노면 태그) / +4cm(밭·풀밭·모래, 보기만).</summary>
    static string OpenGroundMeshes(Transform parent)
    {
        int n = 0;
        foreach (var (g, poly) in OG())
        {
            string kind = (string)g["kind"]; if (kind == "field") continue;   // 밭은 Fields() 가 띠로 깐다
            bool flat = g.TryGetValue("flatten", out var fl) && fl is bool fb && fb; float lift = flat ? 0.02f : 0.04f;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            float minX = poly.Min(p => p.x), maxX = poly.Max(p => p.x), minZ = poly.Min(p => p.y), maxZ = poly.Max(p => p.y);
            for (float x = Mathf.Floor(minX / 2f) * 2f; x < maxX; x += 2f)
                for (float z = Mathf.Floor(minZ / 2f) * 2f; z < maxZ; z += 2f)
                {
                    if (!InP(poly, x + 1f, z + 1f)) continue;
                    if (NearCar(new Vector3(x + 1f, 0f, z + 1f), 1.2f)) continue;   // 차로 위에 겹치면 턱이 된다(방파제 +0.14)
                    int b = verts.Count;
                    foreach (var (dx, dz) in new[] { (0f, 0f), (2f, 0f), (0f, 2f), (2f, 2f) }) { verts.Add(new Vector3(x + dx, H(x + dx, z + dz) + lift, z + dz)); uvs.Add(new Vector2(x + dx, z + dz)); }
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
                }
            if (verts.Count == 0) continue;
            string mat = kind == "plaza" ? "Paving" : kind == "quay" ? "Concrete" : kind == "beach" ? "Sand" : kind == "lot" ? "Gravel" : "Meadow";
            var go = MeshGo(parent, "OG_" + g["id"], verts, uvs, new[] { tris }, new[] { M(mat) }, flat);
            if (flat)
            {
                var rs = go.AddComponent<RoadSurface>(); rs.roadId = (string)g["id"]; rs.kind = kind; rs.width = 0f;
                rs.roughness = g.TryGetValue("roughness", out var ro) && ro != null ? F(ro) : 0.2f;
            }
            n++;
        }
        return $"열린 땅 겉면 {n}장";
    }

    public static float H(float x, float z)
    {
        if (terr == null || x <= X0 || x >= X1 || z <= Z0 || z >= Z1) return BaseH(x, z);
        float fx = (x - X0) / Step, fz = (z - Z0) / Step;
        int i = Mathf.Min((int)fx, terr.GetLength(0) - 2), j = Mathf.Min((int)fz, terr.GetLength(1) - 2); float u = fx - i, w = fz - j;
        // 메시 삼각형과 같은 분할(a-c-b, b-c-d)로 보간 — 쌍선형은 비탈에서 메시와 수 cm 어긋난다
        float a = terr[i, j], b = terr[i + 1, j], c = terr[i, j + 1], d = terr[i + 1, j + 1];
        return u + w <= 1f ? a + (b - a) * u + (c - a) * w : d + (c - d) * (1f - u) + (b - d) * (1f - w);
    }

    static float DistSeg(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        var ab = b - a; float l2 = ab.sqrMagnitude;
        t = l2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
        return Vector2.Distance(p, a + ab * t);
    }

    // ------------------------------------------------------------------ 메시
    static void TerrainMesh(Transform parent, Material mat)
    {
        int nx = terr.GetLength(0), nz = terr.GetLength(1);
        var verts = new List<Vector3>(nx * nz); var uvs = new List<Vector2>(nx * nz);
        for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) { float x = X0 + i * Step, z = Z0 + j * Step; verts.Add(new Vector3(x, terr[i, j], z)); uvs.Add(new Vector2(x, z)); }
        // v5 (D-141 ①): 칸마다 겉 — 풀 / 포장 4종(GroundKind). 밀집·보통 구역 인도~건물 사이 잔디 띠를 없앤다.
        var subs = new List<int>[1 + LotMats.Length]; for (int k = 0; k < subs.Length; k++) subs[k] = new List<int>();
        var green = OG().Where(o => (string)o.g["kind"] == "field" || (string)o.g["kind"] == "meadow").ToList();
        for (int j = 0; j + 1 < nz; j++)
            for (int i = 0; i + 1 < nx; i++)
            {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                float h0 = terr[i, j], h1 = terr[i + 1, j], h2 = terr[i, j + 1], h3 = terr[i + 1, j + 1];
                int kind = GroundKind(X0 + (i + .5f) * Step, Z0 + (j + .5f) * Step, Mathf.Min(Mathf.Min(h0, h1), Mathf.Min(h2, h3)), Mathf.Max(Mathf.Max(h0, h1), Mathf.Max(h2, h3)), green);
                var tris = subs[kind + 1];
                tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
            }
        MeshGo(parent, "BayTerrain", verts, uvs, subs, new[] { mat }.Concat(LotMats.Select(M)).ToArray(), true);
        log.Add($"지면 포장 {pavedCells}칸({pavedCells * Step * Step:F0}㎡, 차길 사이 섬 {islandCells}칸)");
    }

    static void FarTerrainMesh(Transform parent, Material mat)
    {
        int nx = Mathf.RoundToInt((FarX1 - FarX0) / FarStep) + 1, nz = Mathf.RoundToInt((FarZ1 - FarZ0) / FarStep) + 1;
        var verts = new List<Vector3>(nx * nz); var uvs = new List<Vector2>(nx * nz); var tris = new List<int>();
        bool Inside(float x, float z, float m) => x > X0 + m && x < X1 - m && z > Z0 + m && z < Z1 - m;
        for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++)
            {
                float x = FarX0 + i * FarStep, z = FarZ0 + j * FarStep;
                verts.Add(new Vector3(x, FarH(x, z) - (Inside(x, z, 0.1f) ? 1f : 0f), z)); uvs.Add(new Vector2(x, z));
            }
        for (int j = 0; j + 1 < nz; j++)
            for (int i = 0; i + 1 < nx; i++)
            {
                float cx = FarX0 + (i + .5f) * FarStep, cz = FarZ0 + (j + .5f) * FarStep;
                if (Inside(cx, cz, FarStep)) continue;
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
            }
        MeshGo(parent, "FarTerrain", verts, uvs, new[] { tris }, new[] { mat }, false);
    }

    /// <summary>길 띠. 차길 = 차로 콜라이더 한 장(점마다 폭) + 양쪽 단면 띠(Bands). 골목도 v4 부터 콜라이더(볼라드 없이 들어갈 수 있다). 계단만 보기만 + 입구 화분.</summary>
    static void RoadMesh(Transform parent, Road r)
    {
        float sh = r.Shoulder, hw = r.width * .5f;
        // 열 = 차로 반폭 기준 비율(−1..1). 어깨가 있는 옛 길은 [어깨|차로|어깨]
        float[] cols = sh > 0 ? new[] { -hw - sh, -hw, hw, hw + sh } : new[] { -hw, hw };
        // 갈래는 본선보다 3cm 아래 — 갈림목에서 본선 노면이 위에 보이고 넘어갈 때 턱은 3cm
        float lift = r.kind == "main" ? 0f : r.Car ? -0.03f : 0.04f;
        var lane = r.kind == "main" ? M("Asphalt") : r.kind == "village" ? M("Pavement") : r.kind == "shortcut" ? M("Gravel") : r.kind == "stairs" ? M("Stairs") : M("AlleyPath");
        var walk = r.kind == "main" ? M("Pavement") : M("Curb");
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tLane = new List<int>(); var tWalk = new List<int>();
        // 갈래 차길: 차로를 ~1m 열로 쪼개고, 윗 위계 길 띠 안에 들어간 정점은 그 길 노면 −3cm 아래로 누른다.
        // (갈래 띠는 가로로 평평해서, 12% 비탈 본선과 비스듬히 만나면 가장자리가 본선 위로 ~0.3m 솟아 차 상자가 걸렸다 — V_tunnel 실주행)
        var parents = (r.Car && r.kind != "main") || r.kind == "alley" ? roads.Where(q => q.Car && q != r && Rank(q) < Rank(r)).ToList() : new List<Road>();
        if (parents.Count > 0)
        {
            var cl = new List<float>(); int nLane = Mathf.Max(2, Mathf.CeilToInt(r.width / 1f));
            if (sh > 0) cl.Add(-hw - sh);
            for (int k = 0; k <= nLane; k++) cl.Add(-hw + r.width * k / nLane);
            if (sh > 0) cl.Add(hw + sh);
            cols = cl.ToArray();
        }
        var p = r.pts; int n = p.Count, nc = cols.Length; float s = 0; int pressed = 0;
        for (int i = 0; i <= (r.closed ? n : n - 1); i++)
        {
            var pi = p[i % n];
            if (i > 0) s += Vector3.Distance(p[i - 1], pi);
            var f = Fwd(p, i % n, r.closed); var rt = new Vector3(f.z, 0, -f.x);
            float scale = (r.w[i % n] * .5f) / hw;   // 점마다 폭: 차로 열만 늘이고 줄인다(어깨 폭은 그대로)
            foreach (var c0 in cols)
            {
                float c = Mathf.Abs(c0) <= hw + 1e-3f ? c0 * scale : Mathf.Sign(c0) * (hw * scale + sh);
                var v = pi + rt * c + Vector3.up * lift;
                // 윗 길 띠 안: 그 노면 −3cm 로 누름. 띠 밖 2m 띠: 윗 길 가장자리 높이로 이어 붙인다(갈래에서 본선으로 올라탈 때 턱 방지)
                foreach (var par in parents)
                {
                    float d = ParentSurface(par, v.x, v.z, out float py, out float phalf), inner = phalf + 0.2f;
                    if (d < inner) { if (v.y > py - 0.03f) { v.y = py - 0.03f; pressed++; } }
                    else if (d < inner + 2f) v.y = Mathf.Lerp(py - 0.03f, v.y, S((d - inner) / 2f));
                }
                verts.Add(v); uvs.Add(new Vector2(c, s));
            }
        }
        if (pressed > 0) log.Add($"{r.id} 갈림목 정점 {pressed}개 윗 길 아래로");
        int rows = verts.Count / nc;
        for (int i = 0; i + 1 < rows; i++)
            for (int k = 0; k + 1 < nc; k++)
            {
                int a = i * nc + k, b = a + 1, c = a + nc, d = c + 1;
                var l = Mathf.Abs((cols[k] + cols[k + 1]) * .5f) <= hw ? tLane : tWalk;
                l.Add(a); l.Add(c); l.Add(b); l.Add(b); l.Add(c); l.Add(d);
            }
        var go = MeshGo(parent, "Road_" + r.id, verts, uvs, tWalk.Count > 0 ? new[] { tLane, tWalk } : new[] { tLane }, tWalk.Count > 0 ? new[] { lane, walk } : new[] { lane }, r.kind != "stairs");
        var rs = go.AddComponent<RoadSurface>();
        rs.roadId = r.id; rs.kind = r.kind; rs.roughness = r.rough; rs.width = r.width; rs.closed = r.closed; rs.centerline = p.ToArray(); rs.widths = r.w.ToArray();
        Bands(go.transform, r, lift, lane, parents);
        if (r.kind == "stairs")
        {
            // 계단 단: 0.35m 마다 디딤판(보기만)
            for (float q = 0.5f; q < r.len - 0.5f; q += 0.9f)
            {
                At(r, q, out var c, out var f2, out _);
                Box(go.transform, "Step", new Vector3(r.width, 0.12f, 0.35f), c + Vector3.up * (lift + 0.06f), Quaternion.LookRotation(Flat(f2)), M("Stairs"), false);
            }
        }
        if (r.overWater)
        {
            for (float q = 0f; q < r.len; q += 4f)
            {
                At(r, q, out var c, out _, out var rt);
                foreach (var sd in new[] { -1f, 1f })
                {
                    var pp = c + rt * sd * (hw + sh - 0.3f); float g = H(pp.x, pp.z);
                    if (pp.y - g > 0.6f) Box(go.transform, "PierPost", new Vector3(0.35f, pp.y - g, 0.35f), new Vector3(pp.x, (pp.y + g) * .5f, pp.z), Quaternion.identity, M("Wood"), false);
                }
            }
        }
    }

    static int Rank(Road q) => q.kind == "main" ? 0 : q.kind == "village" ? 1 : q.kind == "shortcut" ? 2 : 3;

    /// <summary>(x,z) 에서 길 par 중심선까지 평면 거리, y = 그 투영점 노면 높이(띠는 가로로 평평).</summary>
    static float ParentSurface(Road par, float x, float z, out float y) => ParentSurface(par, x, z, out y, out _);
    static float ParentSurface(Road par, float x, float z, out float y, out float half)
    {
        y = 0; half = par.Half; var q = new Vector2(x, z); float best = float.MaxValue; int n = par.pts.Count, m = par.closed ? n : n - 1;
        for (int i = 0; i < m; i++)
        {
            var a = par.pts[i]; var b = par.pts[(i + 1) % n];
            if (Mathf.Abs(a.x - x) > 12f || Mathf.Abs(a.z - z) > 12f) continue;
            float d = DistSeg(q, new Vector2(a.x, a.z), new Vector2(b.x, b.z), out float t);
            if (d < best) { best = d; y = Mathf.Lerp(a.y, b.y, t); half = par.LaneHalf(i); }
        }
        return best;
    }

    static GameObject MeshGo(Transform parent, string name, List<Vector3> verts, List<Vector2> uvs, List<int>[] subs, Material[] ms, bool collider)
    {
        var mesh = new Mesh { name = name, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.subMeshCount = subs.Length;
        for (int k = 0; k < subs.Length; k++) mesh.SetTriangles(subs[k], k);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string mp = MatDir + "/Mesh_" + name + ".asset";
        AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
        var go = new GameObject(name); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = ms;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    // ------------------------------------------------------------------ 단면 띠(D-139)
    static int bandCount; static float curbM, railM;

    /// <summary>
    /// 차로 가장자리 바깥 양쪽 띠. 열: 차로 끝(노면) → 같은 자리 경계 높이(연석 면) → +0.25m(연석 윗면·배수로·흰선) → 인도 바깥 → 0.35m 아래(치마).
    /// curb 8~12cm 진짜 높이 콜라이더(넘을 수 있음, 거칠기 0.6 — 가설) / white_line 노면과 같은 높이 흰 띠 / gutter 3cm 낮은 회색 도랑 / none 같은 높이 돌포장.
    /// 다른 차길 차로(+0.3m) 안에 들어가는 칸은 빼서 갈림목 입구가 막히지 않게 한다.
    /// </summary>
    static void Bands(Transform parent, Road r, float lift, Material lane, List<Road> parents)
    {
        if (r.kind == "stairs") return;
        var p = r.pts; int n = p.Count, last = r.closed ? n : n - 1;
        var others = roads.Where(q => q.Car && q != r).ToList();
        foreach (int side in new[] { 1, -1 })
        {
            // 단면 종류별 메시: key = edge
            var parts = new Dictionary<string, (List<Vector3> v, List<Vector2> uv, List<int> tEdge, List<int> tWalk)>();
            var rail = new List<(Vector3 a, Vector3 b)>();
            float s = 0;
            for (int i = 0; i < last; i++)
            {
                int i1 = (i + 1) % n; s += Vector3.Distance(p[i], p[i1]);
                float wk = Mathf.Min(r.Walk(i, side), r.Walk(i1, side)); string edge = r.Edge(i, side);
                // v5 (D-141 ④): 흰 선은 본선 해안 구간만 — 지금 평면도의 해안 구간은 연석이라, 흰 선 단면은 전부 배수로(일본 동네 길 側溝)로 바꾼다
                if (edge == "white_line") edge = "gutter";
                var sd = r.Side(i, side);
                // v5b (D-141 ④): 흰 선은 해안도로 구간만 — 그 구간 연석 윗면을 흰 띠로, 나머지 연석 윗면은 아스팔트에 가까운 짙은 회색(.50 도 그늘 인도 옆에선 흰 선으로 읽혔다)
                string key = edge == "curb" && r.coast != null && r.coast[i] ? "curb_coast" : edge;
                // v5 (D-138·D-141 ⑦): 연석 10cm → 최대 6cm (넘어갈 수 있는 낮은 경계석)
                float h = edge == "curb" ? Mathf.Min(0.06f, sd != null && sd.TryGetValue("curbH", out var ch) ? F(ch) : 0.1f) : edge == "gutter" ? -0.03f : 0f;
                var row = new Vector3[2][];
                bool skip = wk < 0.3f;
                for (int e = 0; e < 2 && !skip; e++)
                {
                    int k = e == 0 ? i : i1; var f = Fwd(p, k, r.closed); var nrm = -side * new Vector3(f.z, 0, -f.x);   // side +1 = 진행 방향 왼쪽
                    float lh = r.LaneHalf(k), y0 = p[k].y + lift;
                    float strip = edge == "white_line" ? 0.15f : edge == "curb" ? 0.18f : 0.25f;
                    float[] off = { lh, lh, lh + strip, lh + wk, lh + wk };
                    float[] hy = { 0f, h, h, (edge == "gutter" ? 0f : h) + 0.02f, -0.35f };
                    row[e] = new Vector3[5];
                    for (int c = 0; c < 5; c++)
                    {
                        var v = p[k] + nrm * off[c]; v.y = y0 + hy[c];
                        foreach (var par in parents)
                        {
                            float d = ParentSurface(par, v.x, v.z, out float py, out float phalf);
                            if (d < phalf + 0.2f && c < 4 && v.y > py - 0.03f + hy[c]) v.y = py - 0.03f + Mathf.Max(0f, hy[c]);
                        }
                        row[e][c] = v;
                        if (c >= 1 && c <= 3) foreach (var o in others)
                            {
                                float d = ParentSurface(o, v.x, v.z, out _, out float oh);
                                if (d < oh + 0.3f) skip = true;
                            }
                    }
                }
                if (skip) continue;
                if (!parts.TryGetValue(key, out var pt)) parts[key] = pt = (new List<Vector3>(), new List<Vector2>(), new List<int>(), new List<int>());
                for (int c = 0; c < 4; c++)
                {
                    int b = pt.v.Count;
                    pt.v.Add(row[0][c]); pt.v.Add(row[0][c + 1]); pt.v.Add(row[1][c]); pt.v.Add(row[1][c + 1]);
                    foreach (var q in new[] { row[0][c], row[0][c + 1], row[1][c], row[1][c + 1] }) pt.uv.Add(new Vector2(q.x, q.z));
                    var l = c == 3 ? pt.tWalk : c == 2 ? pt.tWalk : pt.tEdge;
                    if (side < 0) { l.Add(b); l.Add(b + 2); l.Add(b + 1); l.Add(b + 1); l.Add(b + 2); l.Add(b + 3); }   // 오른쪽 = 차로와 같은 감김
                    else { l.Add(b); l.Add(b + 1); l.Add(b + 2); l.Add(b + 1); l.Add(b + 3); l.Add(b + 2); }
                }
                if (edge == "curb") curbM += 1f;
                // 난간: 단면이 railing 을 달고, 인도 바깥 1.5m 땅이 노면보다 2m 넘게 낮으면
                if (sd != null && r.xs[i] != null && r.xs[i].TryGetValue("railing", out var rl) && rl != null)
                {
                    var o0 = row[0][3]; var o1 = row[1][3]; var f0 = Fwd(p, i, r.closed); var nrm0 = -side * new Vector3(f0.z, 0, -f0.x);
                    var g = o0 + nrm0 * 1.5f;
                    if (o0.y - H(g.x, g.z) > 2f) rail.Add((o0 - nrm0 * 0.15f, o1 - nrm0 * 0.15f));
                }
            }
            foreach (var kv in parts)
            {
                string key = kv.Key, edge = key == "curb_coast" ? "curb" : key; var pt = kv.Value;
                string eMat = key == "curb_coast" ? "White" : edge == "curb" ? "CurbDark" : edge == "gutter" ? "Gutter" : edge == "white_line" ? "White" : "Paving";
                var wMat = edge == "curb" ? M("Pavement") : edge == "gutter" ? M("Curb") : edge == "white_line" ? lane : M("Paving");
                var go = MeshGo(parent, $"Band_{r.id}_{(side > 0 ? "L" : "R")}_{key}", pt.v, pt.uv, new[] { pt.tEdge, pt.tWalk }, new[] { M(eMat), wMat }, true);
                var rs = go.AddComponent<RoadSurface>(); rs.roadId = r.id; rs.kind = "walk_" + edge; rs.width = 0f;
                rs.roughness = edge == "curb" ? 0.6f : edge == "gutter" ? 0.45f : Mathf.Min(1f, r.rough + 0.1f);   // 가설: 연석을 넘으면 화물이 크게 흔들린다
                bandCount++;
            }
            // 난간 = 보이는 기둥·손잡이(콜라이더 없음) + 안 보이는 막이(INTENT_Guard, 1.2m)
            if (rail.Count > 0)
            {
                var g = new GameObject($"Railing_{r.id}_{(side > 0 ? "L" : "R")}").transform; g.SetParent(parent);
                for (int k = 0; k < rail.Count; k++)
                {
                    var (a, b) = rail[k]; var mid = (a + b) * .5f; var dir = b - a; float len = dir.magnitude; if (len < 0.05f) continue;
                    var rot = Quaternion.LookRotation(dir);
                    Box(g, "Rail", new Vector3(0.06f, 0.06f, len + 0.02f), mid + Vector3.up * 1.0f, rot, M("Dark"), false);
                    if (k % 2 == 0) Box(g, "Post", new Vector3(0.07f, 1.0f, 0.07f), a + Vector3.up * 0.5f, rot, M("Dark"), false);
                    var guard = Box(g, "INTENT_Guard", new Vector3(0.3f, 1.4f, len + 0.05f), mid + Vector3.up * 0.6f, rot, M("Dark"), true);
                    Object.DestroyImmediate(guard.GetComponent<MeshRenderer>());
                    railM += len;
                }
            }
        }
    }

    /// <summary>차로 점(차로 반폭+0.3m) — 전봇대·화분 비킴, 건물 침범(Intrudes) 기준. 건물 자리 비킴은 인도까지 포함한 clearPts.</summary>
    static void InitCarPts()
    {
        carPts = new List<(Vector3 p, float half)>(); clearPts = new List<(Vector3 p, float half)>();
        foreach (var r in roads.Where(q => q.Car))
            for (int i = 0; i < r.pts.Count; i++)
            {
                carPts.Add((r.pts[i], r.LaneHalf(i) + 0.3f));
                clearPts.Add((r.pts[i], r.LaneHalf(i) + Mathf.Min(r.Walk(i, 1), r.Walk(i, -1)) + 0.1f));
            }
    }
    static List<(Vector3 p, float half)> clearPts;

    // ------------------------------------------------------------------ 계단 입구 화분(v4: 볼라드 대신, 계단에만)
    static void StairPlanters(Transform parent)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Lofi + "Planter.prefab");
        int n = 0;
        foreach (var r in roads.Where(x => x.kind == "stairs"))
            foreach (bool atStart in new[] { true, false })
            {
                if (atStart ? !r.startJoin : !r.endJoin) continue;
                // 이유 있는 화분: 계단 첫 단 앞 1.5m 에 한 줄 — 차는 계단을 못 오르니 입구에서 멈추게, 사람은 사이로 지나간다
                float q = atStart ? 1.5f : r.len - 1.5f;
                if (r.len < 4f) q = r.len * .5f;
                At(r, q, out var c, out _, out var rt);
                int k = Mathf.Max(2, Mathf.CeilToInt((r.width + 0.2f) / 0.9f));
                // 비스듬히 붙은 골목은 4.2m 안쪽도 차길 띠 위일 수 있다 → 줄 전체가 차길에서 빠질 때까지 골목 안으로 1m씩(최대 길이 절반)
                System.Func<Vector3, Vector3, bool> rowOnCar = (cc, rr) => { for (int b = 0; b < k; b++) if (NearCar(cc + rr * ((b - (k - 1) * .5f) * 0.9f), 0.3f)) return true; return false; };
                for (int s = 0; s < 40 && rowOnCar(c, rt); s++)
                {
                    q += atStart ? 1f : -1f; if (q < 0.5f || q > r.len - 0.5f || Mathf.Abs(q - (atStart ? 0 : r.len)) > r.len * .5f) break;
                    At(r, q, out c, out _, out rt);
                }
                if (rowOnCar(c, rt)) continue;
                for (int b = 0; b < k; b++)
                {
                    float off = (b - (k - 1) * .5f) * 0.9f;
                    var pp = c + rt * off; pp.y = Mathf.Max(H(pp.x, pp.z), c.y) - 0.05f;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent); go.name = "INTENT_Planter_" + r.id;
                    go.transform.SetPositionAndRotation(pp, Quaternion.LookRotation(rt)); n++;
                }
            }
        log.Add($"볼라드 0 · 계단 입구 화분 {n}개");
    }

    // ------------------------------------------------------------------ 소품(D-139 앞마당·인도 가구)
    // 리지드바디 결정(가설): 손으로 들 만한 것(자전거·화분·상자·통·벤치, 8~40kg)은 가벼운 리지드바디 — 차가 밀면 밀린다.
    // 무거운 것·땅에 박힌 것(자판기·가로등·전봇대·나무)·벽 부착물은 정적. 시작 때 잠든 상태로 둔다(물리 비용·떨림 방지).
    static readonly Dictionary<string, float> PropMass = new Dictionary<string, float>
    {
        { "bicycle", 15f }, { "Pots", 8f }, { "Crate", 10f }, { "CrateStack", 30f }, { "Barrel", 20f }, { "Bench", 40f }, { "Bin", 12f }, { "TireStack", 25f },
    };
    static readonly HashSet<string> WallProps = new HashSet<string> { "small_signs_a", "small_signs_c", "mailbox", "ac_unit", "AcUnit", "WallLamp", "SignTea", "SignIce", "SignRest" };
    static readonly Dictionary<string, GameObject> propCache = new Dictionary<string, GameObject>();
    static int propN, propRb;

    static GameObject PropPf(string id)
    {
        if (propCache.TryGetValue(id, out var g)) return g;
        g = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/BayLoop/Props/" + id + ".prefab") ?? AssetDatabase.LoadAssetAtPath<GameObject>(Lofi + id + ".prefab");
        propCache[id] = g; return g;
    }

    /// <summary>소품 하나. 바닥 소품 = 렌더 바닥 가운데를 pos 에, 벽 소품 = 뒷면(−Z)을 pos 에(벽면에 붙임). +Z = rot 앞.</summary>
    static GameObject Prop(Transform parent, string id, Vector3 pos, Quaternion rot)
    {
        var pf = PropPf(id); if (pf == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent); go.name = "PROP_" + id;
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { Object.DestroyImmediate(go); return null; }
        var b = Bounds(go); bool wall = WallProps.Contains(id);
        var off = new Vector3(-b.center.x, -b.min.y, wall ? -b.min.z : -b.center.z);
        go.transform.SetPositionAndRotation(pos + rot * off, rot);
        if (!wall && PropMass.TryGetValue(id, out var mass))
        {
            if (go.GetComponentInChildren<Collider>() == null) { var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size; }
            var rb = go.AddComponent<Rigidbody>(); rb.mass = mass; rb.linearDamping = 0.3f; rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.None; rb.sleepThreshold = 0.05f;
            propRb++;
        }
        propN++;
        return go;
    }

    static string Furniture(Transform parent)
    {
        var rnd = new System.Random(2139); int bench = 0, planter = 0, stall = 0;
        foreach (var r in roads.Where(q => q.Car && q.xs != null))
        {
            for (int i = 6; i < r.pts.Count - 6; i += 1)
            {
                foreach (int side in new[] { 1, -1 })
                {
                    var xs = r.xs[i]; if (xs == null) continue; float wk = r.Walk(i, side); if (wk < 1.8f) continue;
                    var furn = xs.TryGetValue("furniture", out var fu) && fu is List<object> fl ? fl.Cast<string>().ToList() : new List<string>();
                    string id = null;
                    if (furn.Contains("Bench") && (i + (side > 0 ? 0 : 17)) % 37 == 0) id = "Bench";
                    else if (furn.Contains("Planter") && (i + (side > 0 ? 0 : 11)) % 29 == 0) id = rnd.NextDouble() < 0.5 ? "planter_row" : "Pots";
                    if (id == null) continue;
                    var f = Fwd(r.pts, i, r.closed); var nrm = -side * new Vector3(f.z, 0, -f.x);
                    var pos = r.pts[i] + nrm * (r.LaneHalf(i) + wk - 0.55f);
                    if (NearCar(pos, 0.6f) || NearBuilding(pos, 0.4f)) continue;
                    pos.y = BandTop(pos);
                    Prop(parent, id, pos, Quaternion.LookRotation(-nrm));
                    if (id == "Bench") bench++; else planter++;
                }
            }
        }
        // 공유 광장(D-139): 노점 2·벤치 2·화분 — 차·손수레·사람이 같이 쓰는 돌포장
        foreach (var (g, poly) in OG().Where(o => (string)o.g["kind"] == "plaza"))
        {
            var c = new Vector2(poly.Average(p => p.x), poly.Average(p => p.y));
            var spots = new[] { (new Vector2(-8f, 2f), "Stall"), (new Vector2(6f, 3f), "Stall"), (new Vector2(-2f, 4f), "Bench"), (new Vector2(10f, -2f), "Bench"), (new Vector2(0f, -4f), "Pots") };
            foreach (var (o, id) in spots)
            {
                var v = c + o; if (!InP(poly, v.x, v.y) || NearCar(new Vector3(v.x, 0, v.y), 0.8f)) continue;
                var pos = new Vector3(v.x, H(v.x, v.y) + 0.02f, v.y);
                var go = Prop(parent, id, pos, Quaternion.Euler(0, rnd.Next(4) * 90f, 0));
                if (go != null && id == "Stall") stall++; else if (id == "Bench") bench++; else planter++;
            }
        }
        return $"인도 가구 벤치 {bench}·화분 {planter}·광장 노점 {stall}";
    }

    /// <summary>인도 띠 윗면 높이(아래로 쏴서 Band_ 를 찾고, 없으면 지형).</summary>
    static float BandTop(Vector3 pos)
    {
        Physics.SyncTransforms();
        foreach (var h in Physics.RaycastAll(new Vector3(pos.x, pos.y + 5f, pos.z), Vector3.down, 12f).OrderBy(h => h.distance))
            if (h.collider.name.StartsWith("Band_") || h.collider.name.StartsWith("Road_") || h.collider.name.StartsWith("OG_")) return h.point.y;
        return H(pos.x, pos.z);
    }

    static readonly List<Bounds> bldBounds = new List<Bounds>();
    static bool NearBuilding(Vector3 q, float m)
    {
        foreach (var b in bldBounds) { var e = b; e.Expand(m * 2f); if (q.x > e.min.x && q.x < e.max.x && q.z > e.min.z && q.z < e.max.z) return true; }
        return false;
    }

    static string Trees(Transform parent)
    {
        var tree = Lofi + "CoastalTree.prefab"; var bush = Lofi + "Shrub.prefab";
        var rnd = new System.Random(140); int nt = 0, nb = 0, skip = 0;
        if (!J.TryGetValue("trees", out var to)) return "나무 0(json 없음)";
        foreach (List<object> t in (List<object>)((Dictionary<string, object>)to)["points"])
        {
            float x = F(t[0]), z = F(t[1]); bool isTree = (string)t[2] == "Tree";
            var q = new Vector3(x, 0, z);
            if (NearCar(q, 1.2f) || NearBuilding(q, 0.8f)) { skip++; continue; }
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(isTree ? tree : bush); if (pf == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent); go.name = (isTree ? "TREE_" : "BUSH_") + nt;
            float sc = 0.85f + (float)rnd.NextDouble() * 0.4f;
            go.transform.SetPositionAndRotation(new Vector3(x, H(x, z) - 0.05f, z), Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0)); go.transform.localScale = Vector3.one * sc;
            if (isTree) nt++; else nb++;
        }
        return $"나무 {nt}·덤불 {nb}(길·건물 겹침 뺌 {skip})";
    }

    // ------------------------------------------------------------------ 건물
    static readonly Dictionary<string, string[]> Fallback = new Dictionary<string, string[]>
    {
        { "harbor_warehouse", new[] { "RepairGarage", "RepairGarageB" } },
        { "shop_house", new[] { "CornerShop", "CornerShopB", "CornerShopC" } },
        { "town_house", new[] { "CoastalHouse", "CoastalHouseB", "CoastalHouseC", "CoastalHouseD", "CoastalHouseE", "CoastalHouseF" } },
        { "terrace_house", new[] { "CoastalHouse", "CoastalHouseB", "CoastalHouseC", "CoastalHouseD", "CoastalHouseE", "CoastalHouseF" } },
        { "cliff_villa", new[] { "CoastalHouseE", "CoastalHouseF" } },
        { "farmhouse", new[] { "RepairGarage", "RepairGarageB" } },
    };
    static readonly Dictionary<string, Color[]> Tints = new Dictionary<string, Color[]>
    {
        { "harbor_warehouse", new[] { new Color(.82f, .86f, .90f), new Color(.92f, .84f, .76f), new Color(.78f, .84f, .80f) } },
        { "shop_house", new[] { new Color(1f, .93f, .84f), new Color(.92f, .86f, .78f), new Color(1f, 1f, 1f) } },
        { "town_house", new[] { new Color(1f, 1f, 1f), new Color(1f, .95f, .88f), new Color(.95f, .92f, .86f) } },
        { "terrace_house", new[] { new Color(1f, .88f, .80f), new Color(.98f, .95f, .78f), new Color(.86f, .92f, 1f), new Color(1f, .86f, .88f), new Color(1f, 1f, 1f) } },
        { "cliff_villa", new[] { new Color(1f, 1f, 1f), new Color(.96f, .98f, 1f) } },
        { "farmhouse", new[] { new Color(.86f, .80f, .70f), new Color(.80f, .76f, .68f) } },
    };

    /// <summary>building-types.md 의 연결표: 구간·앞 길 종류로 type.</summary>
    public static string TypeOf(string sec, string road, float x, float z)
    {
        string k = road.StartsWith("SC_") ? "SC" : road.StartsWith("V_") ? "V" : road.StartsWith("A_") || road.StartsWith("S_") ? "A" : road;
        if (sec == "1" && z < -122f && x < 70f && (road == "main" || road == "V_pier")) return "harbor_warehouse";
        switch (sec)
        {
            case "1": return road == "main" || road == "V_kyoto_back" ? "shop_house" : "town_house";
            case "2": return road == "V_detour" || road == "V_farmyard" || road == "A_field" || road == "A_plateau" ? "farmhouse" : "town_house";
            case "3": return "terrace_house";
            default:
                if (road == "main" || road == "A_cliffwalk") return "cliff_villa";
                if (road == "A_terrace") return "terrace_house";
                return "town_house";
        }
    }

    static string Buildings(Transform parent)
    {
        var rnd = new System.Random(1645);
        var parcels = ((List<object>)J["parcels"]).Cast<Dictionary<string, object>>().ToList();
        var byRoad = roads.ToDictionary(r => r.id);
        var martP = (List<object>)Get(J, "mart", "pos"); var mart = new Vector2(F(martP[0]), F(martP[1]));
        // 앞 길 위 위치(s)로 정렬 → 같은 길·같은 쪽에서 같은 프리팹 3채 연속 금지
        var rows = new List<(Dictionary<string, object> p, string road, int side, float s)>();
        foreach (var p in parcels)
        {
            var fr = (Dictionary<string, object>)p["front"]; string road = (string)fr["road"]; int side = System.Convert.ToInt32(fr["side"]);
            var fm = (List<object>)p["frontMid"]; float s = byRoad.TryGetValue(road, out var rr) ? Project(rr, F(fm[0]), F(fm[1])) : 0f;
            rows.Add((p, road, side, s));
        }
        rows = rows.OrderBy(r => r.road).ThenBy(r => r.side).ThenBy(r => r.s).ToList();
        var typeCount = new Dictionary<string, int>(); var usedPrefab = new Dictionary<string, int>(); var fallbackTypes = new HashSet<string>();
        int n = 0, plinths = 0, skipped = 0, overlap = 0, pushed = 0, artOver = 0, viol = 0, fbFor30 = 0, blockN = 0; var artW = new List<float>();
        var placed = new List<(string path, Vector3 pos)>(); bldBounds.Clear(); propN = propRb = 0; propsG = null;
        var dists = ((List<object>)J["districts"]).Cast<Dictionary<string, object>>().ToDictionary(dd => (string)dd["id"]);
        var facingN = new Dictionary<string, int>();
        // v5 (D-141 ②③): 줄(같은 길·같은 쪽)마다 후퇴·높이·틈 상태
        var rowSt = new Dictionary<string, RowSt>(); var rnd5 = new System.Random(2202); bool skipNext = false; int whMerged = 0, natDressed = 0; string whDbg = ""; int harborRows = rows.Count(r0 => r0.p.TryGetValue("district", out var dd0) && (dd0 as string) == "D_harbor");
        bool Seaward(Dictionary<string, object> q)
        {
            float qx = F(q["x"]), qz = F(q["z"]), qd = F(q["d"]); var qf = (List<object>)q["frontMid"];
            var qdir = new Vector2(F(qf[0]) - qx, F(qf[1]) - qz).normalized; var pr = new Vector2(qx, qz) - qdir * (qd * .5f + 8f);
            return H(pr.x, pr.y) < 1.5f;
        }
        for (int ri = 0; ri < rows.Count; ri++)
        {
            var row = rows[ri];
            if (skipNext) { skipNext = false; continue; }
            var p = row.p;
            float x = F(p["x"]), z = F(p["z"]), w = F(p["w"]), d = F(p["d"]); int floors = System.Convert.ToInt32(p["floors"]);
            string facing = p.TryGetValue("facing", out var fc0) && fc0 is string fs ? fs : "front";
            if (new Vector2(x - mart.x, z - mart.y - 10f).magnitude < 9f) { skipped++; continue; }   // MART 자리
            var fm = (List<object>)p["frontMid"]; var front = new Vector2(F(fm[0]), F(fm[1]));
            var dir = new Vector3(front.x - x, 0, front.y - z).normalized; if (dir.sqrMagnitude < 0.5f) dir = Vector3.forward;
            var rot = Quaternion.LookRotation(dir);
            var dist = p.TryGetValue("district", out var di) && di is string ds && dists.ContainsKey(ds) ? dists[ds] : dists.Values.Last();
            string distId = (string)dist["id"]; string densS = (string)dist["density"]; int dens = densS == "dense" ? 2 : densS == "medium" ? 1 : 0;
            float fsp = p.TryGetValue("frontSpace", out var fso) ? F(fso) : 0f;
            // ③ 골목 틈: 같은 줄에서 앞 필지와 2.5m 넘게 떨어졌으면 이미 틈. 밀집·보통 차길에서 30~60m 동안 틈이 없으면 이 필지를 비워 골목 입구로.
            string rk = row.road + "|" + row.side;
            if (!rowSt.TryGetValue(rk, out var st)) rowSt[rk] = st = new RowSt { lastGap = row.s, next = 30f + (float)rnd5.NextDouble() * 30f };
            var edgeNow = new Vector3(front.x, 0, front.y) + dir * fsp;
            float gapLen = row.s - w * .5f - st.prevEnd;
            if (gapLen >= 2.5f && st.prevEnd > -1e8f)
            {
                st.lastGap = row.s; st.next = 30f + (float)rnd5.NextDouble() * 30f; natGaps++;
                // 자연 틈도 비어 있으면 골목으로 안 읽힌다(v5 1차: 117개가 빈 풀밭) → 밀집·보통 차길이면 입구를 꾸민다
                if (dens >= 1 && byRoad[row.road].Car && gapLen <= 9f)
                {
                    float L = Vector3.Distance(st.prevEdge, edgeNow);
                    if (L > 0.5f && L < 30f)
                    {
                        var ge = Vector3.Lerp(st.prevEdge, edgeNow, Mathf.Clamp01((st.prevW * .5f + gapLen * .5f) / L)); ge.y = H(ge.x, ge.z);
                        gaps.Add(new Gap { edge = ge, dir = dir, w = gapLen, d = d, dens = dens, road = row.road }); natDressed++;
                    }
                }
            }
            st.prevEnd = row.s + w * .5f; st.prevEdge = edgeNow; st.prevW = w;
            if (dens >= 1 && byRoad[row.road].Car && row.s - st.lastGap >= st.next && w >= 2.6f && w <= 5f)   // 넓은 필지를 통째로 비우면 이 빠진 공터(1차 04 컷) — 좁은 필지만
            {
                var edge0 = new Vector3(front.x, 0, front.y) + dir * fsp; edge0.y = H(edge0.x, edge0.z);
                gaps.Add(new Gap { edge = edge0, dir = dir, w = w, d = d, dens = dens, road = row.road });
                st.lastGap = row.s; st.next = 30f + (float)rnd5.NextDouble() * 30f; cutGaps++; st.lastH = -1f; continue;
            }
            // 항구 앞 바다 쪽(뒤 8m 가 물): 옆 필지와 합쳐 창고(아트 폭 9~11m — 필지 하나로는 안 들어간다, v4 창고 0채의 원인)
            bool wh = false;
            if (distId == "D_harbor" && (row.road == "main" || row.road == "V_pier") && whMerged < 4 && ri + 1 < rows.Count)
            {
                var nx = rows[ri + 1];
                float nw = F(nx.p["w"]), span = (nx.s + nw * .5f) - (row.s - w * .5f);
                whDbg += $"{row.road}{row.side}@{row.s:F0}:{span:F1} ";
                if (nx.road == row.road && nx.side == row.side && span >= 9.3f && span <= 13.5f && rnd5.NextDouble() < 0.6)
                {
                    var nfm = (List<object>)nx.p["frontMid"];
                    x = (x + F(nx.p["x"])) * .5f; z = (z + F(nx.p["z"])) * .5f; front = (front + new Vector2(F(nfm[0]), F(nfm[1]))) * .5f;
                    w = span; d = Mathf.Max(d, F(nx.p["d"])); facing = "front"; wh = true; skipNext = true; whMerged++;
                    st.prevEnd = nx.s + nw * .5f;
                }
            }
            // ② 후퇴: 인도 바깥선(front + dir·frontSpace)에서 밀집 0~0.5m, 보통 0~3m. 앞면이 길을 볼 때만.
            if (facing == "front" && dens >= 1 && !wh)
            {
                float sb = PickSetback(st, dens, rnd5);
                float shift = Mathf.Max(fsp - sb, -Mathf.Min(1.5f, d * 0.2f));
                // 당긴 자리 네 모서리가 (다른) 차길에 닿으면 당기지 않는다 — 1차 빌드 P194 가 옆 옛길을 물었다
                var latS = new Vector3(dir.z, 0, -dir.x); var cS = new Vector3(x, 0, z) + dir * shift;
                foreach (var cq in new[] { cS + dir * d * .5f + latS * w * .5f, cS + dir * d * .5f - latS * w * .5f, cS - dir * d * .5f + latS * w * .5f, cS - dir * d * .5f - latS * w * .5f })
                    if (NearCar(cq, 0.3f)) { shift = Mathf.Min(shift, 0f); break; }
                x += dir.x * shift; z += dir.z * shift; front += new Vector2(dir.x, dir.z) * shift; fsp -= shift;
            }
            // 필지가 차길 띠를 덮으면 길 반대쪽으로 0.5m 씩 최대 3m 물린다. 그래도 덮으면(다른 길이 관통) 비운다.
            int push = 0;
            while (push <= 6 && OnCarRoad(new Vector3(x, 0, z) - dir * (push * .5f), rot, w, d)) push++;
            if (push > 6) { overlap++; continue; }
            if (push > 0) { pushed++; x -= dir.x * push * .5f; z -= dir.z * push * .5f; front -= new Vector2(dir.x, dir.z) * (push * .5f); fsp += push * .5f; }
            // D-140: 구역 type 가중치 + 30m 변형 규칙(PickBuilding). v5: 같은 줄 앞집과 높이가 같은(±2.6m) 변형은 가중 ×0.35.
            var wts = wh ? new Dictionary<string, float> { { "harbor_warehouse", 1f } } : ((Dictionary<string, object>)dist["types"]).ToDictionary(kv => kv.Key, kv => F(kv.Value));
            if (!wh) wts.Remove("harbor_warehouse");   // 창고는 합친 바다 쪽 필지에만
            float fitW = facing == "side" ? d : w;
            float prevH = st.lastH;
            var (type, pname, isArt, how) = PickBuilding(wts, fitW, new Vector3(x, 0, z), rnd, placed, prevH > 0f ? (System.Func<string, float>)(pth => Mathf.Abs(PfH(pth) - prevH) < 2.6f ? 0.35f : 1f) : null);
            if (prevH > 0f && pname != null) { hPairs++; if (Mathf.Abs(PfH(pname) - prevH) >= 2.6f) hPairsDiff++; }
            st.lastH = pname != null ? PfH(pname) : -1f;
            if (how == 2) fbFor30++; else if (how == 3) viol++;
            typeCount[type] = typeCount.TryGetValue(type, out var tc) ? tc + 1 : 1;
            facingN[facing] = facingN.TryGetValue(facing, out var fn) ? fn + 1 : 1;
            // 앞면 방향(D-138 facing): front = 길 / back = 반대(뒤가 열린 땅·다른 길) / side = 옆(필지 짧은 쪽이 길)
            var frot = facing == "back" ? rot * Quaternion.Euler(0, 180f, 0) : facing == "side" ? rot * Quaternion.Euler(0, (int.Parse(((string)p["id"]).Substring(1)) & 1) == 0 ? 90f : -90f, 0) : rot;
            // ② 방향 흔듦: 밀집 ±1.5°, 보통 ±4°(나란한 줄 깨기). 성김은 그대로.
            if (dens >= 1 && !wh) { frot *= Quaternion.Euler(0, ((float)rnd5.NextDouble() * 2f - 1f) * (dens == 2 ? 1.5f : 4f), 0); yawJit++; }
            var fdir = frot * Vector3.forward;
            float fdepth = facing == "side" ? w : d;
            float floorY = H(front.x, front.y) + 0.05f;
            // 앞마당이 앞벽보다 나온 변형은 빌더가 fo 만큼 뒤로 물린다(아래 face) → 기단도 물린 만큼 뒤로 늘려 몸체 밑을 덮는다
            float fo = pname != null && ImportBayLoopBuildings.FrontOut.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(pname), out var fo0) ? fo0 : 0f;
            { var bk = new Vector3(x, 0, z) - fdir * fo; if (fo > 0f && H(bk.x, bk.z) - (H(front.x, front.y) + 0.05f) > 2f) fo = 0f; }   // 뒤가 비탈이면 물리지 않음(언덕 속에 묻힘 — P150)
            var pc = new Vector3(x, 0, z) - fdir * (fo * .5f); var pd = facing == "side" ? new Vector2(w + fo, d) : new Vector2(w, d + fo);
            float lo = float.MaxValue;
            for (int sx = -2; sx <= 2; sx++) for (int sz = -2; sz <= 2; sz++)
                { var q = pc + rot * new Vector3(sx * .25f * pd.x, 0, sz * .25f * pd.y); lo = Mathf.Min(lo, H(q.x, q.z)); }
            var center = new Vector3(x, floorY, z);
            GameObject plinth = null;
            if (floorY - lo > 0.25f)
            {
                plinth = Box(parent, "Plinth_" + p["id"], new Vector3(pd.x + 0.4f, floorY - lo + 0.35f, pd.y + 0.4f), new Vector3(pc.x, (floorY + lo - 0.35f) * .5f, pc.z), rot, M("Concrete"), true);
                plinths++;
            }
            if (type == "fish_market") { FishMarket(parent, center, rot, w, d, "BLD_" + p["id"] + "_fish_market"); n++; continue; }
            if (!isArt) fallbackTypes.Add(type);
            if (isArt) { artW.Add(fitW); if (FpW(pname) > fitW + 0.3f) artOver++; }
            GameObject go;
            float perFloor = type == "terrace_house" ? 3.0f : type == "harbor_warehouse" || type == "farmhouse" ? 3.6f : 3.2f;
            if (pname == null)
            {
                // 블록아웃(building-types.md 폴백 '블록아웃' — 성당·학교·주유소 등 새 type 이 아직 프리팹 없이 들어올 때)
                go = new GameObject("BLD_" + p["id"] + "_" + type + "_BLOCK"); go.transform.SetParent(parent); go.transform.SetPositionAndRotation(center, frot);
                float hb = floors * perFloor;
                Box(go.transform, "Mass", new Vector3(facing == "side" ? d : w, hb, fdepth), center + Vector3.up * (hb * .5f), frot, M("White"), true);
                blockN++;
            }
            else
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(pname);
                go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                go.name = "BLD_" + p["id"] + "_" + type + (isArt ? "" : "_FB");
                string pn = System.IO.Path.GetFileNameWithoutExtension(pname);
                usedPrefab[pn] = usedPrefab.TryGetValue(pn, out var uc) ? uc + 1 : 1;
                if (isArt)
                {
                    // 피벗 = 바닥 앞면 가운데 → 앞면이 보는 쪽 필지 가장자리에
                    var face = center + fdir * (fdepth * .5f); if (facing == "front") face = new Vector3(front.x, floorY, front.y);
                    // D-140 변형은 앞마당·담장이 앞벽보다 길 쪽으로 나온다(manifest bounds_max.z) → 그만큼 물려 마당까지 필지 안에
                    face -= fdir * fo;
                    go.transform.SetPositionAndRotation(face, frot);
                }
                else
                {
                    go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); go.transform.localScale = Vector3.one;
                    var b = Bounds(go);
                    float hT = floors * perFloor + (float)rnd.NextDouble() * 0.8f;
                    var sc = new Vector3((facing == "side" ? d : w) / b.size.x, hT / b.size.y, fdepth / b.size.z);
                    go.transform.localScale = sc;
                    go.transform.SetPositionAndRotation(center - Vector3.up * (b.min.y * sc.y) - frot * new Vector3(b.center.x * sc.x, 0, b.center.z * sc.z), frot);
                }
            }
            placed.Add((pname ?? type, new Vector3(x, 0, z)));
            // 실제 콜라이더로 다시 본다(프리팹 콜라이더가 렌더 크기보다 크거나 굽이 안쪽 모서리가 차로를 무는 경우 — P011 실주행에서 걸림).
            int back = 0;
            while (back <= 6 && Intrudes(go, plinth)) { back++; go.transform.position -= dir * .5f; if (plinth) plinth.transform.position -= dir * .5f; }
            if (back > 6) { Object.DestroyImmediate(go); if (plinth) { Object.DestroyImmediate(plinth); plinths--; } overlap++; typeCount[type]--; placed.RemoveAt(placed.Count - 1); continue; }
            if (back > 0) pushed++;
            var tints = Tints.TryGetValue(type, out var tl) ? tl : new[] { Color.white, new Color(1f, .95f, .88f) };
            var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Tint", tints[rnd.Next(tints.Length)]);
            if (!isArt) foreach (var rr in go.GetComponentsInChildren<Renderer>()) rr.SetPropertyBlock(mpb);
            bldBounds.Add(Bounds(go));
            // 앞마당 소품(D-139): 길 쪽 앞 공간에 바닥 소품, 앞면이 길을 볼 때만 벽 소품
            var front2 = new Vector2(front.x, front.y) - new Vector2(dir.x, dir.z) * (back * .5f); fsp += back * .5f;
            FrontProps(propsG ??= Group("FrontProps"), type, go.transform.position, dir, rot, front2, facing == "front" ? w : 0f, w, fsp, rnd);
            // v5 생활 층 자리 기록(LivedIn 이 쓴다): 앞마당 주차 자리 · 가게 앞 · 성긴 집 마당
            var f3 = new Vector3(front2.x, H(front2.x, front2.y), front2.y);
            if (facing == "front" && fsp >= 2.2f && dens == 1) frontSlots.Add(new Slot { edge = f3 + dir * fsp, dir = dir, space = fsp, w = w });
            if (facing == "front" && type == "shop_house" && isArt) shopFronts.Add(new ShopF { face = go.transform.position, dir = dir, w = w });
            if (dens == 0 && type != "fish_market")
                sparseYards.Add(new Yard { c = new Vector3(x, floorY, z) - dir * (back * .5f), face = go.transform.position + fdir * fo, fdir = fdir, rot = rot, w = w, d = d, fo = fo, type = type, ownWall = fo > 0.5f });
            n++;
        }
        // fish_market: 항구 직선 바다 쪽 — 필지가 없으니 json landmark '어시장 지붕' 외에 위판장 2동을 MART 동쪽 부두에
        foreach (var fx in new[] { -6f, 8f })
        {
            var c = new Vector3(mart.x + fx, 0, mart.y - 12f); c.y = H(c.x, c.z) + 0.02f;
            FishMarket(parent, c, Quaternion.LookRotation(Vector3.forward), 10f, 6f, "BLD_fish_market_" + fx); typeCount["fish_market"] = typeCount.TryGetValue("fish_market", out var fc) ? fc + 1 : 1;
        }
        lastFallback = fallbackTypes.OrderBy(a => a).ToList();
        lastTypeCount = typeCount;
        // 30m 규칙 사후 검산: 각 건물 둘레 30m 안 같은 프리팹 수(자기 포함)의 최대
        int worst = 0; foreach (var a in placed) worst = Mathf.Max(worst, placed.Count(b => b.path == a.path && (b.pos - a.pos).sqrMagnitude < 900f));
        return $"건물 {n}채(MART 자리 비움 {skipped}, 차길 침범 물림 {pushed}·비움 {overlap}, 기단 {plinths}, 블록아웃 {blockN}) type " + string.Join(", ", typeCount.OrderBy(k => k.Key).Select(k => k.Key + " " + k.Value))
            + $" | 30m 규칙: 반경 30m 같은 프리팹 최대 {worst}채(자기 포함), 아트 소진→폴백 {fbFor30}, 위반 {viol}"
            + " | facing " + string.Join(", ", facingN.Select(k => k.Key + " " + k.Value))
            + " | 폴백 type: " + string.Join(", ", lastFallback)
            + (artW.Count > 0 ? $" | 아트 필지 폭 {artW.Min():F1}~{artW.Max():F1}m, 필지보다 넓은 변형 {artOver}" : "") + " | 프리팹 " + string.Join(", ", usedPrefab.OrderBy(k => k.Key).Select(k => k.Key + " " + k.Value))
            + $" | 소품 {propN}(리지드바디 {propRb}) | v5 창고 합친 필지 {whMerged} (항구 필지 {harborRows}, 후보 폭: {whDbg}) | 꾸민 자연 틈 {natDressed}";
    }

    // ------------------------------------------------------------------ type → 프리팹 묶음(building-types.md 를 읽어 폴백을 매핑 — type 이 늘어도 표 한 줄이면 된다)
    static Dictionary<string, string[]> artPools, fbPools;
    static Transform propsG;
    static string[] ArtPool(string t)
    {
        artPools ??= new Dictionary<string, string[]>();
        if (!artPools.TryGetValue(t, out var a))
            artPools[t] = a = AssetDatabase.FindAssets("t:Prefab " + t, new[] { BldDir }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => System.IO.Path.GetFileNameWithoutExtension(x).StartsWith(t + "_")).OrderBy(x => x).ToArray();
        return a;
    }
    static string[] FallbackPool(string t)
    {
        if (fbPools == null)
        {
            fbPools = new Dictionary<string, string[]>();
            var md = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/map/building-types.md"));
            if (System.IO.File.Exists(md))
                foreach (var line in System.IO.File.ReadAllLines(md, Encoding.UTF8))
                {
                    var c = line.Split('|'); if (c.Length < 7) continue;
                    string type = c[1].Trim(); if (type == "type" || type.StartsWith("-") || type.Contains("*")) continue;
                    fbPools[type] = ParseFallback(c[5]);
                }
            foreach (var kv in Fallback) if (!fbPools.ContainsKey(kv.Key)) fbPools[kv.Key] = kv.Value.Select(x => Lofi + x + ".prefab").ToArray();
        }
        return fbPools.TryGetValue(t, out var f) ? f : new string[0];
    }
    /// <summary>"CoastalHouse A~F (높이 늘림)" / "CoastalHouseE/F" / "CornerShop, B, C" / "RepairGarage, RepairGarageB" / "블록아웃" → LofiPilot 프리팹 경로들(없는 이름은 버림).</summary>
    static string[] ParseFallback(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\(.*?\)", "").Trim();
        var o = new List<string>();
        var rg = System.Text.RegularExpressions.Regex.Match(s, @"^([A-Za-z]+?)\s+([A-Z])~([A-Z])$");
        var sl = System.Text.RegularExpressions.Regex.Match(s, @"^([A-Za-z]+?)([A-Z])/([A-Z])$");
        if (rg.Success) for (char ch = rg.Groups[2].Value[0]; ch <= rg.Groups[3].Value[0]; ch++) o.Add(rg.Groups[1].Value + (ch == 'A' ? "" : ch.ToString()));
        else if (sl.Success) { o.Add(sl.Groups[1].Value + sl.Groups[2].Value); o.Add(sl.Groups[1].Value + sl.Groups[3].Value); }
        else { string bas = null; foreach (var tok in s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0)) { if (bas != null && tok.Length == 1 && char.IsUpper(tok[0])) o.Add(bas + tok); else { o.Add(tok); bas = tok; } } }
        return o.Select(x => Lofi + x + ".prefab").Where(x => AssetDatabase.LoadAssetAtPath<GameObject>(x) != null).ToArray();
    }
    static float FpW(string path) => path != null && ImportBayLoopBuildings.Footprint.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(path), out var v) ? v.x : 0f;

    /// <summary>
    /// D-140 변형 규칙: 구역 type 가중 무작위. 같은 프리팹이 반경 30m 안에 이미 2채면 그 프리팹은 후보에서 뺀다.
    /// 순서: ① 아트 프리팹(필지 폭 +0.3m 안에 드는 것) → ② LofiPilot 폴백(필지에 맞춰 늘림) → ③ 그래도 없으면 가장 덜 쓰인 것(위반으로 센다).
    /// how: 1 아트 / 2 아트가 30m 규칙에 막혀 폴백 / 3 위반 / 0 type 에 프리팹이 아예 없어 폴백·블록아웃.
    /// </summary>
    static (string type, string path, bool art, int how) PickBuilding(Dictionary<string, float> wts, float fitW, Vector3 pos, System.Random rnd, List<(string path, Vector3 pos)> placed, System.Func<string, float> wmul = null)
    {
        int Near(string pth) => placed.Count(q => q.path == pth && (q.pos - pos).sqrMagnitude < 900f);
        // 어느 건물을 중심으로 재도 30m 안 같은 프리팹 ≤2채(자기 포함): 새 자리 30m 안에 같은 것이 0채이거나, 1채인데 그 1채 둘레에 또 다른 같은 것이 없을 때만 허용
        int Ok(string pth)
        {
            var near = placed.Where(q => q.path == pth && (q.pos - pos).sqrMagnitude < 900f).ToList();
            if (near.Count == 0) return 0;
            if (near.Count > 1) return 2;
            var a = near[0].pos; return placed.Count(q => q.path == pth && (q.pos - a).sqrMagnitude < 900f) >= 2 ? 2 : 1;
        }
        bool anyArt = wts.Keys.Any(t => ArtPool(t).Length > 0);
        for (int stage = 0; stage < 2; stage++)
        {
            var cands = new List<(string t, string pth, float wgt)>();
            foreach (var kv in wts)
            {
                var pool = stage == 0 ? ArtPool(kv.Key).Where(x => FpW(x) <= fitW + 0.3f) : FallbackPool(kv.Key).AsEnumerable();
                var ok = pool.Where(x => Ok(x) < 2).ToList();
                foreach (var x in ok) cands.Add((kv.Key, x, kv.Value / ok.Count * (wmul != null ? wmul(x) : 1f)));
            }
            if (cands.Count == 0) continue;
            float u = (float)rnd.NextDouble() * cands.Sum(c => c.wgt);
            foreach (var c in cands) { u -= c.wgt; if (u <= 0f) return (c.t, c.pth, stage == 0, stage == 0 ? 1 : anyArt ? 2 : 0); }
            var l = cands[cands.Count - 1]; return (l.t, l.pth, stage == 0, stage == 0 ? 1 : anyArt ? 2 : 0);
        }
        // 다 막힘: 가중 1위 type 에서 가장 덜 쓰인 변형(아트 우선), 프리팹이 없는 type 이면 블록아웃(path null)
        string top = wts.OrderByDescending(k => k.Value).First().Key;
        var all = ArtPool(top).Concat(FallbackPool(top)).ToList();
        if (all.Count == 0) return (top, null, false, 0);
        var best = all.OrderBy(Ok).ThenBy(Near).First();
        return (top, best, ArtPool(top).Contains(best), 3);
    }

    /// <summary>앞마당 소품: 앞 공간(frontSpace) 0.8m 이상이면 바닥 소품 1~2, 벽 소품은 앞면이 길을 볼 때만(facadeW&gt;0).</summary>
    static void FrontProps(Transform parent, string type, Vector3 bpos, Vector3 dir, Quaternion rot, Vector2 front, float facadeW, float w, float fsp, System.Random rnd)
    {
        string[] floor, wall;
        switch (type)
        {
            case "shop_house": floor = new[] { "vending_machine", "planter_row", "Pots", "Bench" }; wall = new[] { "small_signs_a", "small_signs_c", "AcUnit" }; break;
            case "harbor_warehouse": floor = new[] { "Crate", "CrateStack", "Barrel", "TireStack" }; wall = new[] { "WallLamp" }; break;
            case "farmhouse": floor = new[] { "Crate", "Barrel", "Pots" }; wall = new[] { "WallLamp" }; break;
            default: floor = new[] { "bicycle", "planter_row", "Pots", "Bin" }; wall = new[] { "mailbox", "ac_unit" }; break;
        }
        var lat = new Vector3(dir.z, 0, -dir.x); float y = H(front.x, front.y);
        var f3 = new Vector3(front.x, y, front.y);
        if (fsp >= 0.8f)
        {
            int k = fsp >= 2f && w >= 6f ? 2 : 1;
            for (int i = 0; i < k; i++)
            {
                string id = floor[rnd.Next(floor.Length)];
                float side = (i == 0 ? -1f : 1f) * w * (0.22f + (float)rnd.NextDouble() * 0.1f);
                var pos = f3 + dir * Mathf.Min(fsp * .5f, 0.9f) + lat * side;
                if (NearCar(pos, 0.5f)) continue;
                pos.y = H(pos.x, pos.z) + 0.02f;
                Prop(parent, id, pos, id == "Bench" || id == "vending_machine" ? rot : Quaternion.LookRotation(lat) * Quaternion.Euler(0, rnd.Next(2) * 180f, 0));
            }
        }
        if (facadeW > 0f && rnd.NextDouble() < 0.6)
        {
            string id = wall[rnd.Next(wall.Length)];
            float hgt = id == "mailbox" ? 1.1f : id.StartsWith("small_signs") ? 2.9f : id == "WallLamp" ? 2.8f : 2.4f;
            var pos = f3 + Vector3.up * (H(front.x, front.y) - y + 0.05f + hgt) + lat * (facadeW * (rnd.NextDouble() < 0.5 ? -0.3f : 0.3f));
            Prop(parent, id, pos, rot);
        }
    }
    public static List<string> lastFallback = new List<string>();
    public static Dictionary<string, int> lastTypeCount = new Dictionary<string, int>();

    static void FishMarket(Transform parent, Vector3 c, Quaternion rot, float w, float d, string name)
    {
        var g = new GameObject(name).transform; g.SetParent(parent); g.SetPositionAndRotation(c, rot);
        foreach (var sx in new[] { -.5f, 0f, .5f }) foreach (var sz in new[] { -.5f, .5f })
            Box(g, "Post", new Vector3(0.25f, 6.2f, 0.25f), c + rot * new Vector3(sx * (w - 0.6f), 0.1f, sz * (d - 0.6f)), rot, M("Wood"), true);   // 3m 묻힘 — 비탈·부두 끝에서 안 뜨게
        Box(g, "Roof", new Vector3(w, 0.25f, d + 0.6f), c + rot * new Vector3(0, 3.3f, 0), rot * Quaternion.Euler(4f, 0, 0), M("TinRoof"), false);
        for (int i = 0; i < 3; i++) Box(g, "Crates", new Vector3(1.2f, 0.5f, 0.8f), c + rot * new Vector3(-w * .3f + i * w * .3f, 0.25f, 0), rot, M("Fish"), false);
    }

    static List<(Vector3 p, float half)> carPts;
    static bool NearCar(Vector3 q, float margin)
    {
        foreach (var (p, half) in carPts) { float dx = p.x - q.x, dz = p.z - q.z, r = half + margin; if (dx * dx + dz * dz < r * r) return true; }
        return false;
    }
    /// <summary>필지 바닥(4x4 표본)이 어느 차길 띠(반폭+어깨+0.3m) 안에 들어가면 true.</summary>
    static bool OnCarRoad(Vector3 c, Quaternion rot, float w, float d)
    {
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                var q = c + rot * new Vector3((i / 3f - .5f) * w * .85f, 0, (j / 3f - .5f) * d * .85f);   // 가장자리 15% 는 봐준다(처마가 길 어깨에 걸치는 정도)
                foreach (var (p, half) in clearPts)   // v4: 차로 + 좁은 쪽 인도까지 비운다(D-139 사람의 층)
                {
                    float dx = p.x - q.x, dz = p.z - q.z;
                    if (dx * dx + dz * dz < half * half) return true;
                }
            }
        return false;
    }

    /// <summary>건물·기단 콜라이더가 차로(차선 반폭 + 어깨 − 0.2m) 안으로 들어오면 true. 노면 1m 위 점에서 ClosestPoint.</summary>
    static bool Intrudes(GameObject go, GameObject plinth)
    {
        Physics.SyncTransforms();
        var cols = go.GetComponentsInChildren<Collider>().ToList(); if (plinth) cols.Add(plinth.GetComponent<Collider>());
        foreach (var col in cols)
        {
            var b = col.bounds; float reach = new Vector2(b.extents.x, b.extents.z).magnitude + 5f;
            bool exact = !(col is MeshCollider mc) || mc.convex;
            foreach (var (p, half) in carPts)
            {
                if (Mathf.Abs(p.x - b.center.x) > reach || Mathf.Abs(p.z - b.center.z) > reach) continue;
                // 길보다 높이 선 건물(기단 위·깎인 길 옆)도 위에서 보면 차로를 덮는다 — 아래로 벗어난 경우는 높이를 상자 안으로 끌어와 본다(v5b P194)
                if (p.y > b.max.y) continue;
                var q = new Vector3(p.x, Mathf.Clamp(p.y + 1f, b.min.y + 0.05f, b.max.y), p.z);
                var cp = exact ? col.ClosestPoint(q) : b.ClosestPoint(q);
                if (new Vector2(cp.x - p.x, cp.z - p.z).magnitude < half - 0.5f) return true;
                // 검사(Check) 레이와 같은 기준: 콜라이더 상자(AABB)가 차로 반폭−0.6 안을 덮으면 침범(v5b P194 — ClosestPoint 는 통과했는데 검사 레이에 걸렸다)
                var ab = b.ClosestPoint(new Vector3(p.x, b.center.y, p.z));
                if (new Vector2(ab.x - p.x, ab.z - p.z).magnitude < half - 0.9f && b.max.y > p.y + 0.3f) return true;
            }
        }
        return false;
    }

    static Bounds Bounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    // ------------------------------------------------------------------ features
    static string Features(Transform parent)
    {
        var parcels = ((List<object>)J["parcels"]).Cast<Dictionary<string, object>>().Select(p => new Vector2(F(p["x"]), F(p["z"]))).ToList();
        int nL = 0; var names = new List<string>();
        foreach (var r in roads)
            foreach (var f in r.features)
            {
                string type = (string)f["type"], name = f.TryGetValue("name", out var nm) ? (string)nm : type;
                float s = F(f["s"]) * r.len / PlanLen(r);   // json s 는 평면도 폴리라인 길이 기준 — 스플라인 길이로 비례
                if (r.closed) s = Mathf.Repeat(s, r.len);
                At(r, s, out var c, out var fw, out var rt);
                var rot = Quaternion.LookRotation(Flat(fw));
                if (type == "jump")
                {
                    float step = F(f["step"]);
                    Jump(parent, r, s, step, name);
                    names.Add($"점프 {r.id} s{s:F0} {step}m");
                }
                else if (type == "gap")
                {
                    float gw = F(f["width"]), gl = F(f["len"]);
                    foreach (var sd in new[] { -1f, 1f })
                        for (float q = -gl * .5f; q < gl * .5f; q += 2f)
                        {
                            At(r, s + q + 1f, out var cc, out var ff, out var rr);
                            var pp = cc + rr * sd * (gw * .5f + 0.35f);
                            float g = Mathf.Min(H(pp.x, pp.z), cc.y);
                            Box(parent, "INTENT_GapWall_" + r.id, new Vector3(0.7f, cc.y - g + 1.2f, 2.05f), new Vector3(pp.x, (cc.y + 1.2f + g) * .5f, pp.z), Quaternion.LookRotation(Flat(ff)), M("Stone"), true);
                        }
                    names.Add($"좁은 틈 {r.id} s{s:F0} 폭{gw} 길이{gl}");
                }
                else if (type == "landmark")
                {
                    // 길 양쪽 중 필지에서 더 먼 쪽(등대·크레인·어시장은 더 낮은 쪽 = 바다 쪽)
                    float off = r.Half + 4.5f;
                    var a = c + rt * off; var b = c - rt * off;
                    bool sea = name.Contains("등대") || name.Contains("크레인") || name.Contains("어시장");
                    float Clear(Vector3 q) => parcels.Min(pp => (pp - new Vector2(q.x, q.z)).magnitude);
                    var pos = sea ? (H(a.x, a.z) < H(b.x, b.z) ? a : b) : (Clear(a) > Clear(b) ? a : b);
                    // 고른 쪽이 다른 차길(갈림목의 갈래 등) 위면 반대쪽, 그래도면 바깥으로 2m 씩
                    if (NearCar(pos, 2.5f)) pos = pos == a ? b : a;
                    for (int k = 0; k < 6 && NearCar(pos, 2.5f); k++) pos += (pos - c).normalized * 2f;
                    pos.y = H(pos.x, pos.z);
                    Landmark(parent, name, pos, rot);
                    nL++; names.Add(name);
                }
            }
        return $"features: 랜드마크 {nL} · " + string.Join(" / ", names);
    }

    static float PlanLen(Road r)
    {
        float l = 0; for (int i = 1; i < r.knots.Count; i++) l += Vector2.Distance(new Vector2(r.knots[i].x, r.knots[i].z), new Vector2(r.knots[i - 1].x, r.knots[i - 1].z));
        if (r.closed) l += Vector2.Distance(new Vector2(r.knots[0].x, r.knots[0].z), new Vector2(r.knots[r.knots.Count - 1].x, r.knots[r.knots.Count - 1].z));
        return l;
    }

    /// <summary>둔턱: 3.5m 동안 step 만큼 오르고 0.6m 평탄, 1.0m 에 떨어짐 — 짐이 뜬다. 노면 위에 얹는 메시 + 콜라이더.</summary>
    static void Jump(Transform parent, Road r, float s, float step, string name)
    {
        var prof = new List<(float q, float h)> { (-3.5f, 0f), (-2.5f, step * 0.32f), (-1.5f, step * 0.62f), (-0.6f, step * 0.88f), (0f, step), (0.6f, step), (1.6f, 0f) };
        float hw = r.width * .5f + 0.2f;
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        foreach (var (q, h) in prof)
        {
            At(r, s + q, out var c, out _, out var rt);
            verts.Add(c + rt * -hw + Vector3.up * (h + 0.005f)); verts.Add(c + rt * hw + Vector3.up * (h + 0.005f));
            uvs.Add(new Vector2(0, q)); uvs.Add(new Vector2(1, q));
        }
        for (int i = 0; i + 1 < prof.Count; i++) { int a = i * 2, b = a + 1, c = a + 2, d = a + 3; tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
        var go = MeshGo(parent, "INTENT_Jump_" + r.id, verts, uvs, new[] { tris }, new[] { M("JumpStripe") }, true);
        go.AddComponent<RoadSurface>().roadId = r.id + "_jump";
    }

    static void Landmark(Transform parent, string name, Vector3 p, Quaternion rot)
    {
        var g = new GameObject("LANDMARK_" + name).transform; g.SetParent(parent); g.SetPositionAndRotation(p, rot);
        void B(Vector3 size, Vector3 local, string mat, bool col = true) => Box(g, "Part", size, p + rot * local, rot, M(mat), col);
        void Cy(Vector3 size, Vector3 local, string mat) => Cyl(g, "Part", size, p + rot * local, M(mat));
        if (name.Contains("크레인")) { B(new Vector3(1.2f, 18f, 1.2f), new Vector3(0, 9f, 0), "Yellow"); B(new Vector3(1f, 1f, 16f), new Vector3(0, 17.5f, -4f), "Yellow", false); B(new Vector3(0.1f, 8f, 0.1f), new Vector3(0, 13f, -11f), "Dark", false); }
        else if (name.Contains("종탑")) { B(new Vector3(3f, 10f, 3f), new Vector3(0, 5f, 0), "Stone"); B(new Vector3(3.6f, 0.6f, 3.6f), new Vector3(0, 10.3f, 0), "Roof", false); Cy(new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0, 11.6f, 0), "Roof"); }
        else if (name.Contains("우체통")) { B(new Vector3(0.6f, 1.3f, 0.5f), new Vector3(0, 0.65f, 0), "Yellow"); }
        else if (name.Contains("물탱크")) { foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) B(new Vector3(0.25f, 6f, 0.25f), new Vector3(sx * 1.4f, 3f, sz * 1.4f), "Dark"); Cy(new Vector3(3.6f, 1.5f, 3.6f), new Vector3(0, 7.5f, 0), "TinRoof"); }
        else if (name.Contains("등대")) { Cy(new Vector3(3f, 7f, 3f), new Vector3(0, 7f, 0), "White"); Cy(new Vector3(2.2f, 1f, 2.2f), new Vector3(0, 15f, 0), "Red"); B(new Vector3(3.2f, 2.5f, 3.2f), new Vector3(0, 1.25f, 0), "White"); }
        else if (name.Contains("어시장")) { FishMarket(g, p, rot, 12f, 8f, "FishMarketRoof"); }
        else if (name.Contains("이정표")) { B(new Vector3(0.5f, 1.3f, 0.35f), new Vector3(0, 0.65f, 0), "Stone"); }
        else if (name.Contains("풍차")) { Cy(new Vector3(3f, 5f, 3f), new Vector3(0, 5f, 0), "White"); for (int i = 0; i < 4; i++) Box(g, "Blade", new Vector3(0.6f, 7f, 0.1f), p + rot * new Vector3(0, 10f, -1.7f), rot * Quaternion.Euler(0, 0, i * 90f + 20f) * Quaternion.Euler(0, 0, 0), M("Wood"), false); }
        else { B(new Vector3(2f, 4f, 2f), new Vector3(0, 2f, 0), "Red"); }
    }

    // ------------------------------------------------------------------ 전봇대·밭·배달지
    static string Poles(Transform parent)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Lofi + "UtilityPole.prefab");
        var lampPf = AssetDatabase.LoadAssetAtPath<GameObject>(Lofi + "StreetLamp.prefab");
        var main = roads.First(r => r.id == "main"); var tops = new List<Vector3>(); int lamps = 0;
        foreach (List<object> pt in (List<object>)Get(J, "poles", "points"))
        {
            float x = F(pt[0]), z = F(pt[1]); float s = Project(main, x, z); At(main, s, out var c, out var f, out _);
            bool lamp = pt.Count > 2 && (pt[2] as string) == "StreetLamp" && lampPf != null;   // v4: 인도가 있는 곳은 가로등(전선 없음)
            var go = (GameObject)PrefabUtility.InstantiatePrefab(lamp ? lampPf : pf, parent);
            // 갈래 차길 위에 떨어진 점은 본선 반대쪽으로 1m씩 비켜 세운다(최대 8m), 그래도 겹치면 생략
            var away = Flat(new Vector3(x, 0, z) - new Vector3(c.x, 0, c.z)); if (away.sqrMagnitude < 0.01f) away = Vector3.Cross(Vector3.up, Flat(f));
            away.Normalize(); var q = new Vector3(x, 0, z);
            for (int k = 0; k < 8 && NearCar(q, 0.8f); k++) q += away;
            if (NearCar(q, 0.8f)) { Object.DestroyImmediate(go); continue; }
            x = q.x; z = q.z;
            var pos = new Vector3(x, (lamp ? BandTop(new Vector3(x, c.y, z)) : H(x, z)) - 0.1f, z);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Flat(f)));
            if (lamp) { lamps++; go.name = "StreetLamp"; } else tops.Add(pos + Vector3.up * 6.2f);
        }
        for (int i = 0; i + 1 < tops.Count; i++)
        {
            var a = tops[i]; var b = tops[i + 1]; if (Vector3.Distance(a, b) > 40f) continue;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Wire"; go.transform.SetParent(parent);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = (a + b) * .5f - Vector3.up * 0.4f; go.transform.rotation = Quaternion.LookRotation(b - a); go.transform.localScale = new Vector3(0.04f, 0.04f, Vector3.Distance(a, b));
            go.GetComponent<Renderer>().sharedMaterial = M("Dark");
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // v5: 전선 그림자가 노면 위 '중앙 점선'으로 읽혔다(D-141 ④)
        }
        return $"전봇대 {tops.Count}·가로등 {lamps}";
    }

    static void Fields(Transform parent)
    {
        foreach (Dictionary<string, object> f in (List<object>)J["fields"])
        {
            var rc = (List<object>)f["rect"]; float x = F(rc[0]), z = F(rc[1]), w = F(rc[2]), d = F(rc[3]); int bands = System.Convert.ToInt32(f["bands"]);
            for (int b = 0; b < bands; b++)
            {
                float z0 = z + d * b / bands + 0.4f, z1 = z + d * (b + 1) / bands - 0.4f;
                var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
                int nx = Mathf.CeilToInt(w / 2f) + 1, nz = Mathf.CeilToInt((z1 - z0) / 2f) + 1;
                for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++)
                    { float px = x + w * i / (nx - 1), pz = z0 + (z1 - z0) * j / (nz - 1); verts.Add(new Vector3(px, H(px, pz) + 0.06f, pz)); uvs.Add(new Vector2(px, pz)); }
                for (int j = 0; j + 1 < nz; j++) for (int i = 0; i + 1 < nx; i++) { int a = j * nx + i, bb = a + 1, c = a + nx, dd = c + 1; tris.Add(a); tris.Add(c); tris.Add(bb); tris.Add(bb); tris.Add(c); tris.Add(dd); }
                MeshGo(parent, "Field_" + f["id"] + "_" + b, verts, uvs, new[] { tris }, new[] { M(b % 2 == 0 ? "FieldA" : "FieldB") }, false);
            }
        }
    }

    static string Stops(Transform parent)
    {
        var sb = new List<string>();
        foreach (Dictionary<string, object> st in (List<object>)J["stops"])
        {
            var p = (List<object>)st["pos"]; float x = F(p[0]), z = F(p[1]);
            var pos = new Vector3(x, H(x, z), z);
            var g = new GameObject($"STOP_{st["n"]}_{st["id"]}").transform; g.SetParent(parent); g.position = pos;
            Box(g, "Door", new Vector3(1.6f, 2.4f, 0.25f), pos + Vector3.up * 1.2f, Quaternion.identity, M("Orange"), false);
            Box(g, "Beacon", new Vector3(0.3f, 6f, 0.3f), pos + Vector3.up * 3f + Vector3.right * 1.2f, Quaternion.identity, M("Orange"), false);
            sb.Add($"{st["n"]}:{st["name"]}({x:F0},{pos.y:F1},{z:F0})");
        }
        {
            var p = (List<object>)Get(J, "mart", "pos"); float x = F(p[0]), z = F(p[1]) + 10f;
            float y = H(x, z - 4f);
            var g = new GameObject("MART").transform; g.SetParent(parent); g.position = new Vector3(x, y, z);
            Box(g, "Hall", new Vector3(12f, 5f, 8f), new Vector3(x, y + 2.5f - 0.4f, z + 1f), Quaternion.identity, M("MartWall"), true);
            Box(g, "Sign", new Vector3(8f, 1.2f, 0.3f), new Vector3(x, y + 5.4f, z - 3.1f), Quaternion.identity, M("MartSign"), false);
            Box(g, "Canopy", new Vector3(12f, 0.2f, 3f), new Vector3(x, y + 3.2f, z - 4.3f), Quaternion.identity, M("MartSign"), false);
            sb.Add($"MART({x:F0},{y:F1},{z:F0})");
        }
        return "배달지 " + string.Join(" ", sb);
    }

    // ------------------------------------------------------------------ 차
    static void Car(Road main)
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
        At(main, 8f, out var c, out var f, out _);
        car.transform.position = c + Vector3.up * .6f; car.transform.rotation = Quaternion.LookRotation(Flat(f));
        var cso = new SerializedObject(car);
        cso.FindProperty("fallRespawnY").floatValue = -3f;          // 해면 0 아래로 떨어지면(바다·해안 절벽 밖) 리스폰. 가장 낮은 길 2.0
        cso.FindProperty("safePointAboveFall").floatValue = 4f;
        cso.ApplyModifiedPropertiesWithoutUndo();
        var cl = new GameObject("CourseCenterline").AddComponent<CourseCenterline>(); cl.transform.SetParent(root);
        var pts = new List<Vector3>(); var fws = new List<Vector3>();
        foreach (var r in roads.Where(x => x.Car)) for (int i = 0; i < r.pts.Count; i += 2) { pts.Add(r.pts[i]); fws.Add(Fwd(r.pts, i, r.closed)); }
        cl.points = pts.ToArray(); cl.forwards = fws.ToArray(); EditorUtility.SetDirty(cl);
        new GameObject("BayLoopDriveOnly (임시: 주문 배선 전)").AddComponent<BayLoopDriveOnly>().transform.SetParent(root);
        var fc = Object.FindFirstObjectByType<FollowCamera>(FindObjectsInactive.Include);
        if (fc != null)
        {
            var sf = Flat(f);
            fc.transform.position = car.transform.position - sf * 4.2f + Vector3.up * 1.9f; fc.transform.LookAt(car.transform.position + sf * 8f + Vector3.up);
            var cam = fc.GetComponent<Camera>(); if (cam != null) cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1800f);
        }
        log.Add($"차 시작 ({c.x:F0},{c.y:F1},{c.z:F0}) 본선 s8, 리스폰 y<-3, 중심선 {pts.Count}점");
    }

    // ------------------------------------------------------------------ 검사(에디트 모드, 빌드 직후)
    /// <summary>
    /// 하향 레이 검사: 차길 2m 마다 가운데·양쪽(반폭−0.6)에서 위 6m → 아래로. 첫 맞은 것이 길 메시가 아니고 노면보다 0.3m 위면 '길 위 장애',
    /// 아무것도 안 맞으면 '구멍', 길인데 높이가 0.25m 넘게 다르면 '높이 어긋남'. INTENT_(볼라드·돌담·둔턱)는 의도라 따로 센다.
    /// 그리고 콜라이더 달린 건물·기단의 바닥 가운데 아래 지형까지 0.6m 넘게 비면 '떠 있음'.
    /// </summary>
    public static string Check()
    {
        Physics.SyncTransforms();
        var sb = new StringBuilder(); int total = 0;
        foreach (var rs in Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 1 && (r.kind == "main" || r.kind == "village" || r.kind == "shortcut")))
        {
            int block = 0, hole = 0, mism = 0, intent = 0, n = 0, step = 0; var ex = new List<string>();
            var p = rs.centerline; var prevTop = new[] { float.NaN, float.NaN, float.NaN, float.NaN, float.NaN };
            for (int i = 0; i < p.Length; i += 2)
            {
                var f = Fwd(p.ToList(), i, rs.closed); var rt = new Vector3(f.z, 0, -f.x);
                float wi = rs.widths != null && rs.widths.Length == p.Length ? rs.widths[i] : rs.width;   // v4 점마다 폭
                float e1 = wi * .5f - 0.6f, e2 = wi * .5f - 0.1f;   // 차 상자 폭 3.8m — 가장자리까지 본다
                var offs = new[] { 0f, -e1, e1, -e2, e2 };
                for (int oi = 0; oi < offs.Length; oi++)
                {
                    float off = offs[oi];
                    var o = p[i] + rt * off; n++;
                    var hits = Physics.RaycastAll(o + Vector3.up * 40f, Vector3.down, 46f).Where(h => h.collider.attachedRigidbody == null).OrderBy(h => h.distance).ToArray();   // 차 자신 제외
                    if (hits.Length == 0) { hole++; if (ex.Count < 4) ex.Add($"구멍({o.x:F0},{o.z:F0})"); continue; }
                    var h0 = hits[0]; bool isRoad = h0.collider.GetComponentInParent<RoadSurface>() != null;
                    if (h0.collider.name.StartsWith("INTENT_") || h0.collider.transform.root.name.StartsWith("INTENT_") || (h0.collider.transform.parent != null && h0.collider.transform.parent.name.StartsWith("INTENT_"))) { intent++; continue; }
                    if (!isRoad && h0.point.y > o.y + 0.3f) { block++; if (ex.Count < 4) ex.Add($"장애 {h0.collider.name}({o.x:F0},{o.z:F0})"); continue; }
                    // 턱 = 차가 느끼는 것: 같은 줄(off)에서 2m 앞 맨 위 노면이 중심선 경사보다 8~25cm 더 튀면. 다른 길 띠에 올라탄 구간도 이걸로 본다
                    bool jump = h0.collider.name.Contains("_jump");
                    if (isRoad && !jump && i >= 2 && !float.IsNaN(prevTop[oi]))
                    {
                        float dj = (h0.point.y - prevTop[oi]) - (p[i].y - p[i - 2].y);
                        if (Mathf.Abs(dj) > 0.08f && Mathf.Abs(dj) <= 0.6f) { step++; if (ex.Count < 4) ex.Add($"턱{dj:+0.00;-0.00} {h0.collider.name}({o.x:F0},{o.z:F0})"); }
                    }
                    prevTop[oi] = isRoad && !jump ? h0.point.y : float.NaN;
                    var hitRs = h0.collider.GetComponentInParent<RoadSurface>();
                    if (isRoad && hitRs == rs && Mathf.Abs(h0.point.y - o.y) > 0.25f && !jump) { mism++; if (ex.Count < 4) ex.Add($"높이{h0.point.y - o.y:+0.00;-0.00} {h0.collider.name}({o.x:F0},{o.z:F0})"); }
                }
            }
            total += block + hole + step + mism;
            sb.Append($"{rs.roadId}: 레이 {n} 장애 {block} 구멍 {hole} 턱(8~25cm) {step} 높이어긋남 {mism} 의도 {intent} {string.Join(" ", ex)}\n");
        }
        int floating = 0; var fx = new List<string>();
        foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            var top = col.transform; while (top.parent != null && top.parent.name != "Buildings") top = top.parent;
            if (top.parent == null || top.name.StartsWith("Plinth_")) continue;   // 기단은 가장 낮은 모서리 −0.35m 까지 내려 만든다(정의상 안 뜸)
            // 위에서 아래로 쏴서, 자기 자신·다른 건물은 건너뛰고 첫 '바닥'(지형·길·기단)까지. 바닥이 콜라이더 밑면보다 0.6m 넘게 낮으면 떠 있음.
            var b = col.bounds; var o = new Vector3(b.center.x, b.max.y + 2f, b.center.z);
            float groundY = float.NegativeInfinity;
            foreach (var h in Physics.RaycastAll(o, Vector3.down, 200f).OrderBy(h => h.distance))
            {
                if (h.collider == col || h.collider.transform.IsChildOf(top)) continue;
                var ht = h.collider.transform; while (ht.parent != null && ht.parent.name != "Buildings") ht = ht.parent;
                if (ht.parent != null && !ht.name.StartsWith("Plinth_")) continue;
                groundY = h.point.y; break;
            }
            float gap = b.min.y - groundY;
            if (gap > 0.6f) { floating++; if (fx.Count < 5) fx.Add($"{top.name} 틈{gap:F1}({b.center.x:F0},{b.min.y:F1},{b.center.z:F0})"); }
        }
        sb.Append($"떠 있는 건물 콜라이더 {floating} {string.Join(" ", fx)}\n");
        return $"차길 장애+구멍+턱+높이어긋남 합 {total}\n" + sb;
    }

    // ------------------------------------------------------------------ 샘플·도우미
    static List<Vector3> Sample(Spline sp, float step, bool closed)
    {
        float len = sp.GetLength(); int n = Mathf.Max(16, Mathf.CeilToInt(len * 4));
        var fine = new List<Vector3>(n + 1);
        for (int i = 0; i <= n; i++) { float3 p = SplineUtility.EvaluatePosition(sp, i / (float)n); fine.Add(new Vector3(p.x, p.y, p.z)); }
        var outp = new List<Vector3> { fine[0] }; float acc = 0, next = step;
        for (int k = 1; k < fine.Count; k++)
        {
            float d = Vector3.Distance(fine[k - 1], fine[k]);
            while (d > 1e-6f && acc + d >= next) { outp.Add(Vector3.Lerp(fine[k - 1], fine[k], (next - acc) / d)); next += step; }
            acc += d;
        }
        if (closed) { if (Vector3.Distance(outp[outp.Count - 1], outp[0]) < step * .5f) outp.RemoveAt(outp.Count - 1); }
        else if (Vector3.Distance(outp[outp.Count - 1], fine[fine.Count - 1]) > 0.05f) outp.Add(fine[fine.Count - 1]);
        return outp;
    }
    static float Len(List<Vector3> p, bool closed) { float l = 0; for (int i = 1; i < p.Count; i++) l += Vector3.Distance(p[i - 1], p[i]); if (closed) l += Vector3.Distance(p[p.Count - 1], p[0]); return l; }
    static float MaxGrade(List<Vector3> p)
    {
        float g = 0; for (int i = 4; i < p.Count; i += 2) { var d = p[i] - p[i - 4]; float h = new Vector2(d.x, d.z).magnitude; if (h > 0.5f) g = Mathf.Max(g, Mathf.Abs(d.y) / h); } return g;
    }
    static Vector3 Fwd(List<Vector3> p, int i, bool closed)
    {
        int n = p.Count; int a = closed ? (i - 2 + n) % n : Mathf.Max(0, i - 2), b = closed ? (i + 2) % n : Mathf.Min(n - 1, i + 2);
        var f = p[b] - p[a]; f.y = 0; return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
    }
    static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized; }

    /// <summary>길 위 s(m) 지점 — 1m 샘플이라 s ≈ 점 번호.</summary>
    static void At(Road r, float s, out Vector3 c, out Vector3 f, out Vector3 rt)
    {
        int n = r.pts.Count;
        if (r.closed) s = Mathf.Repeat(s, n); else s = Mathf.Clamp(s, 0, n - 1.001f);
        int i = Mathf.FloorToInt(s); float u = s - i;
        c = Vector3.Lerp(r.pts[i % n], r.pts[(i + 1) % n], u);
        f = Fwd(r.pts, i % n, r.closed); rt = new Vector3(f.z, 0, -f.x);
    }
    static float Project(Road r, float x, float z)
    {
        int best = 0; float bd = float.MaxValue;
        for (int i = 0; i < r.pts.Count; i++) { float d = (r.pts[i].x - x) * (r.pts[i].x - x) + (r.pts[i].z - z) * (r.pts[i].z - z); if (d < bd) { bd = d; best = i; } }
        return best;
    }

    static Transform Group(string name) { var g = new GameObject(name).transform; g.SetParent(root); return g; }

    static GameObject Box(Transform parent, string name, Vector3 size, Vector3 pos, Quaternion rot, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.rotation = rot; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static void Cyl(Transform parent, string name, Vector3 scale, Vector3 pos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    /// <summary>재질: 기존 에셋은 그대로 참조, 없는 색은 LofiPilot 셰이더 단색으로 MatDir 에 만든다(무드 안개가 같이 먹게).</summary>
    static Material M(string key)
    {
        if (mats.TryGetValue(key, out var m)) return m;
        string src = key switch
        {
            "Asphalt" => "Assets/Art/StreetFurniture/Asphalt.mat", "Pavement" => "Assets/Art/CoastalTown/Pavement.mat",
            "Ground" => "Assets/Art/CoastalTown/Ground.mat", "Concrete" => "Assets/Art/CoastalTown/BoundaryConcrete.mat",
            "Sea" => "Assets/Art/CoastalTown/Sea.mat", _ => null
        };
        if (src != null) m = AssetDatabase.LoadAssetAtPath<Material>(src);
        if (m == null)
        {
            Color c = key switch
            {
                "Gravel" => new Color(.62f, .54f, .42f), "AlleyPath" => new Color(.78f, .74f, .66f), "Stairs" => new Color(.70f, .68f, .64f),
                "Curb" => new Color(.74f, .72f, .68f), "Wood" => new Color(.45f, .33f, .22f), "TinRoof" => new Color(.55f, .62f, .66f),
                "Fish" => new Color(.30f, .50f, .70f), "Stone" => new Color(.60f, .58f, .54f), "Yellow" => new Color(.95f, .78f, .15f),
                "Dark" => new Color(.18f, .18f, .20f), "Roof" => new Color(.70f, .40f, .30f), "White" => new Color(.95f, .95f, .93f),
                "Red" => new Color(.80f, .20f, .18f), "JumpStripe" => new Color(.85f, .70f, .25f), "FieldA" => new Color(.52f, .66f, .30f),
                "FieldB" => new Color(.74f, .72f, .36f), "Orange" => new Color(1f, .42f, .08f), "MartWall" => new Color(.92f, .92f, .88f),
                "MartSign" => new Color(.16f, .42f, .78f), "Paving" => new Color(.72f, .66f, .58f), "Sand" => new Color(.88f, .80f, .62f),
                "Meadow" => new Color(.56f, .64f, .36f), "CurbStone" => new Color(.50f, .49f, .47f), "CurbDark" => new Color(.36f, .36f, .37f),   // v5: 밝은 연석 윗면이 흰 선처럼 읽혔다 → 회색(.60 도 그늘 인도 옆에선 흰 띠 — 2차 .50)
                "LotStone" => new Color(.70f, .66f, .60f), "LotConcrete" => new Color(.72f, .71f, .68f), "LotGravel" => new Color(.63f, .58f, .50f), "LotDark" => new Color(.56f, .54f, .51f),
                "PatchDark" => new Color(.30f, .30f, .31f), "PatchLight" => new Color(.50f, .49f, .47f), "PaintWhite" => new Color(.90f, .90f, .86f), "MirrorGlass" => new Color(.75f, .85f, .92f),
                "Soil" => new Color(.42f, .32f, .22f), "PathDirt" => new Color(.60f, .52f, .40f), "Gutter" => new Color(.42f, .42f, .42f), _ => Color.magenta
            };
            string p = MatDir + "/BL_" + key + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null)
            {
                var sh = Shader.Find("PitCrew/LofiPilot/Surface") ?? Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(sh); AssetDatabase.CreateAsset(m, p);
            }
            if (m.HasProperty("_Tint")) m.SetColor("_Tint", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
        }
        mats[key] = m; return m;
    }

    // ------------------------------------------------------------------ 작은 JSON 파서(Newtonsoft 없음)
    static class Json
    {
        public static object Parse(string s) { int i = 0; return V(s, ref i); }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object V(string s, ref int i)
        {
            Ws(s, ref i); char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true) { Ws(s, ref i); var k = (string)V(s, ref i); Ws(s, ref i); i++; d[k] = V(s, ref i); Ws(s, ref i); if (s[i++] == ',') continue; return d; }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true) { l.Add(V(s, ref i)); Ws(s, ref i); if (s[i++] == ',') continue; return l; }
            }
            if (c == '"')
            {
                var sb = new StringBuilder(); i++;
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++; char e = s[i];
                        if (e == 'u') { sb.Append((char)System.Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                        else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e == 'b' ? '\b' : e == 'f' ? '\f' : e);
                    }
                    else sb.Append(s[i]);
                    i++;
                }
                i++; return sb.ToString();
            }
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }
    }
}
