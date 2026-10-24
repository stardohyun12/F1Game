using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 프로토 코스 (과제 20261023-1754, D-120). 슬라이스(과제 1733, 원화 02_warm_street 재현)를 한 줄 코스로 키운 것.
/// 항구 MART 가게(주문·포장) → 골목 내리막 → 바다 열리는 커브 → 해안도로 → 헤어핀 오르막 3개 → 언덕 마을 광장(되돌아 나올 수 있음).
/// 씬은 ComplexTrack.unity 를 복사해 시스템(주문판·긴급전화·포장·RunController·DeliveryManager·정산·UI·카메라)을 배선째 그대로 두고,
/// 월드(트랙·건물·소품)만 지운 뒤 이 코스를 깐다. ComplexTrack 원본은 안 건드린다.
/// 머티리얼은 전부 사본(Proto_CoastCourse_Mats/) — 무드를 구워 넣어도 ComplexTrack 머티리얼은 안 바뀐다.
/// 없는 소품은 "BLOCKOUT_" 회색 블록(아트팀 목록 = Blockouts).
/// </summary>
public static class BuildPostcardSlice
{
    public const string ScenePath = "Assets/Scenes/Proto_CoastCourse.unity";
    const string SrcScene = "Assets/ComplexTrack.unity";
    const string MatDir = "Assets/Scenes/Proto_CoastCourse_Mats";
    const string OrderDir = "Assets/Data/Orders/ProtoCourse";
    const string Lofi = "Assets/Art/LofiPilot/";

    // ── 길: (길이, 회전각 + 오른쪽, 경사) ──
    static readonly (float len, float turn, float grade)[] Segs =
    {
        (50f, 0f, -0.065f),                       // 0 골목 직선 (원화 장면)
        (40f * 75f * Mathf.Deg2Rad, 75f, -0.065f),// 1 바다 열리는 커브
        (60f, 0f, -0.05f),                        // 2
        (120f * 15f * Mathf.Deg2Rad, 15f, -0.02f),// 3 해안으로
        (150f, 0f, 0f),                           // 4 해안도로
        (40f, 0f, 0.04f),                         // 5 오르막 시작
        (Mathf.PI * 14f, 180f, 0.07f),            // 6 헤어핀 1 (오른쪽)
        (60f, 0f, 0.08f),                         // 7
        (Mathf.PI * 14f, -180f, 0.07f),           // 8 헤어핀 2 (왼쪽)
        (60f, 0f, 0.08f),                         // 9
        (Mathf.PI * 14f, 180f, 0.07f),            // 10 헤어핀 3 (오른쪽)
        (60f, 0f, 0.03f),                         // 11 정점 전망 (1827): 오른쪽 아래 바다·출발 항구 — 시야 막는 것 없음
        (40f * 90f * Mathf.Deg2Rad, -90f, 0.02f), // 12 왼쪽으로 굽어 내륙으로
        (30f, 0f, 0f),                            // 13 언덕마루
        (120f, 0f, -0.01f),                       // 14 들판 한가운데 길 → 외딴집 마당
    };
    const float Y0 = 8f;
    const float HalfRoad = 3.25f, Walk = 2f, WallD = 5.0f, WallH = 2.65f, CliffFrom = 56f;
    const float SliceEnd = 165f;   // 이 앞은 원화 슬라이스 단면(옹벽·절벽), 뒤는 지형 덮개
    const float SeaY = -10f, PlazaR = 14f;   // PlazaR = 외딴집 마당 반경
    public const float SpawnS = 5f, SpawnD = 1.2f;

    /// <summary>구간 목록 하나 = 길 하나. 본선(지름길 포함)과 해안 우회가 각각 하나씩.</summary>
    class RoadPath
    {
        public readonly (float len, float turn, float grade)[] segs;
        public readonly float[] S, Y, H; public readonly Vector3[] P; readonly float y0;
        public RoadPath((float, float, float)[] sg, Vector3 p0, float h0, float y0)
        {
            segs = sg; this.y0 = y0; int n = sg.Length; S = new float[n + 1]; Y = new float[n + 1]; H = new float[n + 1]; P = new Vector3[n + 1];
            P[0] = p0; H[0] = h0;
            for (int i = 0; i < n; i++) { S[i + 1] = S[i] + segs[i].len; Eval(i, segs[i].len, out var p, out var h); P[i + 1] = p; H[i + 1] = h; Y[i + 1] = Y[i] + segs[i].grade * segs[i].len; }
        }
        public float Total => S[segs.Length];
        void Eval(int i, float u, out Vector3 p, out float h)
        {
            var sg = segs[i]; float h0 = H[i]; var p0 = P[i];
            if (sg.turn == 0f) { h = h0; p = p0 + Fwd(h0) * u; return; }
            float R = sg.len / Mathf.Abs(sg.turn * Mathf.Deg2Rad);
            if (sg.turn > 0) { var c = p0 + Right(h0) * R; h = h0 + u / R; p = c - Right(h) * R; }
            else { var c = p0 - Right(h0) * R; h = h0 - u / R; p = c + Right(h) * R; }
        }
        public void Frame(float s, out Vector3 c, out Vector3 f, out Vector3 r)
        {
            int i = 0; while (i < segs.Length - 1 && s > S[i + 1]) i++;
            float u = s - S[i]; Eval(i, u, out var p, out var h);
            p.y = y0 + Y[i] + segs[i].grade * u; c = p; f = Fwd(h); r = Right(h);
        }
    }
    static RoadPath main, detour, cur;
    public static float Total { get { Init(); return main.Total; } }
    public static float SegStart(int i) { Init(); return main.S[i]; }

    // ── 갈림길 (1827): 해안도로 끝(본선 구간 5 시작)에서 갈라져 정점(구간 11 시작)에서 다시 만난다 ──
    //   본선 = 지름길: 헤어핀 3개, 짧고 급경사(7~8%) → 빠르지만 화물이 흔들린다.
    //   우회 = 해안길: 바다 쪽으로 비켜 동쪽으로 더 가다 큰 반원(반경 ≈47m)으로 돌아 완만하게(≈4%) 올라온다 → 길고 평탄.
    public static float ForkS => SegStart(5) - ForkBack;
    const float ForkBack = 50f;   // 헤어핀1 시작 50m 앞(평탄 해안도로)에서 갈라짐 — 붙어 있으면 헤어핀 지형 비탈이 우회로를 덮는다(실측)
    public static float JoinS => SegStart(11);
    const float DetourEast = 100f;   // 반원 전 동쪽 직선(가설: 우회가 지름길의 약 1.8배 길이)
    public static float DetourLength { get { Init(); return detour.Total; } }
    /// <summary>route 0 = 지름길(본선), 1 = 해안 우회. 출발점부터 잰 길이.</summary>
    public static float RouteTotal(int route) { Init(); return route == 0 ? main.Total : ForkS + detour.Total + (main.Total - JoinS); }
    public static void RouteFrame(int route, float s, out Vector3 c, out Vector3 f, out Vector3 r)
    {
        Init();
        if (route == 0 || s <= ForkS) main.Frame(s, out c, out f, out r);
        else if (s <= ForkS + detour.Total) detour.Frame(s - ForkS, out c, out f, out r);
        else main.Frame(s - ForkS - detour.Total + JoinS, out c, out f, out r);
    }

    static readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    static Transform root;
    public static readonly List<string> Blockouts = new List<string>();
    /// <summary>배달지 4곳: (라벨, s, 옆 거리). 가까운 곳=쉬움 → 꼭대기=어려움(D-100 랜드마크).</summary>
    public static (string label, float s, float d)[] Stops => new[] { ("AlleyEnd", 150f, 1.2f), ("CoastCafe", CafeS, 1.2f), ("HairpinOrchard", SegStart(9) + 35f, 1.2f), ("LoneHouse", Total + 12f, 0f) };
    const float CafeS = 250f;   // 1827: 330 → 250 (갈림길 표지판 자리 비움)

    [MenuItem("PitCrew/Slice/Build Proto Coast Course")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Debug.Log(BuildScene());
    }

    static void Init()
    {
        if (main != null) return;
        main = new RoadPath(Segs, Vector3.zero, 0f, Y0); cur = main;
        // 우회: S자(왼 15° → 오른 15°, 반경 250)로 바다 쪽 ≈17m 비킨 뒤 동쪽 직선 → 오른쪽 반원 → 서쪽 직선으로 정점에 합류.
        main.Frame(main.S[5] - ForkBack, out var pf, out var ff, out var rf); main.Frame(main.S[11], out var pj, out _, out _);
        float hf = Mathf.Atan2(ff.x, ff.z), sl = 250f * 15f * Mathf.Deg2Rad;
        var sCurve = new (float, float, float)[] { (sl, -15f, .04f), (sl, 15f, .04f) };
        var tmp = new RoadPath(sCurve, pf, hf, pf.y);
        tmp.Frame(tmp.Total, out var ps, out _, out _);
        float rb = Vector3.Dot(pj - ps, rf) * .5f;
        float lw = Vector3.Dot(ps + ff * DetourEast - pj, ff);
        float g = (pj.y - ps.y) / (DetourEast + Mathf.PI * rb + lw);
        detour = new RoadPath(new (float, float, float)[] { sCurve[0], sCurve[1], (DetourEast, 0f, g), (Mathf.PI * rb, 180f, g), (lw, 0f, g) }, pf, hf, pf.y);
    }

    static Vector3 Fwd(float h) => new Vector3(Mathf.Sin(h), 0, Mathf.Cos(h));
    static Vector3 Right(float h) => new Vector3(Mathf.Cos(h), 0, -Mathf.Sin(h));

    /// <summary>길 중심선 s(m) → 위치·앞·오른쪽. 처음 앞·끝 뒤는 직선으로 연장. 빌드 중엔 지금 까는 길(cur), 평소엔 본선.</summary>
    public static void Frame(float s, out Vector3 c, out Vector3 f, out Vector3 r) { Init(); cur.Frame(s, out c, out f, out r); }

    public static string BuildScene()
    {
        main = null; Init();
        copies.Clear(); Blockouts.Clear();
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Proto_CoastCourse_Mats");
        AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SrcScene, ScenePath)) return "copy failed";
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects())
            if (System.Array.IndexOf(WorldRoots, g.name) >= 0) Object.DestroyImmediate(g);
        root = new GameObject("ProtoCourse").transform;

        var asphalt = M("Assets/Art/StreetFurniture/Asphalt.mat");
        var pavement = M("Assets/Art/CoastalTown/Pavement.mat");
        var ground = M("Assets/Art/CoastalTown/Ground.mat");
        var paint = M("Assets/Art/CoastalTown/RoadPaint.mat");
        var concrete = M("Assets/Art/CoastalTown/BoundaryConcrete.mat");
        var rock = M("Assets/Art/CoastalTown/BoundaryRock.mat");
        var sea = M("Assets/Art/CoastalTown/Sea.mat");
        var grey = NewMat("Slice_Blockout", new Color(0.62f, 0.62f, 0.60f));
        var rail = NewMat("Slice_Guardrail", new Color(0.88f, 0.88f, 0.85f));
        var wire = NewMat("Slice_Wire", new Color(0.16f, 0.14f, 0.13f));
        var hill = NewMat("Slice_FarHill", new Color(0.30f, 0.44f, 0.42f));
        var village = NewMat("Slice_FarVillage", new Color(0.86f, 0.84f, 0.78f));
        var roof = NewMat("Slice_FarRoof", new Color(0.58f, 0.40f, 0.33f));
        var autumn = NewMat("Slice_AutumnTree", new Color(0.78f, 0.45f, 0.20f));

        float total = Total, s0 = -12f, s1 = total + 6f;

        // ── 길 (전 구간) ──
        var terrain = Group("Terrain");
        Strip(terrain, "Road", s0, s1, s => new[] { V(-HalfRoad, 0), V(HalfRoad, 0) }, asphalt, true);
        float fk0 = ForkS - 2f, fk1 = ForkS + 76f;                              // 갈림목: 본선 왼쪽 연석이 우회로를 막지 않게 비움
        float j0 = SegStart(10) + Segs[10].len * .4f, j1 = SegStart(11) + 4f;   // 합류목: 헤어핀3 바깥 연석 비움
        var cuts = new[] { (s0, fk0), (fk1, j0), (j1, s1) };
        foreach (var (a, b) in cuts)
            Strip(terrain, "Sidewalk_L", a, b, s => new[] { V(-HalfRoad - Walk, .15f), V(-HalfRoad, .15f), V(-HalfRoad, 0) }, pavement, true);
        Strip(terrain, "Sidewalk_R", s0, SliceEnd, s => new[] { V(HalfRoad, 0), V(HalfRoad, .15f), V(WallD, .15f) }, pavement, true);
        Strip(terrain, "Sidewalk_R2", SliceEnd - 1f, s1, s => new[] { V(HalfRoad, 0), V(HalfRoad, .15f), V(HalfRoad + Walk, .15f) }, pavement, true);
        // 길가 흙 어깨 — 인도 밖 1.5m 평지 후 지형으로 이어짐
        foreach (var (a, b) in new[] { (SliceEnd - 1f, fk0), (fk1, j0), (j1, s1) })
            Strip(terrain, "Shoulder_L", a, b, s => new[] { V(-HalfRoad - Walk - 3f, -.6f), V(-HalfRoad - Walk, .15f) }, ground, true);

        // ── 해안 우회로 (cur = detour 동안 Frame/Strip 이 우회로를 따른다) ──
        var det = Group("DetourRoad"); float dt = DetourLength;
        cur = detour;
        Strip(det, "Road", 0f, dt, s => new[] { V(-HalfRoad, -.015f), V(HalfRoad, -.015f) }, asphalt, true);   // 갈림목에서 본선과 겹침 → 1.5cm 아래
        Strip(det, "Sidewalk_L", 6f, dt, s => new[] { V(-HalfRoad - Walk, .15f), V(-HalfRoad, .15f), V(-HalfRoad, 0) }, pavement, true);
        Strip(det, "Shoulder_L", 6f, dt, s => new[] { V(-HalfRoad - Walk - 3f, -.6f), V(-HalfRoad - Walk, .15f) }, ground, true);
        Strip(det, "Sidewalk_R", 76f, dt - 25f, s => new[] { V(HalfRoad, 0), V(HalfRoad, .15f), V(HalfRoad + Walk, .15f) }, pavement, true);
        Strip(det, "Shoulder_R", 76f, dt - 25f, s => new[] { V(HalfRoad + Walk, .15f), V(HalfRoad + Walk + 3f, -.6f) }, ground, true);
        Strip(det, "Dash", 76f, dt - 20f, s => Mathf.Repeat(s, 5f) < 2.2f ? new[] { V(-.07f, .012f), V(.07f, .012f) } : null, paint, false, 0.5f);
        Strip(det, "Edge_L", 76f, dt - 20f, x => new[] { V(-HalfRoad + .25f, .012f), V(-HalfRoad + .4f, .012f) }, paint, false);
        Strip(det, "Edge_R", 76f, dt - 20f, x => new[] { V(HalfRoad - .4f, .012f), V(HalfRoad - .25f, .012f) }, paint, false);
        Guardrail(det, 8f, detour.S[4], -1, rail);   // 바다 쪽 + 반원 바깥
        cur = main;
        Strip(terrain, "Shoulder_R", SliceEnd - 1f, s1, s => new[] { V(HalfRoad + Walk, .15f), V(HalfRoad + Walk + 3f, -.6f) }, ground, true);

        // ── 원화 슬라이스 구간 (s < 165): 오른쪽 옹벽+터, 왼쪽 마을 평지 → 바다 절벽 ──
        Strip(terrain, "RetainingWall_R", s0, SliceEnd, s => new[] { V(WallD, .15f), V(WallD, WallH), V(WallD + .5f, WallH) }, concrete, true);
        Strip(terrain, "Terrace_R", s0, SliceEnd, s => new[] { V(WallD + .5f, WallH), V(30f, WallH) }, ground, true);
        Strip(terrain, "Ground_L", s0, SliceEnd, s =>
        {
            float t = Mathf.InverseLerp(CliffFrom - 6f, CliffFrom + 6f, s);
            Vector2[] town = { V(-30, .15f), V(-18, .15f), V(-7.2f, .15f), V(-HalfRoad - 2f, .15f) };
            Vector2[] cliff = { V(-26, -19), V(-10.5f, -5), V(-7.2f, .15f), V(-HalfRoad - 2f, .15f) };
            var p = new Vector2[4]; for (int i = 0; i < 4; i++) p[i] = Vector2.Lerp(town[i], cliff[i], t); return p;
        }, ground, true);
        Strip(terrain, "CliffFace_L", CliffFrom - 6f, SliceEnd, s => new[] { V(-26.2f, -19.2f), V(-10.7f, -5.2f) }, rock, false);
        Strip(terrain, "BLOCKOUT_TerraceFence_R", s0, SliceEnd, s => new[] { V(WallD + .25f, WallH), V(WallD + .25f, WallH + .9f) }, grey, false);
        Blockouts.Add("TerraceFence_R — 골목 옹벽 위 난간(원화 오른쪽 위 철책)");
        Block(terrain, "StartBarrier", new Vector3(14f, 1.4f, 1f), s0 + .5f, 0, .7f, 0, concrete, true);

        // ── 지형 덮개 (s ≥ 165): 길마다 평지 어깨 + 비탈(0.9) 의 최댓값 = 헤어핀이 쌓이면 언덕이 저절로 생긴다 ──
        TerrainCover(terrain, ground);

        // ── 가드레일: 바다 쪽 · 헤어핀 바깥 · 지그재그 아래쪽 ──
        float h1 = SegStart(6), h2 = SegStart(8), h3 = SegStart(10), hEnd = SegStart(11);
        Guardrail(terrain, CliffFrom, fk0, -1, rail);       // 바다 쪽(왼쪽) — 갈림목 앞까지
        Guardrail(terrain, h1, h2, -1, rail);               // 헤어핀1(오른쪽 회전) 바깥 + 다리 7: 아래(해안) 쪽 = 오른쪽? → 바깥만
        Guardrail(terrain, h1 + Segs[6].len, h2, +1, rail); // 다리 7 아래쪽(북=오른쪽)
        Guardrail(terrain, h2, h2 + Segs[8].len, +1, rail); // 헤어핀2(왼쪽) 바깥
        Guardrail(terrain, h2 + Segs[8].len, h3, -1, rail); // 다리 9 아래쪽(북=왼쪽)
        Guardrail(terrain, h3, j0, -1, rail);               // 헤어핀3 바깥 — 합류목 앞까지
        // 정점 전망(구간 11)은 가드레일 없음 — 바다·항구 시야를 열어 둔다

        // ── 차선 ──
        var lines = Group("RoadLines");
        Strip(lines, "Dash", s0 + 1f, s1 - 2f, s => Mathf.Repeat(s - s0, 5f) < 2.2f ? new[] { V(-.07f, .012f), V(.07f, .012f) } : null, paint, false, 0.5f);
        foreach (var (a, b) in cuts)
            Strip(lines, "Edge_L", a, b, x => new[] { V(-HalfRoad + .25f, .012f), V(-HalfRoad + .4f, .012f) }, paint, false);
        Strip(lines, "Edge_R", s0, s1, x => new[] { V(HalfRoad - .4f, .012f), V(HalfRoad - .25f, .012f) }, paint, false);

        // ── 외딴집 마당 (코스 끝, 둥근 자갈 평지 — 담 없음, 들판 한가운데서 돌아 나온다) ──
        Frame(total, out var ec, out var ef, out var er);
        var plazaC = ec + ef * 12f;
        Disc(terrain, "Yard", plazaC + Vector3.up * .02f, PlazaR, pavement);
        Strip(terrain, "YardApron", total - 1f, total + 6f, s => new[] { V(-HalfRoad - Walk, .02f), V(HalfRoad + Walk, .02f) }, pavement, true);

        // ── 건물·소품 ──
        var bld = Group("Buildings"); var props = Group("Props");
        // 원화 구간 (1733 그대로)
        Prefab(bld, "CornerShop", 12f, -8.8f, .15f, FaceRoadLeft);      // MART = 출발·주문·포장
        Prefab(bld, "CornerShopC", 22f, -8.8f, .15f, FaceRoadLeft);
        Prefab(bld, "CornerShopB", 31f, -9.0f, .15f, FaceRoadLeft);
        Prefab(bld, "RepairGarage", 50f, -10.5f, .15f, FaceRoadLeft - 35f);
        foreach (var (s, d) in new[] { (6f, 9.5f), (18f, 9.5f), (30f, 9f), (44f, 9.5f), (64f, 10.5f), (84f, 10f), (104f, 9.5f), (120f, 10f), (138f, 9.5f), (154f, 10f) })
            Prefab(bld, HouseName((int)s), s, d, WallH, FaceRoadRight);
        Prefab(props, "CrateStack", 10.5f, -4.9f, .15f, 90f);
        Prefab(props, "CrateStack", 11.9f, -4.9f, .15f, 90f);
        Prefab(props, "Stall", 14.2f, -5.0f, .15f, FaceRoadLeft);
        Prefab(props, "CoastalTree", 3f, -6.8f, .15f, 0f);
        Prefab(props, "Wall3m", 1.5f, -5.4f, .15f, 90f);
        Prefab(props, "Wall3m", 4.6f, -5.4f, .15f, 90f);
        Prefab(props, "TireStack", 46.5f, -6.6f, .15f, 0f);
        Prefab(props, "TireStack", 47.3f, -6.9f, .15f, 0f);
        Prefab(props, "Barrel", 48.2f, -6.5f, .15f, 0f);
        Prefab(props, "CoastalTree", 60f, -6.2f, .15f, 0f);
        foreach (var s in new[] { 14f, 74f, 112f }) Prefab(props, "CoastalTree", s, 7.5f, WallH, 0f);
        Block(props, "BLOCKOUT_VendingMachine", new Vector3(1f, 1.8f, .8f), 27f, -5.7f, .15f + .9f, 0, grey, true);
        Blockouts.Add("VendingMachine — MART 옆 자판기");
        Block(props, "BLOCKOUT_KeiTruck", new Vector3(1.5f, 1.7f, 3.4f), 44f, -7.6f, .15f + .85f, 20f, grey, true);
        Blockouts.Add("KeiTruck — 정비소 앞 흰 경트럭");
        Block(props, "BLOCKOUT_ShopPosters", new Vector3(.05f, 1.2f, 2.2f), 15.5f, -5.35f, 1.0f, 0, grey, false);
        Blockouts.Add("ShopPosters/Display — 가게 유리 진열·포스터");
        Block(props, "BLOCKOUT_OrderBoardStand", new Vector3(1.4f, 1.6f, .15f), 8.5f, -5.0f, .15f + .8f, 90f, grey, false);
        Blockouts.Add("OrderBoardStand — MART 앞 주문판(입간판)");
        Block(props, "BLOCKOUT_PackingTable", new Vector3(1.2f, .85f, 2.2f), 17f, -5.0f, .15f + .43f, 0, grey, true);
        Blockouts.Add("PackingTable — MART 앞 포장대");
        var vanSrc = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/kenney_car-kit/Models/FBX format/delivery.fbx");
        if (vanSrc != null)
        {
            var van = (GameObject)PrefabUtility.InstantiatePrefab(vanSrc, props); van.name = "Van";
            Frame(26f, out var c, out var f, out var r);
            van.transform.position = c + r * 4.1f + Vector3.up * .15f; van.transform.rotation = Quaternion.LookRotation(-f); van.transform.localScale = Vector3.one * .78f;
            var b = Bounds(van); var bc = van.AddComponent<BoxCollider>(); bc.center = van.transform.InverseTransformPoint(b.center); bc.size = b.size / .78f;
        }

        // 골목 끝 집 (배달지 1) — 커브 끝 오른쪽 옹벽 위 집 = 이미 있는 CoastalHouse(154). 문 앞 계단만 블록아웃
        Block(props, "BLOCKOUT_AlleyEndSteps", new Vector3(1.6f, WallH, 1.2f), 150f, WallD - .6f, WallH * .5f + .1f, 0, grey, true);
        Blockouts.Add("AlleyEndSteps — 배달지1 골목 끝 집 옹벽 계단");

        // 해안: 바다 쪽 가드레일 + 육지 쪽 띄엄띄엄 집·나무, 배달지 2 해안 카페
        for (float s = 190f; s < h1 - 20f; s += 26f)
        {
            if (Mathf.Abs(s - CafeS) < 14f) continue;
            Pad(bld, s, 11f, 9f, 8f, ground);
            Prefab(bld, HouseName((int)s), s, 11f, -.3f, FaceRoadRight);
        }
        for (float s = 203f; s < h1 - 10f; s += 26f) Prefab(props, "CoastalTree", s, 7.2f, -.3f, s);
        Pad(bld, CafeS, 11f, 12f, 10f, ground);
        Block(bld, "BLOCKOUT_CoastCafe", new Vector3(7f, 3.6f, 6f), CafeS, 11f, 1.5f, -90f, grey, true);
        Block(bld, "BLOCKOUT_CoastCafeAwning", new Vector3(1.6f, .25f, 6.4f), CafeS, 6.8f, 2.6f, 0, grey, false);
        Prefab(props, "Awning", CafeS, 7.3f, .15f, FaceRoadRight);
        Blockouts.Add("CoastCafe — 배달지2 해안 카페(건물·차양·파라솔 탁자)");
        ForkSign(props, grey);
        for (float s = 175f; s < fk0; s += 9f) Block(props, "BLOCKOUT_SeaBollard", new Vector3(.25f, .7f, .25f), s, -HalfRoad - .5f, .5f, 0, grey, false);
        Blockouts.Add("SeaBollard — 해안도로 바다 쪽 볼라드·난간 기둥(지금은 가드레일과 겹침, 둘 중 하나)");

        // 헤어핀 언덕: 과수원(배달지 3)·감나무, 옹벽 블록
        var orchard = Stops[2];
        Pad(bld, orchard.s, -11f, 10f, 9f, ground);
        Block(bld, "BLOCKOUT_OrchardShed", new Vector3(6f, 3f, 5f), orchard.s, -11f, 1.2f, 90f, grey, true);
        Blockouts.Add("OrchardShed — 배달지3 과수원 창고");
        for (int i = 0; i < 14; i++)
        {
            float s = h1 + 10f + i * 14f; if (s > hEnd) break;
            float side = (i % 2 == 0) ? -1f : 1f;
            Tree(props, s, side * 7.5f, autumn);
        }
        Blockouts.Add("AutumnTree — 헤어핀 언덕 단풍·감나무(지금 주황 블록 나무)");

        // 정점 너머 (1827): 종탑은 왼쪽 굽이 안쪽 랜드마크로만, 들판 한가운데 외딴집(배달지 4)
        float bendS = SegStart(12) + Segs[12].len * .5f;
        Pad(bld, bendS, -17f, 8f, 8f, ground);
        Block(bld, "BLOCKOUT_BellTower", new Vector3(4f, 16f, 4f), bendS, -17f, 8f - .3f, 0, village, true);
        Block(bld, "BLOCKOUT_BellTowerRoof", new Vector3(5f, 2.4f, 5f), bendS, -17f, 16.9f, 45f, roof, false);
        Blockouts.Add("BellTower — 정점 굽이 안쪽 종탑(랜드마크, 해안도로에서 보여야 함)");
        float houseS = total + 12f + PlazaR + 4f;
        Pad(bld, houseS, 0f, 11f, 10f, ground);
        Prefab(bld, "CoastalHouseC", houseS, 0f, -.3f, 180f + FaceRoadRight + 90f);   // 정면이 들어오는 길(−f) 쪽
        Prefab(props, "CoastalTree", houseS - 2f, 8f, .0f, 0f);
        Block(props, "BLOCKOUT_LaundryLine", new Vector3(5f, .05f, .05f), houseS - 6f, -7f, 1.9f, 90f, grey, false);
        Block(props, "BLOCKOUT_LaundryPostA", new Vector3(.1f, 2f, .1f), houseS - 6f, -9.5f, 1f, 0, grey, false);
        Block(props, "BLOCKOUT_LaundryPostB", new Vector3(.1f, 2f, .1f), houseS - 6f, -4.5f, 1f, 0, grey, false);
        Blockouts.Add("LoneHouse — 배달지4 들판 외딴집(지금 CoastalHouseC 프리팹) · 빨랫줄·빨래(바람에 흔들림)");
        Meadow(Group("Meadow"));
        Atmosphere(grey);

        // 전봇대 + 전선 (오른쪽 인도, 헤어핀 안은 건너뜀)
        var poles = Group("UtilityPoles");
        var tops = new List<Vector3[]>(); float lastS = -999;
        for (float s = 8f; s < hEnd; s += 24f)   // 정점부터는 전봇대 없음 — 탁 트인 하늘
        {
            int si = SegIndex(s); if (Segs[si].turn != 0 && Mathf.Abs(Segs[si].turn) > 90f) continue;
            float pd = s < SliceEnd ? 4.5f : HalfRoad + 1.6f;
            if (Prefab(poles, "UtilityPole", s, pd, .15f, 0f) == null) break;
            Frame(s, out var c, out var f, out var r);
            var top = c + r * pd + Vector3.up * (.15f + 6.15f);
            var t3 = new[] { top - r * .6f, top + r * .6f, top };
            if (tops.Count > 0 && s - lastS < 30f) for (int k = 0; k < 3; k++) Wire(poles, tops[tops.Count - 1][k], t3[k], k == 2 ? .55f : .4f, wire);
            tops.Add(t3); lastS = s;
        }

        // ── 멀리: 바다·섬 ──
        var far = Group("Far");
        var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane); seaGo.name = "Sea"; seaGo.transform.SetParent(far);
        seaGo.transform.position = new Vector3(100f, SeaY, 300f); seaGo.transform.localScale = new Vector3(260f, 1f, 220f);
        Object.DestroyImmediate(seaGo.GetComponent<Collider>()); seaGo.GetComponent<Renderer>().sharedMaterial = sea;
        Hill(far, "Island_Big", new Vector3(-160f, SeaY, 820f), new Vector3(520f, 95f, 160f), hill);
        Hill(far, "Island_Right", new Vector3(330f, SeaY, 760f), new Vector3(300f, 60f, 120f), hill);
        Hill(far, "Islet", new Vector3(170f, SeaY, 330f), new Vector3(90f, 26f, 50f), hill);
        Hill(far, "Ridge_Back", new Vector3(200f, SeaY, -760f), new Vector3(1100f, 150f, 260f), hill);   // 1827: 들판(z −20~−200) 뒤로 물림 — 먼 능선
        for (int i = 0; i < 6; i++) Box(far, "BLOCKOUT_IsletHouse", new Vector3(4f, 3.5f, 4f), new Vector3(148f + i * 7f, SeaY + 4f, 320f + (i % 2) * 6f), 0, village, false);
        Blockouts.Add("Island_Big/Island_Right/Islet/Ridge_Back — 먼 섬·뒷산(구 기본도형) · IsletHouse×6");

        // ── 배달지 4곳: ComplexTrack 의 DeliveryPoint 4개를 옮겨 쓰고 나머지는 지운다 ──
        string stopLog = WireDelivery();

        // ── 주문판: 이 코스 전용 주문 4개 (ZoneUnlockSystem 은 ComplexTrack 구역 풀이라 지웠다) ──
        var catalog = BuildOrders();
        var ob = Object.FindFirstObjectByType<OrderBoard>(FindObjectsInactive.Include);
        if (ob != null) { var so = new SerializedObject(ob); so.FindProperty("catalog").objectReferenceValue = catalog; so.ApplyModifiedPropertiesWithoutUndo(); }
        var rc = Object.FindFirstObjectByType<RunController>(FindObjectsInactive.Include);
        if (rc != null) { var so = new SerializedObject(rc); so.FindProperty("runTimeLimitSec").floatValue = RunTimeLimit; so.FindProperty("parSpeedKmh").floatValue = 55f; so.ApplyModifiedPropertiesWithoutUndo(); }

        // ── 차·카메라 ──
        var car = Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
        Frame(SpawnS, out var sc, out var sf, out var sr);
        car.transform.position = sc + sr * SpawnD + Vector3.up * .6f; car.transform.rotation = Quaternion.LookRotation(sf);
        var cso = new SerializedObject(car);
        cso.FindProperty("fallRespawnY").floatValue = -30f; // 해안도로가 y −3 근처 — 기본 −20 이면 안전 지점이 −5 위에서만 저장돼 해안에서 끊긴다
        cso.FindProperty("freeDrive").boolValue = false;
        cso.ApplyModifiedPropertiesWithoutUndo();
        var fc = Object.FindFirstObjectByType<FollowCamera>(FindObjectsInactive.Include);
        CameraFeelSwitcher.BuildPresets(false);
        if (fc != null)
        {
            var fso = new SerializedObject(fc);
            fso.FindProperty("preset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.Postcard);
            fso.ApplyModifiedPropertiesWithoutUndo();
            fc.transform.position = car.transform.position - sf * 4.2f + Vector3.up * 1.9f; fc.transform.LookAt(car.transform.position + sf * 8f + Vector3.up);
            var cam = fc.GetComponent<Camera>(); if (cam != null) cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1500f);
        }

        // ── 무드 A(가을) — 월드 머티리얼을 사본으로 바꾼 뒤 적용 ──
        RemapToCopies();
        var mood = AssetDatabase.LoadAssetAtPath<MoodProfile>(MoodSwitcher.PathA);
        var sunL = GameObject.Find("Directional Light")?.GetComponent<Light>();
        var volGo = GameObject.Find("PrototypeVolume");
        var vol = volGo != null ? volGo.GetComponent<UnityEngine.Rendering.Volume>() : null;
        if (mood != null) MoodController.ApplyProfile(mood, sunL, vol);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        return "built " + ScenePath + " len=" + total.ToString("F1") + "m copies=" + copies.Count + " blockouts=" + Blockouts.Count + " limit=" + RunTimeLimit + "s | " + stopLog;
    }

    static readonly string[] WorldRoots = { "Ground", "Track", "RoadLines", "Sidewalks", "Props", "StreetFurniture", "StreetVehicles", "HarborSea", "Buildings", "ZoneGrounds", "CoastalTown", "MapBoundary", "ZoneUnlockSystem" };

    /// <summary>
    /// 제한 시간(초, D-119: 주문판이 열린 순간부터 고르기·포장·주행 전부). 가설:
    /// ComplexTrack 90초 = 고르기·포장 약 75초 + 주행 약 15초(경로 150m 안팎). 여기 가장 먼 꼭대기 = 길 약 880m ÷ 55km/h(15.3m/s) ≈ 58초.
    /// → 75 + 58 ≈ 133 → 135초 로 시작했으나, 합성 주행(헤어핀 29km/h 감속) 실측 꼭대기까지 62초 → 75 + 62×1.1 ≈ 143 → 150초.
    /// 가까운 배달지일수록 여유가 남는다(쉬움).
    /// </summary>
    public const float RunTimeLimit = 150f;

    static string WireDelivery()
    {
        var dm = Object.FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        if (dm == null) return "no DeliveryManager";
        var so = new SerializedObject(dm); var arr = so.FindProperty("points");
        var old = new List<DeliveryPoint>();
        for (int i = 0; i < arr.arraySize; i++) { var p = arr.GetArrayElementAtIndex(i).objectReferenceValue as DeliveryPoint; if (p != null) old.Add(p); }
        var stops = Stops; var keep = new List<DeliveryPoint>(); var log = new List<string>();
        for (int i = 0; i < old.Count; i++)
        {
            if (i >= stops.Length) { Object.DestroyImmediate(old[i].gameObject); continue; }
            var p = old[i]; var st = stops[i];
            p.gameObject.name = st.label;
            var pso = new SerializedObject(p); var lp = pso.FindProperty("label"); if (lp != null) { lp.stringValue = st.label; pso.ApplyModifiedPropertiesWithoutUndo(); }
            Vector3 pos;
            if (st.s > Total) { Frame(Total, out var c, out var f, out var r); pos = c + f * (st.s - Total); }
            else { Frame(st.s, out var c, out var f, out var r); pos = c + r * st.d; }
            p.transform.position = pos + Vector3.up * .5f;
            keep.Add(p); log.Add(st.label + "@s" + st.s.ToString("F0"));
        }
        dm.SetPoints(keep, null);
        EditorUtility.SetDirty(dm);
        return "stops " + string.Join(",", log);
    }

    static OrderCatalog BuildOrders()
    {
        if (!AssetDatabase.IsValidFolder(OrderDir)) AssetDatabase.CreateFolder("Assets/Data/Orders", "ProtoCourse");
        var defs = new (string file, string name, string label, CargoType cargo, int reward, int bonus, int tier, Color col)[]
        {
            ("Proto_1_AlleyEnd", "골목 끝 집 — 상자", "AlleyEnd", CargoType.Box, 600, 150, 0, new Color(.55f, .85f, .55f)),
            ("Proto_2_CoastCafe", "해안 카페 — 달걀", "CoastCafe", CargoType.Eggs, 1000, 250, 1, new Color(.55f, .75f, .95f)),
            ("Proto_3_HairpinOrchard", "헤어핀 과수원 — 케이크", "HairpinOrchard", CargoType.Cake, 1500, 400, 1, new Color(.95f, .75f, .45f)),
            ("Proto_4_LoneHouse", "들판 외딴집 — 유리", "LoneHouse", CargoType.Glass, 2200, 600, 2, new Color(1f, .45f, .45f)),
        };
        AssetDatabase.DeleteAsset(OrderDir + "/Proto_4_HilltopBellTower.asset");   // 1827: 배달지 4 = 외딴집
        string cp = OrderDir + "/ProtoCourseCatalog.asset";
        var cat = AssetDatabase.LoadAssetAtPath<OrderCatalog>(cp);
        if (cat == null) { cat = ScriptableObject.CreateInstance<OrderCatalog>(); AssetDatabase.CreateAsset(cat, cp); }
        cat.jobs = new List<OrderJob>();
        foreach (var d in defs)
        {
            string p = OrderDir + "/" + d.file + ".asset";
            var j = AssetDatabase.LoadAssetAtPath<OrderJob>(p);
            if (j == null) { j = ScriptableObject.CreateInstance<OrderJob>(); AssetDatabase.CreateAsset(j, p); }
            j.displayName = d.name; j.color = d.col; j.destinationLabels = new List<string> { d.label };
            j.timeLimitSec = 0f; j.baseReward = d.reward; j.timeBonus = d.bonus; j.weightTier = d.tier; j.cargoType = d.cargo;
            EditorUtility.SetDirty(j); cat.jobs.Add(j);
        }
        EditorUtility.SetDirty(cat);
        return cat;
    }

    // LofiPilot 건물 정면 방향 (1733 캡처로 확인): 왼쪽 건물 +90, 오른쪽 −90.
    const float FaceRoadLeft = 90f, FaceRoadRight = -90f;
    static readonly string[] Houses = { "CoastalHouse", "CoastalHouseB", "CoastalHouseC", "CoastalHouseD", "CoastalHouseE", "CoastalHouseF" };
    static string HouseName(int seed) => Houses[Mathf.Abs(seed * 7 + 3) % Houses.Length];

    static Vector2 V(float d, float h) => new Vector2(d, h);
    static int SegIndex(float s) { Init(); int i = 0; while (i < Segs.Length - 1 && s > main.S[i + 1]) i++; return i; }
    static Transform Group(string name) { var g = new GameObject(name).transform; g.SetParent(root); return g; }

    /// <summary>단면(d=옆 거리, h=높이) 점들을 s0→s1 로 밀어낸 띠 메쉬. profile 이 null 을 주면 그 칸은 비운다(점선).</summary>
    static GameObject Strip(Transform parent, string name, float s0, float s1, System.Func<float, Vector2[]> profile, Material mat, bool collider, float step = 1f)
    {
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        int n = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / step)); int m = -1; var valid = new bool[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float s = Mathf.Lerp(s0, s1, i / (float)n); Frame(s, out var c, out var f, out var r); var prof = profile(s);
            if (prof == null) { if (m < 0) m = profile(s0 + .01f)?.Length ?? 2; prof = new Vector2[m]; }
            else { valid[i] = true; m = prof.Length; }
            float u = 0;
            for (int k = 0; k < m; k++) { if (k > 0) u += Vector2.Distance(prof[k], prof[k - 1]); verts.Add(c + r * prof[k].x + Vector3.up * prof[k].y); uvs.Add(new Vector2(u, s)); }
        }
        for (int i = 0; i < n; i++)
        {
            if (!valid[i] || !valid[i + 1]) continue;
            for (int k = 0; k < m - 1; k++) { int a = i * m + k, b = a + 1, c2 = a + m, d = c2 + 1; tris.Add(a); tris.Add(c2); tris.Add(b); tris.Add(b); tris.Add(c2); tris.Add(d); }
        }
        return MeshGo(parent, name, verts, uvs, tris, mat, collider, Mathf.RoundToInt(s0 * 10));
    }

    static GameObject MeshGo(Transform parent, string name, List<Vector3> verts, List<Vector2> uvs, List<int> tris, Material mat, bool collider, int tag)
    {
        var mesh = new Mesh { name = name, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string mp = MatDir + "/Mesh_" + parent.name + "_" + name + "_" + tag + ".asset";
        AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
        var go = new GameObject(name); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    /// <summary>
    /// s ≥ SliceEnd 구간의 땅. 격자 점마다 "가장 가까운 길에서 어깨(shelf) 만큼 평평 → 그 뒤 1m 당 0.9m 내려감" 의 최댓값.
    /// 길이 지그재그로 쌓이면(헤어핀) 위 다리의 비탈이 아래 다리 쪽으로 흘러내려 언덕 하나가 된다. 바닥은 바다 밑(SeaY−4).
    /// 길보다 0.6m 아래라 길 메쉬를 뚫지 않는다.
    /// </summary>
    static readonly List<(Vector3 p, float shelf)> samp = new List<(Vector3 p, float shelf)>();
    const float SlopeK = 0.9f, FloorY = SeaY - 4f;
    /// <summary>지형 덮개와 같은 식으로 (x,z) 의 땅 높이. 들판 풀을 땅에 심을 때 쓴다.</summary>
    static float TerrainH(float x, float z)
    {
        float h = FloorY;
        foreach (var q in samp)
        {
            float dx = x - q.p.x, dz = z - q.p.z; float y = q.p.y - .6f - SlopeK * Mathf.Max(0f, Mathf.Sqrt(dx * dx + dz * dz) - q.shelf);
            if (y > h) h = y;
        }
        return h;
    }

    static void TerrainCover(Transform parent, Material mat)
    {
        samp.Clear();
        // 1827: 정점 전망까지는 어깨 6.5m(비탈이 바로 떨어져 바다·항구가 보임), 언덕마루 30m 뒤부터는 45m 평평 = 들판 고원
        float plateauS = SegStart(14) + 30f;
        for (float s = SliceEnd; s <= Total + 40f; s += 2f)
        {
            main.Frame(s, out var c, out _, out _);
            samp.Add((c, s >= plateauS ? 45f : 6.5f));
        }
        for (float s = 6f; s < detour.Total; s += 2f) { detour.Frame(s, out var c, out _, out _); samp.Add((c, 6.5f)); }
        var min = new Vector3(1e9f, 0, 1e9f); var max = new Vector3(-1e9f, 0, -1e9f);
        foreach (var q in samp) { min = Vector3.Min(min, q.p); max = Vector3.Max(max, q.p); }
        min -= new Vector3(45, 0, 45); max += new Vector3(45, 0, 45);
        const float cell = 3f, k = 0.9f, floor = SeaY - 4f;
        int nx = Mathf.CeilToInt((max.x - min.x) / cell), nz = Mathf.CeilToInt((max.z - min.z) / cell);
        var verts = new List<Vector3>((nx + 1) * (nz + 1)); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int iz = 0; iz <= nz; iz++)
            for (int ix = 0; ix <= nx; ix++)
            {
                float x = min.x + ix * cell, z = min.z + iz * cell, h = floor;
                foreach (var q in samp)
                {
                    float dx = x - q.p.x, dz = z - q.p.z; float d = Mathf.Sqrt(dx * dx + dz * dz);
                    float y = q.p.y - .6f - k * Mathf.Max(0f, d - q.shelf);
                    if (y > h) h = y;
                }
                verts.Add(new Vector3(x, h, z)); uvs.Add(new Vector2(x, z));
            }
        for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                int a = iz * (nx + 1) + ix, b = a + 1, c = a + nx + 1, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
            }
        MeshGo(parent, "HillTerrain", verts, uvs, tris, mat, true, 0);
    }

    /// <summary>갈림길 20m 전 오른쪽 길가 표지판: 두 길의 대가를 한 줄씩. 판은 임시 블록 + 3D 글자(맑은 고딕 SDF).</summary>
    static void ForkSign(Transform parent, Material post)
    {
        float s = ForkS - 20f, d = HalfRoad + Walk - .4f;
        Block(parent, "BLOCKOUT_ForkSignPostA", new Vector3(.12f, 3.2f, .12f), s, d, 1.6f + .15f, 0, post, false);
        Block(parent, "BLOCKOUT_ForkSignPostB", new Vector3(.12f, 3.2f, .12f), s, d + 2.8f, 1.6f + .15f, 0, post, false);
        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Fonts/Korean/Malgun SDF.asset");
        var boards = new[] { ("지름길 ↑  헤어핀 급경사\n화물 주의", new Color(.95f, .55f, .15f), 2.75f), ("해안길 ↖  멀지만 평탄\n화물 안전", new Color(.20f, .45f, .80f), 1.75f) };
        foreach (var (text, col, h) in boards)
        {
            var b = Block(parent, "BLOCKOUT_ForkSignBoard", new Vector3(3.2f, .9f, .08f), s, d + 1.4f, h, 0, NewMat("Proto_Sign_" + (col.r > .5f ? "Short" : "Coast"), col), false);
            var t = new GameObject("ForkSignText", typeof(TMPro.TextMeshPro)); t.transform.SetParent(b.transform.parent);
            t.transform.position = b.transform.position - b.transform.forward * .05f; t.transform.rotation = b.transform.rotation;
            var tm = t.GetComponent<TMPro.TextMeshPro>(); tm.text = text; tm.fontSize = 3.2f; tm.alignment = TMPro.TextAlignmentOptions.Center; tm.color = Color.white;
            if (font != null) tm.font = font;
            tm.rectTransform.sizeDelta = new Vector2(3.1f, .85f); tm.enableAutoSizing = true; tm.fontSizeMin = 1f; tm.fontSizeMax = 3.2f;
        }
        Blockouts.Add("ForkSign — 갈림길 표지판(지름길 주황/해안길 파랑, 글자는 3D TMP)");
    }

    /// <summary>
    /// 정점 너머 들판: 풀잎 삼각형 수만 장을 30m 타일 메쉬로. 정점 색 a = 흔들림(끝 1·뿌리 0), WindGrass 셰이더가 큰 물결로 흔든다.
    /// 길에서 5.5m 밖·마당/집 밖·고원(길보다 1.4m 이내 낮은 땅)에만 심는다. 같은 셰이더의 바닥판(흔들림 0)이 구름 그림자를 받는다.
    /// </summary>
    static void Meadow(Transform parent)
    {
        var sh = Shader.Find("PitCrew/Proto/WindGrass");
        if (sh == null) { Blockouts.Add("Meadow — WindGrass 셰이더 없음"); return; }
        string mp = MatDir + "/Proto_WindGrass.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, mp); }
        mat.SetColor("_Tint", new Color(.42f, .50f, .24f)); mat.SetColor("_TipColor", new Color(.80f, .76f, .42f));   // 가을 들판: 올리브 뿌리 → 마른 금빛 끝
        EditorUtility.SetDirty(mat);
        Frame(SegStart(14) + 60f, out var mc, out _, out _);
        const float R = 85f, tile = 30f;
        float sRoad0 = SegStart(12);
        var roadPts = new List<Vector3>(); for (float s = sRoad0; s <= Total + 6f; s += 2f) { Frame(s, out var c, out _, out _); roadPts.Add(c); }
        Frame(Total, out var ec, out var ef, out _); var yard = ec + ef * 12f; var house = ec + ef * (12f + PlazaR + 4f);
        float NearRoad(float x, float z) { float m = 1e9f; foreach (var p in roadPts) { float dx = x - p.x, dz = z - p.z; m = Mathf.Min(m, dx * dx + dz * dz); } return Mathf.Sqrt(m); }
        float RoadY(float x, float z) { float m = 1e9f, y = 0; foreach (var p in roadPts) { float dx = x - p.x, dz = z - p.z, q = dx * dx + dz * dz; if (q < m) { m = q; y = p.y; } } return y; }
        bool Ok(float x, float z, out float h)
        {
            h = TerrainH(x, z);
            var flat = new Vector2(x, z);
            if (Vector2.Distance(flat, new Vector2(yard.x, yard.z)) < PlazaR + 1f || Vector2.Distance(flat, new Vector2(house.x, house.z)) < 8f) return false;
            if (NearRoad(x, z) < 5.5f) return false;
            return h >= RoadY(x, z) - 1.4f;
        }
        var rng = new System.Random(1827); int blades = 0, tiles = 0;
        int nt = Mathf.CeilToInt(R * 2f / tile);
        for (int tz = 0; tz < nt; tz++)
            for (int tx = 0; tx < nt; tx++)
            {
                float x0 = mc.x - R + tx * tile, z0 = mc.z - R + tz * tile;
                var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>(); var uv = new List<Vector2>();
                // 바닥판(흔들림 0, 3m 격자) — 구름 그림자·물결 밝기 받는 땅
                const float g = 3f; int ng = Mathf.RoundToInt(tile / g);
                for (int iz = 0; iz < ng; iz++)
                    for (int ix = 0; ix < ng; ix++)
                    {
                        float gx = x0 + ix * g, gz = z0 + iz * g;
                        if (new Vector2(gx + g * .5f - mc.x, gz + g * .5f - mc.z).magnitude > R || !Ok(gx + g * .5f, gz + g * .5f, out _)) continue;
                        int b0 = v.Count;
                        foreach (var (ox, oz) in new[] { (0f, 0f), (g, 0f), (0f, g), (g, g) }) { v.Add(new Vector3(gx + ox, TerrainH(gx + ox, gz + oz) + .04f, gz + oz)); col.Add(new Color(.85f, .85f, .85f, 0f)); uv.Add(Vector2.zero); }
                        tri.AddRange(new[] { b0, b0 + 2, b0 + 1, b0 + 1, b0 + 2, b0 + 3 });
                    }
                // 풀잎: 0.6m² 당 하나
                int n = Mathf.RoundToInt(tile * tile / .6f);
                for (int i = 0; i < n; i++)
                {
                    float x = x0 + (float)rng.NextDouble() * tile, z = z0 + (float)rng.NextDouble() * tile;
                    if (new Vector2(x - mc.x, z - mc.z).magnitude > R || !Ok(x, z, out float h)) continue;
                    float yaw = (float)rng.NextDouble() * Mathf.PI * 2f, w = .25f, ht = .55f + (float)rng.NextDouble() * .35f;
                    var side = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw)) * (w * .5f);
                    var bp = new Vector3(x, h, z); float tint = .85f + (float)rng.NextDouble() * .3f;
                    int b0 = v.Count;
                    v.Add(bp - side); v.Add(bp + side); v.Add(bp + Vector3.up * ht + side * .2f);
                    col.Add(new Color(tint, tint, tint, 0f)); col.Add(new Color(tint, tint, tint, 0f)); col.Add(new Color(tint, tint, tint, 1f));
                    uv.Add(Vector2.zero); uv.Add(Vector2.zero); uv.Add(Vector2.up);
                    tri.Add(b0); tri.Add(b0 + 2); tri.Add(b0 + 1); blades++;
                }
                if (v.Count == 0) continue;
                var go = MeshGo(parent, "MeadowTile", v, uv, tri, mat, false, tz * 100 + tx);
                var mesh = go.GetComponent<MeshFilter>().sharedMesh; mesh.SetColors(col);
                var nrm = new Vector3[v.Count]; for (int k = 0; k < nrm.Length; k++) nrm[k] = Vector3.up; mesh.normals = nrm;   // 풀은 위를 향한 면처럼 밝힌다
                EditorUtility.SetDirty(mesh);
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tiles++;
            }
        Blockouts.Add("Meadow — 들판 풀잎 " + blades + "장/" + tiles + "타일(코드 삼각형 풀, WindGrass 셰이더)");
    }

    /// <summary>구름(천천히 흐름 + 들판 구름 그림자) · 바람 소리 · 정점 카메라 연출 자리.</summary>
    static void Atmosphere(Material grey)
    {
        var atm = Group("Atmosphere");
        var clouds = new GameObject("Clouds"); clouds.transform.SetParent(atm);
        var cd = clouds.AddComponent<CloudDrift>(); Frame(JoinS, out var jc, out _, out _); cd.areaCenter = new Vector3(jc.x, 0, jc.z);
        var white = NewMat("Proto_Cloud", new Color(1f, .98f, .95f)); white.SetFloat("_FaceShade", .35f);
        var rng = new System.Random(7);
        for (int i = 0; i < 9; i++)
        {
            var cc = new Vector3(jc.x + ((float)rng.NextDouble() * 2f - 1f) * 700f, 140f + (float)rng.NextDouble() * 40f, jc.z + ((float)rng.NextDouble() * 2f - 1f) * 700f);
            var cl = new GameObject("CloudCluster").transform; cl.SetParent(clouds.transform); cl.position = cc;
            int puffs = 3 + rng.Next(3);
            for (int k = 0; k < puffs; k++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Sphere); p.name = "BLOCKOUT_CloudPuff"; p.transform.SetParent(cl);
                float sz = 28f + (float)rng.NextDouble() * 26f;
                p.transform.position = cc + new Vector3(((float)rng.NextDouble() * 2f - 1f) * 40f, (float)rng.NextDouble() * 8f, ((float)rng.NextDouble() * 2f - 1f) * 25f);
                p.transform.localScale = new Vector3(sz * 1.6f, sz * .45f, sz);
                Object.DestroyImmediate(p.GetComponent<Collider>());
                var rr = p.GetComponent<Renderer>(); rr.sharedMaterial = white; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rr.receiveShadows = false;
            }
        }
        Blockouts.Add("Clouds — 납작한 흰 공 구름 9무리(천천히 흐름)");
        var wind = new GameObject("WindAmbience").AddComponent<WindAmbience>(); wind.transform.SetParent(atm);
        wind.lowY = 6f; wind.highY = 20f;   // 해안도로(y −2) 거의 무음 → 정점·들판(y 18~21 + 카메라 높이) 최대
        Frame(SegStart(11) + 30f, out var oc, out _, out _);
        var zone = new GameObject("OverlookFramingZone").AddComponent<CameraFramingZone>(); zone.transform.SetParent(atm); zone.transform.position = oc;
    }

    static void Guardrail(Transform parent, float s0, float s1, int side, Material mat)
    {
        if (s1 <= s0) return;
        float d = side * (HalfRoad + Walk - .15f);
        Strip(parent, "Guardrail", s0, s1, s => side < 0
            ? new[] { V(d, .55f), V(d, .95f), V(d + .15f, .95f), V(d + .15f, .55f) }
            : new[] { V(d, .55f), V(d, .95f), V(d - .15f, .95f), V(d - .15f, .55f) }, mat, true);
        for (float s = s0; s <= s1; s += 2.5f) Block(parent, "RailPost", new Vector3(.12f, .95f, .12f), s, d, .15f + .475f, 0, mat, false);
    }

    static GameObject Prefab(Transform parent, string name, float s, float d, float h, float yaw)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Lofi + name + ".prefab");
        if (src == null) { if (!Blockouts.Contains(name + " — 프리팹 없음")) Blockouts.Add(name + " — 프리팹 없음"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        Frame(s, out var c, out var f, out var r);
        go.transform.position = c + r * d + Vector3.up * h; go.transform.rotation = Quaternion.LookRotation(f) * Quaternion.Euler(0, yaw, 0);
        return go;
    }

    /// <summary>집 밑 평평한 받침(땅이 비탈일 때 집이 뜨지 않게). 윗면 = 길 높이 −0.3.</summary>
    static void Pad(Transform parent, float s, float d, float w, float depth, Material mat)
    {
        Frame(s, out var c, out var f, out var r);
        var top = c + r * d + Vector3.up * -.3f;
        Box(parent, "Pad", new Vector3(w, 10f, depth), top - Vector3.up * 5f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, mat, true);
    }

    static void Tree(Transform parent, float s, float d, Material leaf)
    {
        Frame(s, out var c, out var f, out var r); var p = c + r * d;
        Box(parent, "BLOCKOUT_AutumnTrunk", new Vector3(.35f, 2.2f, .35f), p + Vector3.up * .8f, 0, leaf, false);
        var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "BLOCKOUT_AutumnCrown"; crown.transform.SetParent(parent);
        crown.transform.position = p + Vector3.up * 2.8f; crown.transform.localScale = new Vector3(2.6f, 2.2f, 2.6f);
        Object.DestroyImmediate(crown.GetComponent<Collider>()); crown.GetComponent<Renderer>().sharedMaterial = leaf;
    }

    static GameObject Block(Transform parent, string name, Vector3 size, float s, float d, float h, float yaw, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        Frame(s, out var c, out var f, out var r);
        go.transform.position = c + r * d + Vector3.up * h; go.transform.rotation = Quaternion.LookRotation(f) * Quaternion.Euler(0, yaw, 0); go.transform.localScale = size;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat; return go;
    }

    static void Box(Transform parent, string name, Vector3 size, Vector3 pos, float yaw, Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = size;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void Disc(Transform parent, string name, Vector3 c, float radius, Material mat)
    {
        var verts = new List<Vector3> { c }; var uvs = new List<Vector2> { Vector2.zero }; var tris = new List<int>();
        const int seg = 40;
        for (int i = 0; i <= seg; i++) { float a = i / (float)seg * Mathf.PI * 2f; verts.Add(c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius); uvs.Add(new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius); }
        for (int i = 1; i <= seg; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
        MeshGo(parent, name, verts, uvs, tris, mat, true, 0);
    }

    static void Hill(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    /// <summary>처진 전선: 두 토막(가운데가 sag 만큼 내려감).</summary>
    static void Wire(Transform parent, Vector3 a, Vector3 b, float sag, Material mat)
    {
        var mid = (a + b) * .5f + Vector3.down * sag;
        foreach (var seg in new[] { (a, mid), (mid, b) })
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Wire"; go.transform.SetParent(parent);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var v = seg.Item2 - seg.Item1; go.transform.position = (seg.Item1 + seg.Item2) * .5f; go.transform.rotation = Quaternion.LookRotation(v);
            go.transform.localScale = new Vector3(.04f, .04f, v.magnitude); go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    static Bounds Bounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    static Material M(string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) Debug.LogWarning("[ProtoCourse] 머티리얼 없음: " + path);
        return m;
    }

    static Material NewMat(string name, Color tint)
    {
        string p = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("PitCrew/LofiPilot/Surface")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_Tint", tint); m.SetFloat("_FaceShade", .65f); EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>씬의 월드 셰이더 머티리얼(안개 속성 가진 것)을 MatDir 사본으로 교체. 같은 원본끼리 사본 하나를 공유.</summary>
    static void RemapToCopies()
    {
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || !m.HasProperty("_FogColor") || !m.HasProperty("_Ambient")) continue;
                string ap = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(ap) || ap.StartsWith(MatDir)) continue;
                if (!copies.TryGetValue(m, out var cp))
                {
                    string p = MatDir + "/" + m.name + ".mat";
                    AssetDatabase.DeleteAsset(p);
                    cp = new Material(m) { name = m.name }; AssetDatabase.CreateAsset(cp, p); copies[m] = cp;
                }
                mats[i] = cp; changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }
}
