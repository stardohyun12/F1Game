using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

/// <summary>
/// D-132 1단계 — 언덕 위 도착 구간 200m 슬라이스 (리서치 §6-2 순서 1~3, 4 맛보기).
///  1) 지형 먼저: HilltopHeight(손 높이 함수, 조절점 7개)
///  2) 길: SplineContainer 하나에 스플라인 2개(본선 = 오르막→꼭대기→지름길 헤어핀 하강, 우회 = 꼭대기 갈림목에서 왼쪽)
///  3) 길 얹기: SplineTown.CutFill 규칙을 함수 수준으로 옮겨 지형을 길에 맞춰 깎고 메움 + 높이 차가 문턱 넘는 곳만 옹벽
///  4) 맛보기: 지형 높이를 읽어 받침을 앉히는 건물 덩어리
/// ComplexTrack 을 복사해 시스템(주문판·포장·차·카메라)만 쓰고 월드는 지운다(Proto 씬과 같은 방식).
/// </summary>
public static class BuildHilltopSlice
{
    const string SrcScene = "Assets/ComplexTrack.unity";
    const string ScenePath = "Assets/Scenes/Slice_Hilltop.unity";
    const string MatDir = "Assets/Scenes/Slice_Hilltop_Mats";
    const string OrderDir = "Assets/Data/Orders/Hilltop";
    const float SeaY = -30f;   // 2차: −10 → −30. 꼭대기 노면 14.5 = 해면 위 44.5m (PD 가설 40~60m)

    // ── 길 단면 (가설) ──
    // 차로 4.8m(반폭 2.4) + 보도 0.6m = 단면 반폭 3.0. 어깨 끝 3.2, 옹벽은 어깨 끝 + 0.4 = 중심에서 3.6m → 벽 사이 7.2m(좁은 오르막).
    // 길 옆 평탄 띠 = 3.0~3.6m(≤ 6m 규칙), 그 너머 블렌드 6m 뒤 원래 지형.
    const float HalfLane = 2.4f, HalfSec = 3.0f, EdgeD = 3.2f, WallSetback = 0.4f, Blend = 6f, WallThreshold = 1.5f;

    // ── 지형 조절점 (HilltopHeight) — 2차 재배치 ──
    // 1차 판정: 꼭대기가 평평한 풀밭이고 바다는 오른쪽 끝 한 줄(TRUNK CAM 패널 뒤). 열림이 안 보임.
    // 2차 그림: 오르막(북쪽으로) → 꼭대기 모서리에서 길이 오른쪽으로 꺾여 "모서리 길"을 달린다. 길 바로 왼쪽(북)이 절벽면.
    //   차가 모서리에 서는 순간 정면~왼쪽 아래로 절벽면 → 계단식 마을 → 바다가 한꺼번에 열린다. 열린 쪽 가장자리가 카메라 가까이 있어야
    //   아래가 보인다(가장자리가 멀면 평지가 아래 풍경을 가린다 — 1차 꼭대기 평지 30m 가 그랬다).
    const float Plateau = 14.5f;      // ① 꼭대기 노면
    const float RampZ0 = -10f, RampLen = 150f, RampBase = 1.5f;   // ② 오르막: z −10 → 140 에서 1.5 → 14.5 (노면을 따라가게 — 1차는 노면보다 5m 높아 옹벽 협곡)
    const float TerraceX = -14f, TerraceW = 10f, TerraceH = 2.5f; // ③ 오르막 왼쪽 계단식 단(최대 2단) — 꼭대기 전 z 130~150 에서 사라짐
    // ④ 절벽 모서리 — 2차 첫 시도(모서리를 길에서 7m+ 떨어뜨림)는 미리보기에서 평지만 보였다: 카메라가 노면 1.9m 위라
    //   모서리가 멀면 아래가 전부 가려진다. 그래서 모서리를 길 어깨에 붙인다 — 북쪽 모서리 = 모서리 길 북쪽 어깨선, 서쪽 모서리 = 꼭대기 길 왼쪽 x −4.5.
    static readonly Vector2[] NorthEdge = { new Vector2(-60f, 166f), new Vector2(-4.5f, 168f), new Vector2(11f, 170.2f), new Vector2(34f, 174.2f), new Vector2(65f, 176.4f), new Vector2(95f, 183.4f), new Vector2(106f, 186.2f), new Vector2(200f, 210f) };
    // 서쪽 모서리 = 꼭대기 길 왼쪽 어깨 1.4m 밖 (z 별 x). 처음엔 x −4.5 고정이라 길 옆 블렌드 6m 가 절벽 머리를 풀 둔덕으로 덮었다(미리보기).
    static readonly Vector2[] WestEdge = { new Vector2(120f, -7.2f), new Vector2(138f, -5.4f), new Vector2(150f, -3.6f), new Vector2(158f, -0.8f), new Vector2(165f, 3.5f), new Vector2(175f, 12f) };
    const float WestEdgeZ0 = 128f;  // 서쪽 모서리는 z 128~138 에서 생긴다 — 오르막 끝까지는 왼쪽이 막혀 있다
    const float CliffH = 7f, CliffRun = 4f, SlopeAfter = 0.12f;  // ⑤ 모서리 5m 안에 7m 떨어지고, 그 뒤 12% 계단 비탈(마을)
    const float TownDrop = 14f, TownStart = 35f, TownRun = 40f, ShoreE = 80f;    // ⑥ 모서리에서 35m 넘으면 40m 동안 14m 더, 80m 에서 해안 절벽 → 바다
    //   (처음 135m 였을 때 실게임 캡처에서 바다가 지평선 바로 밑 한 줄이었다 — 바다를 가까이 당겨 화면 아래쪽까지 채운다)
    // ⑦' 3차: 꼭대기 너머 내리막 — z 138 → 182 에서 2.5m 낮춘다(최대 경사 약 9%; 3.5m/40m 는 꼭대기 직후 차가 떠서 유리 −2). 평지 그대로면 앞 모서리가 카메라(노면 위 1.9m)와
    //   거의 같은 높이라 아래 마을·바다를 가렸다(레이 측정: 바다+마을 전체 화면 약 18%). 앞이 내려가면 모서리가 시선 밑으로 숨는다.
    const float CrestRoll = 2.5f, CrestRollZ0 = 138f, CrestRollLen = 44f;
    const float EastHill = 22f;                      // ⑧ 동쪽(x>140) 언덕 — 헤어핀 뒤 막이
    static readonly Vector3 RingC = new Vector3(0f, 0f, 180f);  // ⑨ 먼 산 고리 중심 — 반경 380m 넘어 솟아 판 끝을 가린다

    static float S(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
    static float XW(float z) => Pl(WestEdge, z);
    static float ZN(float x) => Pl(NorthEdge, x);
    static float Pl(Vector2[] e, float x)
    {
        if (x <= e[0].x) return e[0].y;
        for (int i = 1; i < e.Length; i++)
            if (x <= e[i].x) return Mathf.Lerp(e[i - 1].y, e[i].y, (x - e[i - 1].x) / (e[i].x - e[i - 1].x));
        return e[e.Length - 1].y;
    }
    /// <summary>모서리 밖으로 e m 나간 곳이 꼭대기보다 얼마나 낮은가.</summary>
    static float Drop(float e) => e <= 0f ? 0f : CliffH * S(e / CliffRun) + SlopeAfter * Mathf.Max(0f, e - 2.5f) + TownDrop * S((e - TownStart) / TownRun) + 16f * S((e - ShoreE) / 12f);
    /// <summary>꼭대기 높이보다 9m 넘게 낮은 곳 = 마을 자리(모서리 윗부분은 비워 열림을 안 막는다).</summary>
    static float DropAt(float x, float z) => Mathf.Max(Drop(z - ZN(x)), Drop(XW(z) - x) * S((z - WestEdgeZ0) / 10f));
    public static float HilltopHeight(float x, float z)
    {
        float h = RampBase + (Plateau - RampBase) * S((z - RampZ0) / RampLen);
        h -= CrestRoll * S((z - CrestRollZ0) / CrestRollLen);
        if (x < TerraceX) h += Mathf.Floor(Mathf.Clamp((TerraceX - x) / TerraceW, 0f, 2f)) * TerraceH * (1f - S((z - 130f) / 20f));
        h -= DropAt(x, z);
        h += EastHill * S((x - 140f) / 45f) + 15f * S((-z - 10f) / 40f);
        float r = new Vector2(x - RingC.x, z - RingC.z).magnitude;
        if (r > 380f)
        {
            float th = Mathf.Atan2(z - RingC.z, x - RingC.x);
            float n = 0.5f + 0.3f * Mathf.Sin(3f * th + 1f) + 0.2f * Mathf.Sin(7f * th + 2f) * Mathf.Sin(r * 0.02f);
            h += (45f + 40f * n) * S((r - 380f) / 160f);
        }
        return h;
    }

    // ── 스플라인 제어점 (x, y, z) ──
    // 본선 14점: 오르막 S자 → 갈림목(3, 우회는 오른쪽 평지로) → 꼭대기(4) → 모서리 우회전(5) → 모서리 길 동쪽(왼쪽이 절벽)
    //   → 헤어핀 좌회전(9~11) → 아래 길 서쪽 → 배달 문(꼭대기 모서리에서 정면 왼쪽 아래 ~65m)
    static readonly Vector3[] MainKnots =
    {
        new Vector3(0f, 1f, 0f), new Vector3(6f, 5.5f, 50f), new Vector3(-6f, 10.5f, 95f),
        new Vector3(-2f, 13.6f, 125f),   // 갈림목
        new Vector3(1f, 14.05f, 150f),    // 꼭대기 — 왼쪽(서쪽) 모서리가 여기서 열린다
        new Vector3(12f, 12.6f, 167f),   // 모서리 우회전 — 3차: 꼭대기 너머 내리막(CrestRoll)을 따라 1.8m 낮춤
        new Vector3(34f, 12.3f, 171f), new Vector3(65f, 12.0f, 173f), new Vector3(95f, 11.6f, 180f),
        // 헤어핀: 중심 (110,194) 반경 11m 원 위 3점, 반시계(좌회전). 1차 교훈: 꼭짓점 1점 AutoSmooth 는 반경 3.5m 로 꺾임
        new Vector3(110f, 11.0f, 183f), new Vector3(121f, 10.0f, 194f), new Vector3(110f, 8.8f, 205f),
        new Vector3(85f, 7.4f, 211f), new Vector3(48f, 5.8f, 216f),
    };
    static readonly Vector2 HairpinC = new Vector2(110f, 194f);
    // 우회 6점(갈림목 공유): 오른쪽 — 꼭대기 평지 남쪽을 동쪽으로(모서리 길과 25m 떨어짐)
    static readonly Vector3[] DetourKnots =
    {
        new Vector3(-2f, 13.6f, 125f), new Vector3(10f, 14.1f, 140f), new Vector3(38f, 14.3f, 147f), new Vector3(72f, 14.3f, 147f), new Vector3(104f, 14.1f, 141f), new Vector3(130f, 13.6f, 132f),
    };
    const int ForkKnot = 3;

    // 지형 격자 — 정밀(길 깎기용, 2m)과 먼 판(8m). 경계는 8 의 배수로 맞춰 먼 판 꼭짓점과 겹친다.
    const float X0 = -144f, Z0 = -32f, X1 = 144f, Z1 = 240f, Step = 2f;
    const float FarX0 = -704f, FarZ0 = -608f, FarX1 = 704f, FarZ1 = 800f, FarStep = 8f;

    static Transform root;
    static readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    static readonly List<string> log = new List<string>();

    [MenuItem("PitCrew/Slice/Build Hilltop Slice")]
    public static void Menu() { Debug.Log("[Hilltop] " + BuildScene()); }

    public static string BuildScene()
    {
        copies.Clear(); log.Clear();
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Slice_Hilltop_Mats");
        AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SrcScene, ScenePath)) return "copy failed";
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects())
            if (System.Array.IndexOf(WorldRoots, g.name) >= 0) Object.DestroyImmediate(g);
        root = new GameObject("Hilltop").transform;

        var asphalt = M("Assets/Art/StreetFurniture/Asphalt.mat");
        var pavement = M("Assets/Art/CoastalTown/Pavement.mat");
        var groundM = M("Assets/Art/CoastalTown/Ground.mat");
        var concrete = M("Assets/Art/CoastalTown/BoundaryConcrete.mat");
        var sea = M("Assets/Art/CoastalTown/Sea.mat");
        var grey = NewMat("Slice_Blockout", new Color(0.62f, 0.62f, 0.60f));
        var wallA = NewMat("Slice_HouseWall", new Color(0.86f, 0.83f, 0.76f));
        var wallB = NewMat("Slice_HouseWallB", new Color(0.78f, 0.70f, 0.62f));

        // ── 2) 스플라인 ──
        var splGo = new GameObject("RoadSplines"); splGo.transform.SetParent(root);
        var container = splGo.AddComponent<SplineContainer>();
        var sMain = container.Spline; sMain.Clear();
        for (int ki = 0; ki < MainKnots.Length; ki++)
        {
            var k = MainKnots[ki];
            if (ki >= 9 && ki <= 11)
            {
                // 헤어핀 3점: 원 접선을 직접 — 반시계 진행, 원 근사 Bezier 손잡이 k = 4/3·tan(90°/4)·11 ≈ 6.1m, 경사 8%
                float phi = Mathf.Atan2(k.z - HairpinC.y, k.x - HairpinC.x);
                var d = new float3(-Mathf.Sin(phi), -0.08f, Mathf.Cos(phi)) * 6.1f;
                sMain.Add(new BezierKnot(new float3(k.x, k.y, k.z), -d, d, quaternion.identity), TangentMode.Broken);
            }
            else sMain.Add(new BezierKnot(new float3(k.x, k.y, k.z)), TangentMode.AutoSmooth);
        }
        var sDet = container.AddSpline();
        for (int ki = 0; ki < DetourKnots.Length; ki++)
        {
            var k = DetourKnots[ki];
            if (ki == 0)
            {
                // 갈림목: 본선 진행 방향(앞뒤 제어점 차) 그대로 출발 — AutoSmooth 첫 점은 다음 점만 보고 꺾여 갈림목에서 꺾임
                var fd = (MainKnots[ForkKnot + 1] - MainKnots[ForkKnot - 1]); fd.y = 0; fd = fd.normalized * 6f;
                var t3 = new float3(fd.x, 0f, fd.z);
                sDet.Add(new BezierKnot(new float3(k.x, k.y, k.z), -t3, t3, quaternion.identity), TangentMode.Broken);
            }
            else sDet.Add(new BezierKnot(new float3(k.x, k.y, k.z)), TangentMode.AutoSmooth);
        }
        try { container.LinkKnots(new SplineKnotIndex(0, ForkKnot), new SplineKnotIndex(1, 0)); } catch (System.Exception e) { log.Add("link 실패 " + e.Message); }

        var main = Sample(sMain, 1f); var det = Sample(sDet, 1f);
        float lenMain = Len(main), lenDet = Len(det);
        log.Add($"길 본선 {lenMain:F0}m(제어점 {MainKnots.Length}) 우회 {lenDet:F0}m(제어점 {DetourKnots.Length})");
        log.Add("커브 반경(제어점, m) 본선 " + string.Join("/", Radii(MainKnots)) + " | 우회 " + string.Join("/", Radii(DetourKnots)));
        log.Add($"실제 최소 반경(1m 샘플, 앞뒤 4m) 본선 {MinRadius(main, out var mrp):F1}m @({mrp.x:F0},{mrp.z:F0}) 우회 {MinRadius(det, out _):F1}m");
        log.Add($"최대 경사 본선 {MaxGrade(main):P0} 우회 {MaxGrade(det):P0}");

        // ── 3) 길 얹기: 깎기/메우기 + 옹벽 ──
        var polys = new List<List<Vector3>> { main, det };
        var walls = new List<(Vector3 a, Vector3 b, float topA, float topB, float botA, float botB)>();
        var terr = CutFill(polys, walls, out float maxCut);
        // D-133: 계단 지름길 자리를 지형에서 파낸다(계단이 흙에 묻히지 않게) + 밭·주차 자리는 마을이 비운다
        StairPath(main, out stairS, out stairE);
        CarveStairs(terr, stairS, stairE);
        keepOut.Clear();
        keepOut.Add(new Vector3(FieldRect.center.x, FieldRect.center.y, 13f));
        keepOut.Add(new Vector3(stairS.x * .5f + stairE.x * .5f, stairS.z * .5f + stairE.z * .5f, 12f));
        log.Add($"지형 격자 {terr.GetLength(0)}x{terr.GetLength(1)} @{Step}m, 길 밑 최대 깎기/메우기 {maxCut:F1}m, 옹벽 {walls.Count}조각 {walls.Sum(w => Vector3.Distance(w.a, w.b)):F0}m");

        var gT = Group("Terrain");
        TerrainMesh(gT, terr, groundM);
        FarTerrainMesh(gT, groundM);
        var gR = Group("Road");
        RoadMesh(gR, "Road_Main", main, asphalt, pavement, 0f);
        RoadMesh(gR, "Road_Detour", det, asphalt, pavement, 0.002f);
        var gW = Group("RetainingWalls");
        WallMesh(gW, walls, concrete);

        var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane); seaGo.name = "Sea"; seaGo.transform.SetParent(root);
        seaGo.transform.position = new Vector3(0f, SeaY, 200f); seaGo.transform.localScale = new Vector3(140f, 1f, 140f);
        Object.DestroyImmediate(seaGo.GetComponent<Collider>()); seaGo.GetComponent<Renderer>().sharedMaterial = sea;

        // ── 4) 건물: 오르막은 길에 붙은 집 줄, 아래는 계단식 마을 ──
        var gB = Group("Buildings");
        var roofM = NewMat("Slice_Roof", new Color(0.70f, 0.42f, 0.33f));
        var propM = NewMat("Slice_Prop", new Color(0.30f, 0.32f, 0.36f));
        var bikeM = NewMat("Slice_Bike", new Color(0.62f, 0.16f, 0.14f));
        var potM = NewMat("Slice_Pot", new Color(0.72f, 0.40f, 0.24f));
        int nb = Buildings(gB, main, terr, new[] { wallA, wallB }, concrete, propM, bikeM, potM, out string bLog);
        var pk = ParkingSpot(main, out _); keepOut.Add(new Vector3(pk.x, pk.z, 9f));
        int nt = Town(gB, terr, polys, new[] { wallA, wallB, roofM, concrete });
        log.Add($"오르막 건물 {nb}채({bLog}), 아래 마을 덩어리 {nt}채");
        log.Add("D-133 " + LifeDetails(Group("LifeDetails"), gB, main, det, terr, concrete, groundM, bikeM));

        // ── 배달 문 + 사람 실루엣: 아래 길 끝 북쪽 집, 문은 남쪽(꼭대기 쪽)을 본다 — 꼭대기에서 정면 왼쪽 아래 ~80m ──
        Vector3 end = main[main.Count - 1], endF = (main[main.Count - 1] - main[main.Count - 6]); endF.y = 0; endF.Normalize();
        Vector3 endL = new Vector3(-endF.z, 0f, endF.x);            // 진행 방향 왼쪽 = 북쪽(서쪽으로 가니까)
        if (endL.z < 0) endL = -endL;
        Vector3 doorP = end + endL * (EdgeD + 1.2f); float doorY = Hs(terr, doorP.x, doorP.z);
        var houseRot = Quaternion.LookRotation(-endL);
        var doorM = NewMat("Slice_DeliveryDoor", new Color(1f, 0.42f, 0.08f));
        var personM = NewMat("Slice_PersonDark", new Color(0.10f, 0.12f, 0.20f));
        float hb = Mathf.Min(doorY, end.y) - 0.4f;
        Box(gB, "BLOCKOUT_DeliveryHouse", new Vector3(9f, 7f + (doorY - hb), 7f), doorP + endL * 3.6f + Vector3.up * (hb + (7f + doorY - hb) * .5f), houseRot, wallA, true);
        Box(gB, "BLOCKOUT_DeliveryDoor", new Vector3(1.6f, 2.4f, 0.2f), doorP + endL * 0.05f + Vector3.up * (doorY + 1.2f), houseRot, doorM, false);
        Box(gB, "BLOCKOUT_DeliveryAwning", new Vector3(3.2f, 0.25f, 1.2f), doorP - endL * 0.5f + Vector3.up * (doorY + 2.75f), houseRot, doorM, false);
        Person(gB, doorP - endL * 0.9f + houseRot * Vector3.right * 1.3f + Vector3.up * doorY, personM);
        Vector3 dEnd = det[det.Count - 1], dF = det[det.Count - 1] - det[det.Count - 6]; dF.y = 0; dF.Normalize();
        string stopLog = WireDelivery(new[] { ("HilltopDoor", end + endF * 2f), ("DetourEnd", dEnd + dF * 2f) });
        var catalog = BuildOrders();
        var ob = Object.FindFirstObjectByType<OrderBoard>(FindObjectsInactive.Include);
        if (ob != null) { var so = new SerializedObject(ob); so.FindProperty("catalog").objectReferenceValue = catalog; so.ApplyModifiedPropertiesWithoutUndo(); }
        var rc = Object.FindFirstObjectByType<RunController>(FindObjectsInactive.Include);
        if (rc != null) { var so = new SerializedObject(rc); so.FindProperty("runTimeLimitSec").floatValue = 150f; so.ApplyModifiedPropertiesWithoutUndo(); }

        // ── 차·카메라 ──
        var car = Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
        Vector3 sf = main[4] - main[0]; sf.y = 0; sf.Normalize();
        car.transform.position = main[2] + Vector3.up * .6f; car.transform.rotation = Quaternion.LookRotation(sf);
        var cso = new SerializedObject(car);
        float lowest = Mathf.Min(main.Min(p => p.y), det.Min(p => p.y));
        cso.FindProperty("fallRespawnY").floatValue = lowest - 7f;          // 가장 낮은 길(≈1) − 7 = −6, 해수면 −10 위
        cso.FindProperty("safePointAboveFall").floatValue = 4f;
        cso.FindProperty("freeDrive").boolValue = false;
        cso.ApplyModifiedPropertiesWithoutUndo();
        {
            var cl = new GameObject("CourseCenterline").AddComponent<CourseCenterline>(); cl.transform.SetParent(root);
            var pts = new List<Vector3>(); var fws = new List<Vector3>();
            foreach (var poly in polys) for (int i = 0; i < poly.Count; i += 2) { pts.Add(poly[i]); fws.Add(Fwd(poly, i)); }
            cl.points = pts.ToArray(); cl.forwards = fws.ToArray(); EditorUtility.SetDirty(cl);
        }
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
        {
            // 3차: 꼭대기 카메라 구역 — 시선을 CrestDrop m 내려 피치 약 −4° → −13°. 갈림목 지나 걸리기 시작, 꼭대기(1,150)에서 다 숙이고,
            //   모서리 길 x≈30 까지 유지 후 x≈46 에서 풀림(꼭대기 넘은 뒤 약 40m). 조작은 안 건드린다(D-131).
            var zgo = new GameObject("CameraFramingZone_Crest"); zgo.transform.SetParent(root); zgo.transform.position = CrestZoneC;
            var zone = zgo.AddComponent<CameraFramingZone>(); zone.radius = CrestZoneR; zone.drop = CrestDrop; zone.rate = 1.3f;
        }

        RemapToCopies();
        // 무드: PathA 사본에 안개만 멀리 — 원본 75~240m 면 150~400m 아래 마을·바다가 안개색으로 사라진다.
        //   140m 부터 끼기 시작해 620m 에 다 덮음 → 마을은 보이고, 380m 넘는 먼 산 고리는 옅은 실루엣, 판 끝은 안개 속.
        var moodSrc = AssetDatabase.LoadAssetAtPath<MoodProfile>(MoodSwitcher.PathA);
        MoodProfile mood = null;
        if (moodSrc != null)
        {
            string mp = MatDir + "/HilltopMood.asset";
            AssetDatabase.DeleteAsset(mp);
            mood = Object.Instantiate(moodSrc); mood.fogStart = 140f; mood.fogEnd = 620f; mood.fogDensity = 0.0016f;
            AssetDatabase.CreateAsset(mood, mp);
            var mc = Object.FindFirstObjectByType<MoodController>(FindObjectsInactive.Include);
            if (mc != null) { var mso = new SerializedObject(mc); mso.FindProperty("profile").objectReferenceValue = mood; mso.ApplyModifiedPropertiesWithoutUndo(); }
            log.Add("무드 " + (mc != null ? "MoodController 연결" : "MoodController 없음(에디트 적용만)"));
        }
        var sunL = GameObject.Find("Directional Light")?.GetComponent<Light>();
        var volGo = GameObject.Find("PrototypeVolume");
        var vol = volGo != null ? volGo.GetComponent<UnityEngine.Rendering.Volume>() : null;
        if (mood != null) MoodController.ApplyProfile(mood, sunL, vol);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        return "built " + ScenePath + " | " + string.Join(" | ", log) + " | " + stopLog;
    }

    static readonly string[] WorldRoots = { "Ground", "Track", "RoadLines", "Sidewalks", "Props", "StreetFurniture", "StreetVehicles", "HarborSea", "Buildings", "ZoneGrounds", "CoastalTown", "MapBoundary", "ZoneUnlockSystem" };

    // ------------------------------------------------------------------ 스플라인 샘플
    /// <summary>스플라인을 잘게 평가한 뒤 호 길이 step 간격으로 다시 뽑는다.</summary>
    static List<Vector3> Sample(Spline sp, float step)
    {
        var fine = new List<Vector3>(); int n = 4000;
        for (int i = 0; i <= n; i++) { float3 p = SplineUtility.EvaluatePosition(sp, i / (float)n); fine.Add(new Vector3(p.x, p.y, p.z)); }
        var outp = new List<Vector3> { fine[0] }; float acc = 0f;
        for (int i = 1; i < fine.Count; i++)
        {
            float d = Vector3.Distance(fine[i - 1], fine[i]); acc += d;
            if (acc >= step) { outp.Add(fine[i]); acc = 0f; }
        }
        if (Vector3.Distance(outp[outp.Count - 1], fine[fine.Count - 1]) > 0.3f) outp.Add(fine[fine.Count - 1]);
        return outp;
    }
    static float Len(List<Vector3> p) { float l = 0; for (int i = 1; i < p.Count; i++) l += Vector3.Distance(p[i - 1], p[i]); return l; }
    static float MaxGrade(List<Vector3> p)
    {
        float g = 0; for (int i = 5; i < p.Count; i += 5) { var a = p[i - 5]; var b = p[i]; float dxz = new Vector2(b.x - a.x, b.z - a.z).magnitude; if (dxz > .1f) g = Mathf.Max(g, Mathf.Abs(b.y - a.y) / dxz); }
        return g;
    }
    /// <summary>제어점 셋(앞·자신·뒤)을 지나는 원의 반경(XZ). 근사치 — 같은 반경 연속 2회 이하 확인용.</summary>
    static List<string> Radii(Vector3[] k)
    {
        var r = new List<string>();
        for (int i = 1; i + 1 < k.Length; i++)
        {
            Vector2 a = new Vector2(k[i - 1].x, k[i - 1].z), b = new Vector2(k[i].x, k[i].z), c = new Vector2(k[i + 1].x, k[i + 1].z);
            float ab = (b - a).magnitude, bc = (c - b).magnitude, ca = (a - c).magnitude;
            float cross = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
            r.Add(cross < 1e-3f ? "직선" : (ab * bc * ca / (2f * cross)).ToString("F0"));
        }
        return r;
    }
    static float MinRadius(List<Vector3> p, out Vector3 at)
    {
        float m = float.MaxValue; at = p[0];
        for (int i = 4; i + 4 < p.Count; i++)
        {
            Vector2 a = new Vector2(p[i - 4].x, p[i - 4].z), b = new Vector2(p[i].x, p[i].z), c = new Vector2(p[i + 4].x, p[i + 4].z);
            float cross = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)); if (cross < 1e-4f) continue;
            float r = (b - a).magnitude * (c - b).magnitude * (a - c).magnitude / (2f * cross);
            if (r < m) { m = r; at = p[i]; }
        }
        return m;
    }
    static Vector3 Fwd(List<Vector3> p, int i)
    {
        var f = p[Mathf.Min(i + 1, p.Count - 1)] - p[Mathf.Max(i - 1, 0)]; f.y = 0; return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
    }

    // ------------------------------------------------------------------ 깎기/메우기 (SplineTown.CutFill 규칙 이식)
    struct Seg { public Vector2 a, b; public float ya, yb; public bool wallL, wallR; public int poly; }

    /// <summary>
    /// SplineTown.CutFill(TownGen/Core/SplineTown.cs:1982) 의 규칙을 함수 수준으로 옮김:
    ///  d ≤ 단면 반폭 + 0.15: 길 밑 — 길 높이 −0.10 (길 메시 아래 숨김)
    ///  d ≤ 어깨 끝: 길 높이로 깎기·메우기
    ///  옹벽 쪽은 옹벽 자리까지 평평, 그 너머 원래 지형. 아니면 블렌드 6m(smoothstep)로 원래 지형.
    /// 옹벽 = 옹벽 자리 원래 지형과 길 높이 차 > WallThreshold. 급커브 안쪽에서 접히는 조각·다른 길 단면 위 조각은 세우지 않는다.
    /// 공간 해시 대신 조각 bbox 로 거른다(조각 수백 개라 충분).
    /// </summary>
    static float[,] CutFill(List<List<Vector3>> polys, List<(Vector3 a, Vector3 b, float topA, float topB, float botA, float botB)> walls, out float maxCut)
    {
        int nx = Mathf.RoundToInt((X1 - X0) / Step) + 1, nz = Mathf.RoundToInt((Z1 - Z0) / Step) + 1;
        var orig = new float[nx, nz]; var t = new float[nx, nz];
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) t[i, j] = orig[i, j] = HilltopHeight(X0 + i * Step, Z0 + j * Step);

        var segs = new List<Seg>(); int fold = 0, clipped = 0;
        for (int pi = 0; pi < polys.Count; pi++)
        {
            var p = polys[pi]; int n = p.Count;
            var wl = new bool[n]; var wr = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var f = Fwd(p, i); var nl = new Vector3(-f.z, 0, f.x);   // 왼쪽 법선
                float wd = EdgeD + WallSetback;
                float gl = HilltopHeight(p[i].x + nl.x * wd, p[i].z + nl.z * wd), gr = HilltopHeight(p[i].x - nl.x * wd, p[i].z - nl.z * wd);
                wl[i] = Mathf.Abs(gl - p[i].y) > WallThreshold; wr[i] = Mathf.Abs(gr - p[i].y) > WallThreshold;
            }
            for (int i = 0; i + 1 < n; i++)
            {
                segs.Add(new Seg { a = new Vector2(p[i].x, p[i].z), b = new Vector2(p[i + 1].x, p[i + 1].z), ya = p[i].y, yb = p[i + 1].y, wallL = wl[i] || wl[i + 1], wallR = wr[i] || wr[i + 1], poly = pi });
                foreach (int side in new[] { 1, -1 })
                {
                    bool w = side == 1 ? (wl[i] && wl[i + 1]) : (wr[i] && wr[i + 1]);
                    if (!w) continue;
                    var f0 = Fwd(p, i); var f1 = Fwd(p, i + 1);
                    var n0 = new Vector3(-f0.z, 0, f0.x); var n1 = new Vector3(-f1.z, 0, f1.x);
                    float wd = EdgeD + WallSetback;
                    var pa = p[i] + side * n0 * wd; var pb = p[i + 1] + side * n1 * wd;
                    var cd = p[i + 1] - p[i]; cd.y = 0; var wdv = pb - pa; wdv.y = 0;
                    if (Vector3.Dot(wdv, cd) <= 0.25f * cd.sqrMagnitude) { fold++; continue; }
                    float ta = p[i].y, tb = p[i + 1].y;
                    float ga = HilltopHeight(pa.x, pa.z), gb = HilltopHeight(pb.x, pb.z);
                    walls.Add((new Vector3(pa.x, 0, pa.z), new Vector3(pb.x, 0, pb.z), Mathf.Max(ta, ga), Mathf.Max(tb, gb), Mathf.Min(ta, ga) - 0.3f, Mathf.Min(tb, gb) - 0.3f));
                }
            }
        }
        // 다른 길(또는 같은 길의 다른 다리) 단면 위를 지나는 옹벽 조각은 지운다 — 갈림목·헤어핀에서 벽이 길을 가로지름 방지
        clipped = walls.RemoveAll(w =>
        {
            foreach (var q in new[] { w.a, (w.a + w.b) * .5f, w.b })
                foreach (var sg in segs)
                    if (DistSeg(new Vector2(q.x, q.z), sg.a, sg.b, out _) < HalfSec + 0.2f) return true;
            return false;
        });
        log.Add($"옹벽 접힘 생략 {fold}, 길 위 조각 제거 {clipped}");

        float reach = EdgeD + Mathf.Max(Blend, WallSetback);
        maxCut = 0f;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                var v = new Vector2(X0 + i * Step, Z0 + j * Step);
                float bestScore = float.MaxValue, bestD = 0, bestY = 0; bool bestWall = false, found = false;
                foreach (var sg in segs)
                {
                    if (v.x < Mathf.Min(sg.a.x, sg.b.x) - reach || v.x > Mathf.Max(sg.a.x, sg.b.x) + reach || v.y < Mathf.Min(sg.a.y, sg.b.y) - reach || v.y > Mathf.Max(sg.a.y, sg.b.y) + reach) continue;
                    float d = DistSeg(v, sg.a, sg.b, out float tt);
                    if (d > reach || d >= bestScore) continue;
                    bestScore = d; bestD = d; found = true; bestY = Mathf.Lerp(sg.ya, sg.yb, tt);
                    float cross = (sg.b.x - sg.a.x) * (v.y - sg.a.y) - (sg.b.y - sg.a.y) * (v.x - sg.a.x);
                    bestWall = cross > 0 ? sg.wallL : sg.wallR;
                }
                if (!found) continue;
                float o = orig[i, j], top = bestY, nh;
                float under = HalfSec + Mathf.Min(Step * 0.75f, (EdgeD - HalfSec) * 0.75f);
                if (bestD <= under) nh = bestY - 0.10f;
                else if (bestD <= EdgeD) nh = top;
                // 깎기 쪽 옹벽: 벽 뒤 격자 한 칸 대각선(2.9m)까지도 길 높이로 — 벽 안 낮은 점과 벽 뒤 높은 점을 잇는 삼각형이
                //   벽 앞으로 비스듬히 솟아 원뿔처럼 보였다(1차 Play 캡처). 솟는 면을 벽 뒤로 보낸다. 메우기 쪽은 원래대로.
                else if (bestWall) { if (bestD <= EdgeD + WallSetback || (o > top && bestD <= EdgeD + WallSetback + Step * 1.45f)) nh = top; else continue; }
                else if (bestD <= EdgeD + Blend) { float w = S((bestD - EdgeD) / Blend); nh = top + (o - top) * w; }
                else continue;
                t[i, j] = nh;
                if (bestD <= EdgeD) maxCut = Mathf.Max(maxCut, Mathf.Abs(nh - o));
            }
        return t;
    }

    static float DistSeg(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        var ab = b - a; float l2 = ab.sqrMagnitude;
        t = l2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
        return Vector2.Distance(p, a + ab * t);
    }

    /// <summary>깎은 지형 격자의 (x,z) 높이 — 쌍선형.</summary>
    static float Hs(float[,] t, float x, float z)
    {
        float fx = Mathf.Clamp((x - X0) / Step, 0, t.GetLength(0) - 1.001f), fz = Mathf.Clamp((z - Z0) / Step, 0, t.GetLength(1) - 1.001f);
        int i = (int)fx, j = (int)fz; float u = fx - i, w = fz - j;
        return Mathf.Lerp(Mathf.Lerp(t[i, j], t[i + 1, j], u), Mathf.Lerp(t[i, j + 1], t[i + 1, j + 1], u), w);
    }

    // ------------------------------------------------------------------ 메시
    static void TerrainMesh(Transform parent, float[,] t, Material mat)
    {
        int nx = t.GetLength(0), nz = t.GetLength(1);
        var verts = new List<Vector3>(nx * nz); var uvs = new List<Vector2>(nx * nz); var tris = new List<int>();
        for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) { float x = X0 + i * Step, z = Z0 + j * Step; verts.Add(new Vector3(x, t[i, j], z)); uvs.Add(new Vector2(x, z)); }
        for (int j = 0; j + 1 < nz; j++)
            for (int i = 0; i + 1 < nx; i++)
            {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
            }
        MeshGo(parent, "HilltopTerrain", verts, uvs, new[] { tris }, new[] { mat }, true);
    }

    /// <summary>
    /// 먼 판(8m 격자, 콜라이더 없음): 정밀 격자 바깥 −704~704 × −608~800. 정밀 격자 안쪽 칸은 건너뛰되 경계 한 줄은 겹쳐 깔고,
    /// 정밀 격자 안에 들어간 꼭짓점은 1m 내린다 — 경계 T자 이음매의 틈을 밑에서 막는다(길은 경계 근처에 없다).
    /// </summary>
    static void FarTerrainMesh(Transform parent, Material mat)
    {
        int nx = Mathf.RoundToInt((FarX1 - FarX0) / FarStep) + 1, nz = Mathf.RoundToInt((FarZ1 - FarZ0) / FarStep) + 1;
        var verts = new List<Vector3>(nx * nz); var uvs = new List<Vector2>(nx * nz); var tris = new List<int>();
        bool Inside(float x, float z, float m) => x > X0 + m && x < X1 - m && z > Z0 + m && z < Z1 - m;
        for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++)
            {
                float x = FarX0 + i * FarStep, z = FarZ0 + j * FarStep;
                verts.Add(new Vector3(x, HilltopHeight(x, z) - (Inside(x, z, 0.1f) ? 1f : 0f), z)); uvs.Add(new Vector2(x, z));
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

    /// <summary>길 띠: 단면 [−3.0 보도 | −2.4 차로 +2.4 | 보도 +3.0], 머티리얼 2개·콜라이더 한 장(0308 교훈: 겹친 콜라이더·이음매 금지).</summary>
    static void RoadMesh(Transform parent, string name, List<Vector3> p, Material lane, Material walk, float lift)
    {
        float[] cols = { -HalfSec, -HalfLane, HalfLane, HalfSec };
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tLane = new List<int>(); var tWalk = new List<int>();
        float s = 0;
        for (int i = 0; i < p.Count; i++)
        {
            if (i > 0) s += Vector3.Distance(p[i - 1], p[i]);
            var f = Fwd(p, i); var r = new Vector3(f.z, 0, -f.x);
            foreach (var c in cols) { verts.Add(p[i] + r * c + Vector3.up * lift); uvs.Add(new Vector2(c, s)); }
        }
        for (int i = 0; i + 1 < p.Count; i++)
            for (int k = 0; k < 3; k++)
            {
                int a = i * 4 + k, b = a + 1, c = a + 4, d = c + 1;
                var l = k == 1 ? tLane : tWalk;
                l.Add(a); l.Add(c); l.Add(b); l.Add(b); l.Add(c); l.Add(d);
            }
        MeshGo(parent, name, verts, uvs, new[] { tLane, tWalk }, new[] { lane, walk }, true);
    }

    static void WallMesh(Transform parent, List<(Vector3 a, Vector3 b, float topA, float topB, float botA, float botB)> walls, Material mat)
    {
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        const float th = 0.35f; float u = 0;
        foreach (var w in walls)
        {
            var d = w.b - w.a; float len = d.magnitude; if (len < 1e-3f) continue;
            var n = new Vector3(-d.z, 0, d.x) / len;   // 한쪽 법선 — 양면을 다 만드니 방향 무관
            Vector3 a0 = w.a, b0 = w.b, a1 = w.a + n * th, b1 = w.b + n * th;
            void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u0, float u1, float v0, float v1)
            {
                int bi = verts.Count; verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                uvs.Add(new Vector2(u0, v0)); uvs.Add(new Vector2(u1, v0)); uvs.Add(new Vector2(u0, v1)); uvs.Add(new Vector2(u1, v1));
                tris.Add(bi); tris.Add(bi + 2); tris.Add(bi + 1); tris.Add(bi + 1); tris.Add(bi + 2); tris.Add(bi + 3);
                tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2); tris.Add(bi + 1); tris.Add(bi + 3); tris.Add(bi + 2);   // 양면
            }
            Quad(a0 + Vector3.up * w.botA, b0 + Vector3.up * w.botB, a0 + Vector3.up * w.topA, b0 + Vector3.up * w.topB, u, u + len, w.botA, w.topA);
            Quad(a1 + Vector3.up * w.botA, b1 + Vector3.up * w.botB, a1 + Vector3.up * w.topA, b1 + Vector3.up * w.topB, u, u + len, w.botA, w.topA);
            Quad(a0 + Vector3.up * w.topA, b0 + Vector3.up * w.topB, a1 + Vector3.up * w.topA, b1 + Vector3.up * w.topB, u, u + len, 0, th);
            u += len;
        }
        MeshGo(parent, "RetainingWall", verts, uvs, new[] { tris }, new[] { mat }, true);
    }

    static GameObject MeshGo(Transform parent, string name, List<Vector3> verts, List<Vector2> uvs, List<int>[] subs, Material[] mats, bool collider)
    {
        var mesh = new Mesh { name = name, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.subMeshCount = subs.Length;
        for (int k = 0; k < subs.Length; k++) mesh.SetTriangles(subs[k], k);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string mp = MatDir + "/Mesh_" + name + ".asset";
        AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
        var go = new GameObject(name); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    // ------------------------------------------------------------------ 건물
    /// <summary>
    /// 오르막 양옆 집 줄(2차): 1차는 높이 5m 옹벽이 이어져 "수로/협곡"이었다(PD 판정). 이제 옹벽 대신 집 앞면이 길에 붙는다.
    ///  앞면 = 어깨 끝 + 0.4 + 물림 0~1m. 길이 6~9m, 높이 5~11m 들쭉날쭉. 4~6채마다 틈 하나(골목 2.5m / 계단 3.5m / 빈터 7m + 낮은 벽 1.5~3m).
    ///  받침 = 그 집 구간 노면·뒤 지형 중 낮은 쪽 −0.4. 소품(콜라이더 없음): 실외기·화분·오토바이.
    /// </summary>
    static int Buildings(Transform parent, List<Vector3> main, float[,] t, Material[] walls, Material concrete, Material propM, Material bikeM, Material potM, out string info)
    {
        int n = 0, nGap = 0, nAc = 0, nPot = 0, nBike = 0, nShop = 0;
        foreach (int side in new[] { 1, -1 })
        {
            // 오른쪽은 z 80 에서 끝(3차: 그 뒤 z 82~118 은 길이 산 어깨를 깎고 지나간 깎은 면 — CutBank),
            //   왼쪽은 꼭대기 직전 z 132 에서 끝 — 그 뒤로 왼쪽이 열린다
            int sEnd = 0; while (sEnd < main.Count - 1 && main[sEnd].z < (side > 0 ? BankZ0 - 2f : 132f)) sEnd++;
            var rnd = new System.Random(side > 0 ? 2051 : 2052);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            float s = side > 0 ? 5f : 8f; int count = 0, nextGap = rnd.Next(4, 7), gi = side > 0 ? 0 : 1;
            while (s < sEnd - 6)
            {
                bool gap = count == nextGap;
                float len = gap ? new[] { 2.5f, 3.5f, 7f }[gi % 3] : R(6f, 9f);
                if (s + len > sEnd) break;
                int i0 = Mathf.Clamp(Mathf.RoundToInt(s), 0, main.Count - 1), i1 = Mathf.Clamp(Mathf.RoundToInt(s + len), 0, main.Count - 1), im = (i0 + i1) / 2;
                var f = Fwd(main, im); var r = new Vector3(f.z, 0, -f.x) * side; var rot = Quaternion.LookRotation(f);
                float roadY = Mathf.Min(main[i0].y, main[i1].y);
                float sb = gap ? 0f : R(0f, 1f), front = EdgeD + 0.4f + sb;
                var mid = main[im]; mid.y = 0;
                if (!gap)
                {
                    float h = R(5f, 11f), d = 8f;
                    var c = mid + r * (front + d * .5f);
                    float baseY = roadY;
                    foreach (var sx in new[] { -.5f, .5f }) foreach (var sz in new[] { -.5f, .5f })
                        { var q = c + rot * new Vector3(sx * d, 0, sz * len); baseY = Mathf.Min(baseY, Hs(t, q.x, q.z)); }
                    baseY -= 0.4f; float top = main[i1].y + h;
                    Box(parent, "BLOCKOUT_AlleyHouse_" + n, new Vector3(d, top - baseY, len), c + Vector3.up * ((top + baseY) * .5f), rot, walls[n % walls.Length], true);
                    double p = rnd.NextDouble();
                    var faceP = mid + r * front;
                    if (p < 0.35) { Box(parent, "PROP_AC_" + nAc++, new Vector3(0.35f, 0.6f, 0.9f), faceP - r * 0.18f + f * R(-len * .3f, len * .3f) + Vector3.up * (main[im].y + R(2.6f, 3.6f)), rot, propM, false); }
                    else if (p < 0.6) { Cyl(parent, "PROP_FlowerPot_" + nPot++, new Vector3(0.45f, 0.22f, 0.45f), faceP - r * 0.3f + f * (len * .5f - 0.6f) + Vector3.up * (main[i1].y + 0.22f), potM); }
                    else if (p < 0.75) { Box(parent, "PROP_Motorbike_" + nBike++, new Vector3(0.55f, 0.9f, 1.7f), faceP - r * 0.35f + Vector3.up * (main[im].y + 0.45f), rot, bikeM, false); }
                    if (n % 6 == 2)
                    {
                        // D-133 가게: 차양 + 가게 앞 보도 얼룩(드나드는 발길이 닳게 한 곳만) — 얼룩은 경사 따라 기울인 얇은 판
                        var awn = NewMat("Slice_Awning", new Color(0.24f, 0.44f, 0.52f)); var stn = NewMat("Slice_Stain", new Color(0.40f, 0.38f, 0.35f));
                        Box(parent, "PROP_ShopAwning_" + nShop, new Vector3(1.1f, 0.12f, Mathf.Min(len - 1f, 4.5f)), faceP - r * 0.55f + Vector3.up * (main[im].y + 2.7f), rot, awn, false);
                        var slope = Quaternion.LookRotation(main[i1] - main[i0]);
                        Box(parent, "WEAR_ShopStain_" + nShop++, new Vector3(1.3f, 0.03f, Mathf.Min(len - 1.5f, 3.5f)), mid + r * (HalfSec - 0.1f) + Vector3.up * (main[im].y + 0.02f), slope, stn, false);
                    }
                    n++; count++;
                }
                else
                {
                    int type = gi % 3;
                    if (type == 1)   // 계단: 6단, 단 높이 0.45·너비 1.0, 틈 안쪽으로 올라감
                        for (int k = 0; k < 6; k++)
                        {
                            float hh = (k + 1) * 0.45f;
                            Box(parent, "BLOCKOUT_Stairs_" + nGap + "_" + k, new Vector3(1.0f, hh + 0.4f, len - 0.6f), mid + r * (front + k + .5f) + Vector3.up * (roadY - 0.4f + (hh + 0.4f) * .5f), rot, concrete, false);
                        }
                    else if (type == 2)   // 빈터: 앞에 낮은 옹벽 1.5~3m (짧은 구간만)
                    {
                        float wh = R(1.5f, 3f);
                        Box(parent, "BLOCKOUT_LotWall_" + nGap, new Vector3(0.35f, wh + 0.3f, len - 0.6f), mid + r * (front + 0.2f) + Vector3.up * (roadY - 0.3f + (wh + 0.3f) * .5f), rot, concrete, true);
                    }
                    nGap++; gi++; count = 0; nextGap = rnd.Next(4, 7);
                }
                s += len + (gap ? 0f : 0.3f);
            }
        }
        info = $"틈 {nGap}(골목·계단·빈터), 실외기 {nAc}, 화분 {nPot}, 오토바이 {nBike}, 가게 {nShop}";
        return n;
    }

    /// <summary>
    /// 아래 계단식 마을(2차): 절벽면~해안 비탈에 11×10m 격자 + 흔들기. 길 9m 안·바다(해면+1.5 아래)·배달 집 12m 안은 비운다.
    /// 절반은 지붕판(테라코타)을 얹어 위에서 내려다볼 때 "마을"로 읽히게. 콜라이더 없음(길 밖).
    /// </summary>
    static int Town(Transform parent, float[,] t, List<List<Vector3>> polys, Material[] mats)
    {
        int n = 0; var rnd = new System.Random(232);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var main = polys[0]; Vector3 door = main[main.Count - 1] + Vector3.forward * 8f;
        for (float gx = -176f; gx <= 136f; gx += 11f)
            for (float gz = 180f; gz <= 342f; gz += 10f)
            {
                float x = gx + R(-2.5f, 2.5f), z = gz + R(-2f, 2f);
                if (DropAt(x, z) < 9f) continue;   // 절벽면 윗부분(≈7m 내려가기 전)은 비운다 — 처음엔 +8 이라 지붕이 모서리 위로 솟아 열림을 막았다
                if (new Vector2(x - door.x, z - door.z).magnitude < 12f) continue;
                bool nearRoad = false;
                foreach (var poly in polys) { for (int i = 0; i < poly.Count; i += 2) if (new Vector2(poly[i].x - x, poly[i].z - z).sqrMagnitude < 81f) { nearRoad = true; break; } if (nearRoad) break; }
                if (nearRoad) continue;
                if (keepOut.Any(k => new Vector2(x - k.x, z - k.y).magnitude < k.z)) continue;   // 밭·계단·주차 자리
                var size = new Vector3(R(5f, 8f), R(4f, 8f), R(5f, 7f));
                var rot = Quaternion.Euler(0f, R(-8f, 8f), 0f);
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var sx in new[] { -.5f, .5f }) foreach (var sz in new[] { -.5f, .5f })
                    { var q = new Vector3(x, 0, z) + rot * new Vector3(sx * size.x, 0, sz * size.z); float g = Hg(t, q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
                if (lo < SeaY + 1.5f) continue;
                // 지붕 상한: 꼭대기 눈높이(노면+2)에서 모서리 밖으로 10% 내려가는 선 아래 — 처음엔 상한이 없어 모서리 바로 밑 집이 바다를 가렸다(미리보기)
                // 3차: 꼭대기 너머 내리막(CrestRoll)만큼 눈높이도 내려갔다 — 가까운 모서리 지점의 내림을 빼야 북쪽 집이 다시 솟지 않는다
                float eN = z - ZN(x), eW = XW(z) - x, e = Mathf.Max(eN, eW), zr = eN >= eW ? ZN(x) : z;
                float cap = Plateau - CrestRoll * S((zr - CrestRollZ0) / CrestRollLen) - 2.5f - 0.10f * e;
                // 3차: 꼭대기(8,15.4,162) → 배달 문 눈선 밑으로 — 길을 낮춘 뒤 3-crest 캡처에서 문이 모서리 밑 집에 가렸다
                { Vector2 a = new Vector2(8f, 162f), b = new Vector2(door.x, door.z - 8f), p2 = new Vector2(x, z);
                  float u = Mathf.Clamp01(Vector2.Dot(p2 - a, b - a) / (b - a).sqrMagnitude);
                  if ((a + (b - a) * u - p2).magnitude < Mathf.Max(size.x, size.z) * .5f + 2f) cap = Mathf.Min(cap, Mathf.Lerp(15.4f, door.y + 2.4f, u) - 1f); }
                float baseY = lo - 0.5f, top = Mathf.Min(hi + size.y, cap);
                if (top - hi < 3.5f) continue;
                Box(parent, "BLOCKOUT_Town_" + n, new Vector3(size.x, top - baseY, size.z), new Vector3(x, (top + baseY) * .5f, z), rot, mats[n % 2], false);
                // D-133 축대: 비탈에 앉은 집은 평평한 터를 쌓아 만든 것 — 고저차 1.2m 넘으면 집보다 넓은 석축 단
                if (hi - lo > 1.2f) Box(parent, "BLOCKOUT_TownPlinth_" + n, new Vector3(size.x + 1.4f, hi + 0.3f - baseY, size.z + 1.4f), new Vector3(x, (hi + 0.3f + baseY) * .5f, z), rot, mats[3], false);
                if (rnd.NextDouble() < 0.55) Box(parent, "BLOCKOUT_TownRoof_" + n, new Vector3(size.x + 0.4f, 0.35f, size.z + 0.4f), new Vector3(x, top + 0.17f, z), rot, mats[2], false);
                n++;
            }
        return n;
    }

    /// <summary>정밀 격자 안이면 깎은 높이, 밖이면 원래 높이 함수.</summary>
    static float Hg(float[,] t, float x, float z) => (x > X0 && x < X1 && z > Z0 && z < Z1) ? Hs(t, x, z) : HilltopHeight(x, z);

    static void Cyl(Transform parent, string name, Vector3 scale, Vector3 pos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    static GameObject Box(Transform parent, string name, Vector3 size, Vector3 pos, Quaternion rot, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.rotation = rot; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    /// <summary>배달 문 앞 회색 사람 실루엣(캡슐 몸 + 공 머리), 키 1.75m. 콜라이더 없음.</summary>
    static void Person(Transform parent, Vector3 feet, Material mat)
    {
        var g = new GameObject("BLOCKOUT_Person").transform; g.SetParent(parent); g.position = feet;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.transform.SetParent(g); body.transform.localPosition = new Vector3(0, .7f, 0); body.transform.localScale = new Vector3(.5f, .7f, .35f);
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); head.transform.SetParent(g); head.transform.localPosition = new Vector3(0, 1.55f, 0); head.transform.localScale = Vector3.one * .3f;
        foreach (var go in new[] { body, head }) { go.GetComponent<Renderer>().sharedMaterial = mat; Object.DestroyImmediate(go.GetComponent<Collider>()); }
    }

    // ------------------------------------------------------------------ D-133 사람이 깎은 길
    // 규칙: 길을 낸 이유와 공사 흔적이 지형에 남는다. 아래 요소마다 "왜 거기 있는지"는 보고서 표에.
    static readonly Vector3 CrestZoneC = new Vector3(6f, 14.5f, 162f);
    const float CrestZoneR = 40f, CrestDrop = 2.2f;          // 시선 1.0 − 2.2 → 카메라에서 12.2m 앞 3.1m 아래 = 피치 약 −14°. 레이 측정(전체 화면 바다+마을): 1.9 → 20%, 2.2 → 22%, 2.35 → 21%(위쪽이 HUD 띠로 들어감)
    const float BankZ0 = 82f, BankZ1 = 118f;                  // 오르막 오른쪽 깎은 면 구간
    static readonly Rect FieldRect = new Rect(62f, 186f, 22f, 14f);   // 계단식 밭(모서리 길과 아래 길 사이 비탈)
    static readonly List<Vector3> keepOut = new List<Vector3>();      // (x, z, 반경) — 마을이 비우는 자리
    static Vector3 stairS, stairE;

    static int Nearest(List<Vector3> p, float x, float z)
    {
        int bi = 0; float bd = float.MaxValue;
        for (int i = 0; i < p.Count; i++) { float d = (p[i].x - x) * (p[i].x - x) + (p[i].z - z) * (p[i].z - z); if (d < bd) { bd = d; bi = i; } }
        return bi;
    }
    /// <summary>헤어핀 위 다리(x≈97) 어깨 → 아래 다리 어깨. 걸어서 내려가는 사람의 지름길.</summary>
    static void StairPath(List<Vector3> main, out Vector3 s, out Vector3 e)
    {
        Vector3 a = main[Nearest(main, 97f, 182f)], b = main[Nearest(main, 96f, 208f)];
        var n = b - a; n.y = 0; n.Normalize();
        s = a + n * (EdgeD + 0.7f); e = b - n * (EdgeD + 0.7f); s.y = a.y; e.y = b.y;
    }
    static void CarveStairs(float[,] t, Vector3 s, Vector3 e)
    {
        for (int i = 0; i < t.GetLength(0); i++)
            for (int j = 0; j < t.GetLength(1); j++)
            {
                var v = new Vector2(X0 + i * Step, Z0 + j * Step);
                float d = DistSeg(v, new Vector2(s.x, s.z), new Vector2(e.x, e.z), out float tt);
                if (d < 1.6f) t[i, j] = Mathf.Min(t[i, j], Mathf.Lerp(s.y, e.y, tt) - 0.1f);
            }
    }
    /// <summary>아래 길 끝 16m 전, 바다 쪽(북) 어깨 밖 — 배달 집 앞 동네 사람들 오토바이 세우는 곳.</summary>
    static Vector3 ParkingSpot(List<Vector3> main, out Quaternion rot)
    {
        int i = main.Count - 17; var f = Fwd(main, i); var l = new Vector3(-f.z, 0, f.x); if (l.z < 0) l = -l;
        rot = Quaternion.LookRotation(f);
        return main[i] + l * (EdgeD + 3.4f);
    }

    static string LifeDetails(Transform parent, Transform buildings, List<Vector3> main, List<Vector3> det, float[,] t, Material concrete, Material groundM, Material bikeM)
    {
        var earth = NewMat("Slice_CutEarth", new Color(0.62f, 0.45f, 0.30f));
        var rock = NewMat("Slice_CutRock", new Color(0.52f, 0.50f, 0.46f));
        var bush = NewMat("Slice_Bush", new Color(0.22f, 0.32f, 0.17f));
        var drain = NewMat("Slice_Drain", new Color(0.22f, 0.22f, 0.22f));
        var dirt = NewMat("Slice_WornDirt", new Color(0.55f, 0.44f, 0.30f));
        var railM = NewMat("Slice_Rail", new Color(0.88f, 0.88f, 0.84f));
        var poleM = NewMat("Slice_Pole", new Color(0.38f, 0.31f, 0.24f));
        var wireM = NewMat("Slice_Wire", new Color(0.06f, 0.06f, 0.07f));
        var fieldM = NewMat("Slice_Field", new Color(0.44f, 0.54f, 0.22f));
        var cropM = NewMat("Slice_Crop", new Color(0.30f, 0.42f, 0.16f));
        var rnd = new System.Random(133);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var polys = new List<List<Vector3>> { main, det };
        // 다른 길(또는 같은 길의 먼 구간)까지 거리 — 난간·전봇대가 갈림목·헤어핀 반대 다리를 가로막지 않게
        float OtherDist(Vector3 q, int pi, int idx)
        {
            float best = float.MaxValue;
            for (int k = 0; k < polys.Count; k++) for (int j = 0; j < polys[k].Count; j++)
                {
                    if (k == pi && Mathf.Abs(j - idx) < 15) continue;
                    float d = new Vector2(polys[k][j].x - q.x, polys[k][j].z - q.z).sqrMagnitude; if (d < best) best = d;
                }
            return Mathf.Sqrt(best);
        }
        float RoadNear(float x, float z, out float y)
        {
            float best = float.MaxValue; y = 0;
            foreach (var p in polys) for (int j = 0; j < p.Count; j++) { float d = (p[j].x - x) * (p[j].x - x) + (p[j].z - z) * (p[j].z - z); if (d < best) { best = d; y = p[j].y; } }
            return Mathf.Sqrt(best);
        }
        bool NearStairs(Vector3 q) => new Vector2(q.x - stairS.x, q.z - stairS.z).magnitude < 3.5f || new Vector2(q.x - stairE.x, q.z - stairE.z).magnitude < 3.5f;
        var info = new List<string>();

        // ① 깎은 면: 오르막 오른쪽 z 82~118 — 길이 산 어깨를 깎고 지나갔다. 단면 = 석축 발(0.7m) → 맨흙 비탈 → 위 덤불 → 뒤로 완만히 내려감.
        {
            int i0 = main.FindIndex(p => p.z >= BankZ0), i1 = main.FindIndex(p => p.z >= BankZ1);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tToe = new List<int>(); var tCut = new List<int>(); var tTop = new List<int>();
            float[] dOff = { 0f, 0f, 1.7f, 4.6f, 12f };            // 어깨(EdgeD+0.45)에서 바깥으로
            float[] hOff = { -0.5f, 0.7f, 4.2f, 5.2f, -0.6f };
            int cols = dOff.Length, rows = 0;
            for (int i = i0; i <= i1; i++, rows++)
            {
                float k = S(Mathf.Min(i - i0, i1 - i) / 7f);         // 양 끝 7m 동안 낮아져 길과 만난다
                var f = Fwd(main, i); var r = new Vector3(f.z, 0, -f.x);
                for (int c = 0; c < cols; c++)
                {
                    float h = c == 0 || c == cols - 1 ? hOff[c] : Mathf.Lerp(0.2f, hOff[c], k);
                    v.Add(main[i] + r * (EdgeD + 0.45f + dOff[c]) + Vector3.up * h); uv.Add(new Vector2(dOff[c] + h, i));
                }
                if (i > i0)
                    for (int c = 0; c + 1 < cols; c++)
                    {
                        int a = (rows - 1) * cols + c, b = a + 1, cc = a + cols, d = cc + 1;
                        var l = c == 0 ? tToe : c == 1 ? tCut : tTop;
                        l.Add(a); l.Add(b); l.Add(cc); l.Add(b); l.Add(d); l.Add(cc); l.Add(a); l.Add(cc); l.Add(b); l.Add(b); l.Add(cc); l.Add(d);   // 양면
                    }
            }
            MeshGo(parent, "CutBank", v, uv, new[] { tToe, tCut, tTop }, new[] { concrete, earth, groundM }, true);
            int nb = 0;
            for (int i = i0 + 4; i < i1 - 4; i += 2)
            {
                var f = Fwd(main, i); var r = new Vector3(f.z, 0, -f.x);
                float k = S(Mathf.Min(i - i0, i1 - i) / 7f);
                var q = main[i] + r * (EdgeD + 0.45f + R(2.4f, 5f)) + Vector3.up * Mathf.Lerp(0.2f, 4.9f, k);
                float sz = R(1.2f, 2.4f);
                Blob(parent, "VEG_BankBush_" + nb++, q, new Vector3(sz, sz * .7f, sz), bush);
            }
            // ② 배수로: 깎은 면 발치 — 비탈에서 내려온 물을 받는 얕은 홈(진한 띠). 노면 보도 끝 3.0 ~ 석축 3.65.
            Strip(parent, "Drain_Bank", main, i0 + 2, i1 - 2, 1, HalfSec - 0.05f, EdgeD + 0.45f, 0.025f, drain);
            info.Add($"깎은 면 {i1 - i0}m·덤불 {nb}");
        }

        // ③ 굽이 안쪽 배수구 + 굽이 바깥 닳은 흙: 반경 40m 미만 굽이에서 — 물은 안쪽으로 모이고, 바퀴는 바깥으로 밀린다
        int nGrate = 0, nDirt = 0;
        foreach (var p in polys)
        {
            int runStart = -1, runSide = 0;
            for (int i = 4; i < p.Count - 4; i++)
            {
                var f0 = Fwd(p, i - 4); var f1 = Fwd(p, i + 4);
                float ang = Vector3.Angle(f0, f1) * Mathf.Deg2Rad, rad = ang > 1e-3f ? 8f / ang : 1e9f;
                int turn = Vector3.Cross(f0, f1).y > 0 ? 1 : -1;     // 오른쪽으로 돌면(외적 +y) 안쪽 = 오른쪽(side +1)
                bool tight = rad < 40f;
                if (tight && i % 9 == 0)
                {
                    var r = new Vector3(Fwd(p, i).z, 0, -Fwd(p, i).x) * turn;
                    var q = p[i] + r * (HalfSec + 0.35f);
                    if (OtherDist(q, polys.IndexOf(p), i) > EdgeD + 1f)
                        Box(parent, "Drain_Grate_" + nGrate++, new Vector3(0.6f, 0.08f, 0.9f), q + Vector3.up * (Hs(t, q.x, q.z) + 0.03f), Quaternion.LookRotation(Fwd(p, i)), drain, false);
                }
                bool inRun = runStart >= 0;
                if (tight && !inRun) { runStart = i; runSide = -turn; }
                if ((!tight || i == p.Count - 5) && inRun) { if (i - runStart > 5) { Strip(parent, "WEAR_CurveDirt_" + nDirt++, p, runStart, i, runSide, HalfSec - 0.05f, HalfSec + 1.1f, 0.03f, dirt, t); } runStart = -1; }
            }
        }
        info.Add($"배수구 {nGrate}·굽이 흙 {nDirt}");

        // ④ 바다 쪽 메운 면 + 낮은 난간: 길 바깥 3m 지점 원래 땅이 노면보다 1.2m 넘게 낮으면 메운 쪽 → 어깨에 0.9m 난간(기둥 2m 간격)
        int nRail = 0; float railLen = 0;
        for (int pi = 0; pi < polys.Count; pi++)
        {
            var p = polys[pi];
            foreach (int side in new[] { 1, -1 })
            {
                var run = new List<Vector3>();
                void Flush()
                {
                    if (run.Count >= 5)
                    {
                        for (int k = 0; k < run.Count; k += 2)
                        {
                            Box(parent, "RAIL_Post_" + nRail + "_" + k, new Vector3(0.12f, 0.9f, 0.12f), run[k] + Vector3.up * 0.45f, Quaternion.identity, railM, false);
                            if (k + 2 < run.Count)
                            {
                                var a = run[k] + Vector3.up * 0.82f; var b = run[k + 2] + Vector3.up * 0.82f;
                                Box(parent, "RAIL_Bar_" + nRail + "_" + k, new Vector3(0.1f, 0.12f, Vector3.Distance(a, b) + 0.05f), (a + b) * .5f, Quaternion.LookRotation(b - a), railM, true);
                                railLen += Vector3.Distance(a, b);
                            }
                        }
                        nRail++;
                    }
                    run.Clear();
                }
                for (int i = 0; i < p.Count; i++)
                {
                    var f = Fwd(p, i); var r = new Vector3(f.z, 0, -f.x) * side;
                    var q = p[i] + r * (EdgeD + 0.15f);
                    float g = HilltopHeight(p[i].x + r.x * (EdgeD + 3f), p[i].z + r.z * (EdgeD + 3f));
                    bool fill = g < p[i].y - 1.2f && !NearStairs(q) && !(pi == 0 && i > p.Count - 14) && OtherDist(q, pi, i) > EdgeD + 0.6f;
                    if (fill) run.Add(q); else Flush();
                }
                Flush();
            }
        }
        info.Add($"난간 {nRail}줄 {railLen:F0}m");

        // ⑤ 계단 지름길: 헤어핀 두 다리 사이 — 사람은 걸어서 질러간다. 양 끝 볼라드 3개(틈 0.6m)라 차는 못 들어간다.
        {
            var s = stairS; var e = stairE; var n = e - s; n.y = 0; float L = n.magnitude; n /= L;
            int N = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(s.y - e.y) / 0.18f));
            var rot = Quaternion.LookRotation(n);
            for (int k = 0; k < N; k++)
            {
                float top = Mathf.Lerp(s.y, e.y, k / (float)(N - 1)), along = (k + 0.5f) / N * L;
                Box(parent, "STAIR_Step_" + k, new Vector3(1.4f, 0.7f, L / N + 0.02f), s + n * along + Vector3.up * (top - 0.35f) - Vector3.up * s.y, rot, concrete, false);
            }
            var s3 = s; var e3 = e; var mid = (s3 + e3) * .5f; var slope = Quaternion.LookRotation(e3 - s3);
            var right = new Vector3(n.z, 0, -n.x);
            foreach (int sd in new[] { 1, -1 })
                Box(parent, "STAIR_SideWall_" + (sd > 0 ? "R" : "L"), new Vector3(0.2f, 1.1f, Vector3.Distance(s3, e3)), mid + right * sd * 0.8f + Vector3.up * 0.15f, slope, concrete, false);
            int nb = 0;
            foreach (var (pt, dir) in new[] { (s, -n), (e, n) })
                foreach (float o in new[] { -0.75f, 0f, 0.75f })
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = "STAIR_Bollard_" + nb++; go.transform.SetParent(parent);
                    go.transform.position = pt + dir * 0.35f + right * o + Vector3.up * 0.45f; go.transform.localScale = new Vector3(0.24f, 0.45f, 0.24f);
                    go.GetComponent<Renderer>().sharedMaterial = railM;   // 콜라이더 유지 — 차를 막는 게 목적
                }
            info.Add($"계단 {N}단 {L:F0}m 낙차 {s.y - e.y:F1}m");
        }

        // ⑥ 계단식 밭 3단: 비탈에서 평평한 땅은 사람이 쌓아 만든 곳 — 앞(아래쪽)에 석축, 위에 밭과 이랑
        {
            int bands = 3; float bw = FieldRect.height / bands;
            for (int b = 0; b < bands; b++)
            {
                float z0 = FieldRect.yMin + b * bw, z1 = z0 + bw; float lo = float.MaxValue, hi = float.MinValue;
                for (float x = FieldRect.xMin; x <= FieldRect.xMax; x += 2f) for (float z = z0; z <= z1; z += 2f) { float g = Hg(t, x, z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
                float level = hi + 0.1f; var c = new Vector3(FieldRect.center.x, 0, (z0 + z1) * .5f);
                Box(parent, "FIELD_Terrace_" + b, new Vector3(FieldRect.width, level - lo + 0.6f, bw), c + Vector3.up * ((level + lo - 0.6f) * .5f - 0.15f), Quaternion.identity, concrete, false);
                Box(parent, "FIELD_Soil_" + b, new Vector3(FieldRect.width - 0.6f, 0.3f, bw - 0.6f), c + Vector3.up * level, Quaternion.identity, fieldM, false);
                for (float x = FieldRect.xMin + 1.5f; x < FieldRect.xMax - 1f; x += 1.6f)
                    Box(parent, "FIELD_Crop_" + b + "_" + (int)x, new Vector3(0.5f, 0.35f, bw - 1.4f), new Vector3(x, level + 0.3f, c.z), Quaternion.identity, cropM, false);
            }
            info.Add("밭 3단");
        }

        // ⑦ 작은 주차: 아래 길 끝 16m 전 바다 쪽 — 메운 땅 위 콘크리트 단 + 오토바이 2대 + 흰 칸 선
        {
            var pk = ParkingSpot(main, out var prot); float roadY = main[main.Count - 17].y;
            float lo = Mathf.Min(Hs(t, pk.x, pk.z), roadY) - 1.5f;
            Box(parent, "PARK_Pad", new Vector3(5.5f, roadY - lo, 7f), pk + Vector3.up * ((roadY + lo) * .5f - 0.02f), prot, concrete, true);
            for (int k = 0; k < 2; k++)
            {
                Box(parent, "PARK_Motorbike_" + k, new Vector3(0.55f, 0.9f, 1.7f), pk + prot * new Vector3(0.6f, 0, -1.6f + k * 2.2f) + Vector3.up * (roadY + 0.45f), prot * Quaternion.Euler(0, 90, 0), bikeM, false);
                Box(parent, "PARK_Line_" + k, new Vector3(4f, 0.02f, 0.12f), pk + prot * new Vector3(0.4f, 0, -2.7f + k * 2.2f) + Vector3.up * (roadY + 0.01f), prot, railM, false);
            }
        }

        // ⑧ 전봇대: 길 따라 26m 마다, 메운 쪽(난간·벼랑) 말고 산·집 쪽 어깨에 — 전선은 다음 기둥과 가까운 집 지붕으로
        var poles = new List<Vector3>(); int nWire = 0;
        var houses = buildings.Cast<Transform>().Where(h => h.name.StartsWith("BLOCKOUT_AlleyHouse") || h.name.StartsWith("BLOCKOUT_Town_") || h.name == "BLOCKOUT_DeliveryHouse").Select(h => h.GetComponent<Renderer>().bounds).ToList();
        for (int i = 8; i < main.Count - 6; i += 26)
        {
            var f = Fwd(main, i); int side = 0;
            foreach (int sd in new[] { 1, -1 })
            {
                var r0 = new Vector3(f.z, 0, -f.x) * sd;
                float g = HilltopHeight(main[i].x + r0.x * (EdgeD + 3f), main[i].z + r0.z * (EdgeD + 3f));
                var q0 = main[i] + r0 * (EdgeD + 0.2f);
                if (g >= main[i].y - 0.8f && OtherDist(q0, 0, i) > EdgeD + 0.8f && !NearStairs(q0)) { side = sd; break; }
            }
            if (side == 0) continue;
            var r = new Vector3(f.z, 0, -f.x) * side; var q = main[i] + r * (EdgeD + 0.2f);
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pole.name = "POLE_" + poles.Count; pole.transform.SetParent(parent);
            pole.transform.position = q + Vector3.up * 4f; pole.transform.localScale = new Vector3(0.26f, 4f, 0.26f); pole.GetComponent<Renderer>().sharedMaterial = poleM;
            Box(parent, "POLE_Arm_" + poles.Count, new Vector3(1.5f, 0.12f, 0.12f), q + Vector3.up * 7.6f, Quaternion.LookRotation(f), poleM, false);
            var top = q + Vector3.up * 7.5f;
            if (poles.Count > 0 && Vector3.Distance(poles[poles.Count - 1], top) < 40f)
                foreach (float o in new[] { -0.6f, 0.6f }) { Wire(parent, "WIRE_" + nWire++, poles[poles.Count - 1] + r * o, top + r * o, 0.6f, wireM); }
            // 가까운 집 1~2채로 인입선
            foreach (var hb in houses.Where(h => h.SqrDistance(top) < 14f * 14f && h.SqrDistance(top) > 1f).OrderBy(h => h.SqrDistance(top)).Take(2))
            {
                var hp = hb.ClosestPoint(top); hp.y = hb.max.y - 0.4f;
                Wire(parent, "WIRE_House_" + nWire++, top, hp, 0.35f, wireM);
            }
            poles.Add(top);
        }
        info.Add($"전봇대 {poles.Count}·전선 {nWire}");

        // ⑨ 깎은 면/벼랑 색 + 덤불: 정밀 지형에서 경사 0.7 넘는 칸 = 사람이 못 쓰는 땅. 길 14m 안이고 노면보다 높으면 "깎은 흙", 아니면 "바위".
        //   덤불은 벼랑·급경사에만(길 5m 밖), 손 닿는 평지는 비운다.
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tE = new List<int>(); var tR = new List<int>(); int nb = 0;
            for (int i = 0; i + 1 < t.GetLength(0); i++)
                for (int j = 0; j + 1 < t.GetLength(1); j++)
                {
                    float a = t[i, j], b = t[i + 1, j], c = t[i, j + 1], d = t[i + 1, j + 1];
                    float mx = Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d)), mn = Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
                    if ((mx - mn) / Step < 0.7f) continue;
                    float x = X0 + i * Step, z = Z0 + j * Step, cx = x + Step * .5f, cz = z + Step * .5f;
                    float rd = RoadNear(cx, cz, out float ry);
                    if (rd < EdgeD + 0.5f) continue;
                    bool cut = rd < 14f && (a + b + c + d) * .25f > ry + 0.5f;
                    int bi = v.Count; const float lift = 0.05f;
                    v.Add(new Vector3(x, a + lift, z)); v.Add(new Vector3(x + Step, b + lift, z)); v.Add(new Vector3(x, c + lift, z + Step)); v.Add(new Vector3(x + Step, d + lift, z + Step));
                    uv.Add(new Vector2(x, a)); uv.Add(new Vector2(x + Step, b)); uv.Add(new Vector2(x, c)); uv.Add(new Vector2(x + Step, d));
                    var l = cut ? tE : tR; l.Add(bi); l.Add(bi + 2); l.Add(bi + 1); l.Add(bi + 1); l.Add(bi + 2); l.Add(bi + 3);
                    if (rd > 5f && nb < 420 && rnd.NextDouble() < 0.16)
                    {
                        float sz = R(1.0f, 2.6f);
                        Blob(parent, "VEG_CliffBush_" + nb++, new Vector3(cx + R(-.6f, .6f), Hs(t, cx, cz) + sz * .2f, cz + R(-.6f, .6f)), new Vector3(sz, sz * .65f, sz), bush);
                    }
                }
            MeshGo(parent, "CutAndCliffFaces", v, uv, new[] { tE, tR }, new[] { earth, rock }, false);
            info.Add($"벼랑 덤불 {nb}");
        }
        return string.Join(", ", info);
    }

    /// <summary>길 한쪽 옆 띠(배수로·닳은 흙): 노면 기준 d0~d1 폭, 높이는 지형을 따라가되 노면보다 낮아지지 않게.</summary>
    static void Strip(Transform parent, string name, List<Vector3> p, int i0, int i1, int side, float d0, float d1, float lift, Material mat, float[,] t = null)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int i = i0; i <= i1; i++)
        {
            var f = Fwd(p, i); var r = new Vector3(f.z, 0, -f.x) * side;
            foreach (float d in new[] { d0, d1 })
            {
                var q = p[i] + r * d; float y = p[i].y;
                if (t != null) y = Mathf.Max(Hs(t, q.x, q.z), y - 0.6f);
                v.Add(new Vector3(q.x, y + lift, q.z)); uv.Add(new Vector2(d, i));
            }
            if (i > i0) { int a = (i - i0 - 1) * 2; tri.Add(a); tri.Add(a + 2); tri.Add(a + 1); tri.Add(a + 1); tri.Add(a + 2); tri.Add(a + 3); tri.Add(a); tri.Add(a + 1); tri.Add(a + 2); tri.Add(a + 1); tri.Add(a + 3); tri.Add(a + 2); }
        }
        MeshGo(parent, name, v, uv, new[] { tri }, new[] { mat }, false);
    }
    /// <summary>처진 전선: 가운데가 sag m 내려간 두 토막.</summary>
    static void Wire(Transform parent, string name, Vector3 a, Vector3 b, float sag, Material mat)
    {
        var m = (a + b) * .5f + Vector3.down * sag;
        foreach (var (p, q, k) in new[] { (a, m, "a"), (m, b, "b") })
            Box(parent, name + k, new Vector3(0.05f, 0.05f, Vector3.Distance(p, q)), (p + q) * .5f, Quaternion.LookRotation(q - p), mat, false);
    }
    static void Blob(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    // ------------------------------------------------------------------ 배달지·주문
    static string WireDelivery((string label, Vector3 pos)[] stops)
    {
        var dm = Object.FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        if (dm == null) return "no DeliveryManager";
        var so = new SerializedObject(dm); var arr = so.FindProperty("points");
        var old = new List<DeliveryPoint>();
        for (int i = 0; i < arr.arraySize; i++) { var p = arr.GetArrayElementAtIndex(i).objectReferenceValue as DeliveryPoint; if (p != null) old.Add(p); }
        var keep = new List<DeliveryPoint>(); var l = new List<string>();
        for (int i = 0; i < old.Count; i++)
        {
            if (i >= stops.Length) { Object.DestroyImmediate(old[i].gameObject); continue; }
            var p = old[i]; var st = stops[i];
            p.gameObject.name = st.label;
            var pso = new SerializedObject(p); var lp = pso.FindProperty("label"); if (lp != null) { lp.stringValue = st.label; pso.ApplyModifiedPropertiesWithoutUndo(); }
            p.transform.position = st.pos + Vector3.up * .5f;
            keep.Add(p); l.Add(st.label + "@" + st.pos.ToString("F0"));
        }
        dm.SetPoints(keep, null);
        EditorUtility.SetDirty(dm);
        return "stops " + string.Join(",", l);
    }

    static OrderCatalog BuildOrders()
    {
        if (!AssetDatabase.IsValidFolder(OrderDir)) AssetDatabase.CreateFolder("Assets/Data/Orders", "Hilltop");
        var defs = new (string file, string name, string label, CargoType cargo, int reward, int bonus, int tier, Color col)[]
        {
            ("Hilltop_1_Door", "언덕 위 집 — 유리", "HilltopDoor", CargoType.Glass, 1500, 400, 1, new Color(1f, .55f, .45f)),
            ("Hilltop_2_Detour", "우회로 끝 — 상자", "DetourEnd", CargoType.Box, 600, 150, 0, new Color(.55f, .85f, .55f)),
        };
        string cp = OrderDir + "/HilltopCatalog.asset";
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

    // ------------------------------------------------------------------ 공통
    static Transform Group(string name) { var g = new GameObject(name).transform; g.SetParent(root); return g; }
    static Material M(string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) Debug.LogWarning("[Hilltop] 머티리얼 없음: " + path);
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
    /// <summary>월드 셰이더 머티리얼(안개 속성 가진 것)을 MatDir 사본으로 교체 — 무드가 원본을 안 바꾸게.</summary>
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
