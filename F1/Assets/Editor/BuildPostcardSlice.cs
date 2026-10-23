using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 원화(art/concept/lofi-styles-2026-10-13/02_warm_street.png) 한 장면 재현 수직 슬라이스 (과제 20261023-1733).
/// 목표: 조명이 아니라 구도·지형·밀도만으로 분위기가 나는가 확인. 메뉴 PitCrew/Slice/Build Postcard Street → 새 씬을 처음부터 만든다.
/// 길: 직선 50m → 오른쪽 커브(반경 40m, 75°) → 직선 60m = 약 162m, 폭 6.5m, 6.5% 내리막.
/// 왼쪽: 가게 2채·정비소 → 커브부터 절벽+가드레일(바다 쪽). 오른쪽: 인도 + 옹벽(2.65m) 위 집들 + 전봇대·전선 + 승합차.
/// 커브 너머 북쪽에 바다·아래 마을·먼 섬. 없는 소품은 "BLOCKOUT_" 회색 블록(아트팀 목록).
/// 머티리얼은 전부 슬라이스 전용 사본(Slice_PostcardStreet_Mats/) — 무드 A 를 구워 넣어도 ComplexTrack 머티리얼은 안 바뀐다.
/// </summary>
public static class BuildPostcardSlice
{
    public const string ScenePath = "Assets/Scenes/Slice_PostcardStreet.unity";
    const string MatDir = "Assets/Scenes/Slice_PostcardStreet_Mats";
    const string Lofi = "Assets/Art/LofiPilot/";

    // 길 모양
    const float L1 = 50f, R = 40f, TurnDeg = 75f, L3 = 60f;
    const float Y0 = 8f, Slope = 0.065f;
    const float HalfRoad = 3.25f, WallD = 5.0f, WallH = 2.65f, CliffFrom = 56f;
    static float Arc => R * TurnDeg * Mathf.Deg2Rad;
    public static float Total => L1 + Arc + L3;
    public const float SpawnS = 5f, SpawnD = 1.2f;
    const float SeaY = -8f;

    static readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    static Transform root;
    public static readonly List<string> Blockouts = new List<string>();

    [MenuItem("PitCrew/Slice/Build Postcard Street")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildScene();
    }

    public static string BuildScene()
    {
        copies.Clear(); Blockouts.Clear();
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Slice_PostcardStreet_Mats");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("PostcardStreet").transform;

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
        var shelf = NewMat("Slice_Shelf", new Color(0.55f, 0.60f, 0.45f));

        // --- 지형 띠 (단면을 길 따라 밀어낸 메쉬) ---
        var terrain = Group("Terrain");
        float s0 = -12f, s1 = Total + 2f;
        Strip(terrain, "Road", s0, s1, s => new[] { V(-HalfRoad, 0), V(HalfRoad, 0) }, asphalt, true);
        Strip(terrain, "Sidewalk_L", s0, s1, s => new[] { V(-HalfRoad - 2f, .15f), V(-HalfRoad, .15f), V(-HalfRoad, 0) }, pavement, true);
        Strip(terrain, "Sidewalk_R", s0, s1, s => new[] { V(HalfRoad, 0), V(HalfRoad, .15f), V(WallD, .15f) }, pavement, true);
        Strip(terrain, "RetainingWall_R", s0, s1, s => new[] { V(WallD, .15f), V(WallD, WallH), V(WallD + .5f, WallH) }, concrete, true);
        Strip(terrain, "Terrace_R", s0, s1, s => new[] { V(WallD + .5f, WallH), V(30f, WallH) }, ground, true);
        // 왼쪽: 마을 구간 평지 → 커브부터 바다로 떨어지는 절벽 (점 개수 같게 두고 보간)
        Strip(terrain, "Ground_L", s0, s1, s =>
        {
            float t = Mathf.InverseLerp(CliffFrom - 6f, CliffFrom + 6f, s);
            Vector2[] town = { V(-30, .15f), V(-18, .15f), V(-7.2f, .15f), V(-HalfRoad - 2f, .15f) };
            Vector2[] cliff = { V(-26, -19), V(-10.5f, -5), V(-7.2f, .15f), V(-HalfRoad - 2f, .15f) };
            var p = new Vector2[4]; for (int i = 0; i < 4; i++) p[i] = Vector2.Lerp(town[i], cliff[i], t); return p;
        }, ground, true);
        Strip(terrain, "CliffFace_L", CliffFrom - 6f, s1, s => new[] { V(-26.2f, -19.2f), V(-10.7f, -5.2f) }, rock, false);
        // 가드레일(바다 쪽) — 레일 띠 + 기둥
        Strip(terrain, "Guardrail_L", CliffFrom, s1, s => new[] { V(-6.7f, .55f), V(-6.7f, .95f), V(-6.55f, .95f), V(-6.55f, .55f) }, rail, true);
        for (float s = CliffFrom; s <= s1; s += 2.5f) Block(terrain, "RailPost", new Vector3(.12f, .95f, .12f), s, -6.62f, .15f + .475f, 0, rail, false);
        // 옹벽 위 난간(블록아웃)
        Strip(terrain, "BLOCKOUT_TerraceFence_R", s0, s1, s => new[] { V(WallD + .25f, WallH), V(WallD + .25f, WallH + .9f) }, grey, false);
        Blockouts.Add("TerraceFence_R — 옹벽 위 난간(원화 오른쪽 위 철책)");
        // 끝막이·뒤막이
        Block(terrain, "EndBarrier", new Vector3(14f, 1.4f, 1f), Total + 1.5f, 0, .7f, 0, concrete, true);
        Block(terrain, "StartBarrier", new Vector3(14f, 1.4f, 1f), s0 + .5f, 0, .7f, 0, concrete, true);

        // --- 차선 ---
        var lines = Group("RoadLines");
        for (float s = s0 + 1f; s < s1 - 2f; s += 5f) Strip(lines, "Dash", s, s + 2.2f, x => new[] { V(-.07f, .012f), V(.07f, .012f) }, paint, false, 0.5f);
        Strip(lines, "Edge_L", s0, s1, x => new[] { V(-HalfRoad + .25f, .012f), V(-HalfRoad + .4f, .012f) }, paint, false);
        Strip(lines, "Edge_R", s0, s1, x => new[] { V(HalfRoad - .4f, .012f), V(HalfRoad - .25f, .012f) }, paint, false);

        // --- 건물 (기존 생성기 프리팹) ---
        var bld = Group("Buildings");
        Prefab(bld, "CornerShop", 12f, -8.8f, .15f, FaceRoadLeft);      // 원화의 MART 자리
        Prefab(bld, "CornerShopC", 22f, -8.8f, .15f, FaceRoadLeft);
        Prefab(bld, "CornerShopB", 31f, -9.0f, .15f, FaceRoadLeft);
        Prefab(bld, "RepairGarage", 50f, -10.5f, .15f, FaceRoadLeft - 35f); // 커브 바깥, 다가오는 차를 마주봄
        Prefab(bld, "CoastalHouseB", 6f, 9.5f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouseC", 18f, 9.5f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouseD", 30f, 9.0f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouse", 44f, 9.5f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouseB", 64f, 10.5f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouseC", 84f, 10f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouseD", 104f, 9.5f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouse", 120f, 10f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouseB", 138f, 9.5f, WallH, FaceRoadRight);
        Prefab(bld, "CoastalHouseC", 154f, 10f, WallH, FaceRoadRight);

        // --- 거리 소품 ---
        var props = Group("Props");
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
        Prefab(props, "CoastalTree", 14f, 7.5f, WallH, 0f);
        Prefab(props, "CoastalTree", 74f, 8f, WallH, 0f);
        Prefab(props, "CoastalTree", 112f, 7.5f, WallH, 0f);
        Block(props, "BLOCKOUT_VendingMachine", new Vector3(1f, 1.8f, .8f), 27f, -5.7f, .15f + .9f, 0, grey, true);
        Blockouts.Add("VendingMachine — 가게 옆 자판기");
        Block(props, "BLOCKOUT_KeiTruck", new Vector3(1.5f, 1.7f, 3.4f), 44f, -7.6f, .15f + .85f, 20f, grey, true);
        Blockouts.Add("KeiTruck — 정비소 앞 흰 경트럭");
        Block(props, "BLOCKOUT_ShopPosters", new Vector3(.05f, 1.2f, 2.2f), 15.5f, -5.35f, 1.0f, 0, grey, false);
        Blockouts.Add("ShopPosters/Display — 가게 유리 진열·포스터");
        Block(props, "BLOCKOUT_PoleStripe", new Vector3(.32f, 1.6f, .32f), 30f, 4.5f, .15f + .8f, 0, grey, false);
        Blockouts.Add("PoleStripe — 전봇대 아래 노랑·검정 띠");
        // 승합차(케니 delivery) — 오른쪽 인도에 걸쳐 주차, 반대 방향
        var vanSrc = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/kenney_car-kit/Models/FBX format/delivery.fbx");
        if (vanSrc != null)
        {
            var van = (GameObject)PrefabUtility.InstantiatePrefab(vanSrc, props); van.name = "Van";
            Frame(26f, out var c, out var f, out var r);
            van.transform.position = c + r * 4.1f + Vector3.up * .15f; van.transform.rotation = Quaternion.LookRotation(-f); van.transform.localScale = Vector3.one * .78f;
            var b = Bounds(van); var bc = van.AddComponent<BoxCollider>(); bc.center = van.transform.InverseTransformPoint(b.center); bc.size = Vector3.Scale(b.size, new Vector3(1 / .78f, 1 / .78f, 1 / .78f));
        }
        else Blockouts.Add("Van — delivery.fbx 없음");

        // 전봇대 + 전선 (오른쪽 인도)
        var poles = Group("UtilityPoles");
        var tops = new List<Vector3[]>();
        for (float s = 8f; s < Total; s += 22f)
        {
            var p = Prefab(poles, "UtilityPole", s, 4.5f, .15f, 0f);
            if (p == null) break;
            Frame(s, out var c, out var f, out var r);
            var top = c + r * 4.5f + Vector3.up * (.15f + 6.15f);
            tops.Add(new[] { top - r * .6f, top + r * .6f, top });
        }
        for (int i = 0; i + 1 < tops.Count; i++)
            for (int k = 0; k < 3; k++) Wire(poles, tops[i][k], tops[i + 1][k], k == 2 ? .55f : .4f, wire);

        // --- 멀리: 바다·아래 마을·섬 ---
        var far = Group("Far");
        var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane); seaGo.name = "Sea"; seaGo.transform.SetParent(far);
        seaGo.transform.position = new Vector3(40f, SeaY, 400f); seaGo.transform.localScale = new Vector3(300f, 1f, 200f);
        Object.DestroyImmediate(seaGo.GetComponent<Collider>()); seaGo.GetComponent<Renderer>().sharedMaterial = sea;
        Box(far, "VillageShelf", new Vector3(90f, 1f, 34f), new Vector3(90f, SeaY + 2.7f, 175f), 8f, shelf);
        var rng = new System.Random(1733);
        for (int i = 0; i < 16; i++)
        {
            float x = 50f + (float)rng.NextDouble() * 80f, z = 163f + (float)rng.NextDouble() * 22f, w = 4f + (float)rng.NextDouble() * 3f, h = 3f + (float)rng.NextDouble() * 4f;
            Box(far, "BLOCKOUT_FarHouse", new Vector3(w, h, w * .9f), new Vector3(x, SeaY + 3f + h * .5f, z), (float)rng.NextDouble() * 20f, village);
            Box(far, "BLOCKOUT_FarRoof", new Vector3(w + .4f, .8f, w * .9f + .4f), new Vector3(x, SeaY + 3f + h + .4f, z), 0, roof);
        }
        Blockouts.Add("FarHouse×16 + 지붕 — 커브 아래 바닷가 마을(먼 거리라 낮은 해상도로 충분)");
        Hill(far, "Island_Big", new Vector3(-160f, SeaY, 820f), new Vector3(520f, 95f, 160f), hill);
        Hill(far, "Island_Right", new Vector3(330f, SeaY, 760f), new Vector3(300f, 60f, 120f), hill);
        Hill(far, "Islet_Village", new Vector3(150f, SeaY, 280f), new Vector3(90f, 26f, 50f), hill);
        for (int i = 0; i < 6; i++)
        {
            float x = 128f + i * 7f, z = 270f + (i % 2) * 6f;
            Box(far, "BLOCKOUT_IsletHouse", new Vector3(4f, 3.5f, 4f), new Vector3(x, SeaY + 4f, z), 0, village);
        }
        Blockouts.Add("Island_Big/Island_Right/Islet — 먼 섬(구 기본도형) · IsletHouse×6");

        // --- 해·하늘·후처리 = 무드 A (슬라이스 전용 사본, 안개는 먼 섬이 보이게 늘림) ---
        var sunGo = new GameObject("Directional Light"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft;
        var volGo = new GameObject("Global Volume"); var vol = volGo.AddComponent<UnityEngine.Rendering.Volume>(); vol.isGlobal = true;
        var moodSrc = AssetDatabase.LoadAssetAtPath<MoodProfile>(MoodSwitcher.PathA);
        string moodPath = MatDir + "/Mood_A_Slice.asset";
        var mood = AssetDatabase.LoadAssetAtPath<MoodProfile>(moodPath);
        if (mood == null && moodSrc != null) { mood = Object.Instantiate(moodSrc); AssetDatabase.CreateAsset(mood, moodPath); }
        if (mood != null)
        {
            EditorUtility.CopySerialized(moodSrc, mood);
            mood.fogStart = 70f; mood.fogEnd = 900f; // 원화는 먼 섬이 또렷 — 기본 A(45–210)면 섬이 안개에 다 묻힌다
            EditorUtility.SetDirty(mood);
        }

        // --- 차·카메라 ---
        var carSrc = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Race.prefab");
        var car = (GameObject)PrefabUtility.InstantiatePrefab(carSrc);
        Frame(SpawnS, out var sc, out var sf, out var sr);
        car.transform.position = sc + sr * SpawnD + Vector3.up * .6f; car.transform.rotation = Quaternion.LookRotation(sf);
        var cso = new SerializedObject(car.GetComponent<ArcadeCarController>()); cso.FindProperty("freeDrive").boolValue = true; cso.ApplyModifiedPropertiesWithoutUndo();

        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = .2f; cam.farClipPlane = 1500f; cam.fieldOfView = 50f;
        camGo.AddComponent<AudioListener>();
        var fc = camGo.AddComponent<FollowCamera>();
        CameraFeelSwitcher.BuildPresets(false);
        var fso = new SerializedObject(fc);
        fso.FindProperty("target").objectReferenceValue = car.transform;
        fso.FindProperty("car").objectReferenceValue = car.GetComponent<ArcadeCarController>();
        fso.FindProperty("preset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.Postcard);
        fso.FindProperty("positionSmoothTime").floatValue = .05f; fso.FindProperty("rotationLerpSpeed").floatValue = 10f; // ComplexTrack 씬 값과 같게
        fso.ApplyModifiedPropertiesWithoutUndo();
        camGo.transform.position = car.transform.position - sf * 7f + Vector3.up * 2.3f; camGo.transform.LookAt(car.transform.position + sf * 9f + Vector3.up);

        // 월드 머티리얼을 전부 슬라이스 사본으로 바꾼 뒤 무드 적용 → ComplexTrack 쪽 에셋은 안 건드림
        RemapToCopies();
        if (mood != null) MoodController.ApplyProfile(mood, sun, vol);
        AssetDatabase.SaveAssets();

        EditorSceneManager.SaveScene(scene, ScenePath);
        return "built " + ScenePath + " len=" + Total.ToString("F1") + "m copies=" + copies.Count + " blockouts=" + Blockouts.Count;
    }

    // 프리팹 정면 방향: LofiPilot 건물은 로컬 −Z 가 정면(가정 — 캡처로 확인). 왼쪽 건물은 +r(길) 을, 오른쪽은 −r 을 보게.
    const float FaceRoadLeft = 90f, FaceRoadRight = -90f; // 첫 캡처에서 정면이 길 반대로 나와 뒤집음

    static Vector2 V(float d, float h) => new Vector2(d, h);

    public static void Frame(float s, out Vector3 c, out Vector3 f, out Vector3 r)
    {
        float th; Vector3 p;
        float a1 = TurnDeg * Mathf.Deg2Rad;
        if (s <= L1) { p = new Vector3(0, 0, s); th = 0; }
        else if (s <= L1 + Arc) { float a = (s - L1) / R; p = new Vector3(R - R * Mathf.Cos(a), 0, L1 + R * Mathf.Sin(a)); th = a; }
        else { var pe = new Vector3(R - R * Mathf.Cos(a1), 0, L1 + R * Mathf.Sin(a1)); th = a1; p = pe + new Vector3(Mathf.Sin(th), 0, Mathf.Cos(th)) * (s - L1 - Arc); }
        f = new Vector3(Mathf.Sin(th), 0, Mathf.Cos(th)); r = new Vector3(f.z, 0, -f.x);
        p.y = Y0 - Slope * s; c = p;
    }

    static Transform Group(string name) { var g = new GameObject(name).transform; g.SetParent(root); return g; }

    /// <summary>단면(d=옆 거리, h=높이) 점들을 s0→s1 로 밀어낸 띠 메쉬. 단면 점은 "윗면이 보이는" 순서(왼→오, 벽은 아래→위)로.</summary>
    static GameObject Strip(Transform parent, string name, float s0, float s1, System.Func<float, Vector2[]> profile, Material mat, bool collider, float step = 1f)
    {
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        int n = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / step)); int m = 0;
        for (int i = 0; i <= n; i++)
        {
            float s = Mathf.Lerp(s0, s1, i / (float)n); Frame(s, out var c, out var f, out var r); var prof = profile(s); m = prof.Length; float u = 0;
            for (int k = 0; k < m; k++) { if (k > 0) u += Vector2.Distance(prof[k], prof[k - 1]); verts.Add(c + r * prof[k].x + Vector3.up * prof[k].y); uvs.Add(new Vector2(u, s)); }
        }
        for (int i = 0; i < n; i++) for (int k = 0; k < m - 1; k++) { int a = i * m + k, b = a + 1, c2 = a + m, d = c2 + 1; tris.Add(a); tris.Add(c2); tris.Add(b); tris.Add(b); tris.Add(c2); tris.Add(d); }
        var mesh = new Mesh { name = name, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        // 메쉬를 씬에만 두면 저장 때 씬 파일이 커진다 — 에셋으로 저장
        string mp = MatDir + "/Mesh_" + parent.name + "_" + name + "_" + Mathf.RoundToInt(s0 * 10) + ".asset";
        AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
        var go = new GameObject(name); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    static GameObject Prefab(Transform parent, string name, float s, float d, float h, float yaw)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Lofi + name + ".prefab");
        if (src == null) { Blockouts.Add(name + " — 프리팹 없음"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        Frame(s, out var c, out var f, out var r);
        go.transform.position = c + r * d + Vector3.up * h; go.transform.rotation = Quaternion.LookRotation(f) * Quaternion.Euler(0, yaw, 0);
        return go;
    }

    static GameObject Block(Transform parent, string name, Vector3 size, float s, float d, float h, float yaw, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        Frame(s, out var c, out var f, out var r);
        go.transform.position = c + r * d + Vector3.up * h; go.transform.rotation = Quaternion.LookRotation(f) * Quaternion.Euler(0, yaw, 0); go.transform.localScale = size;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat; return go;
    }

    static void Box(Transform parent, string name, Vector3 size, Vector3 pos, float yaw, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mat;
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
        if (m == null) Debug.LogWarning("[Slice] 머티리얼 없음: " + path);
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

    /// <summary>씬의 월드 셰이더 머티리얼(안개 속성 가진 것)을 MatDir 사본으로 교체. 사본은 같은 원본끼리 하나를 공유.</summary>
    static void RemapToCopies()
    {
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || !m.HasProperty("_FogColor") || !m.HasProperty("_Ambient")) continue;
                if (AssetDatabase.GetAssetPath(m).StartsWith(MatDir)) continue;
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
