using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 맵 경계 생성기 (D-123 후속, 2026-10-23). 바닥(Ground 192x144)이 끝나는 곳 바깥은 콜라이더가 없어 차가 떨어졌다.
/// 만드는 것: 바깥 바닥 3장(서·남·동, 충돌+잔디/흙 질감) · 북쪽 바다 밑 해저(충돌) · 맵 둘레 옹벽(충돌+메쉬, 1720 부터 가드레일 없음) · 장식 바위(충돌 없음).
/// 다시 만들기: 메뉴 PitCrew/Build Map Boundary — "MapBoundary" 루트를 지우고 처음부터 만든다. 수치는 아래 상수.
/// </summary>
public static class BuildMapBoundary
{
    const string Dir = "Assets/Art/CoastalTown";
    const string Kit = "Assets/Art/LofiPilot";
    const string RootName = "MapBoundary";

    // Ground 평면 가장자리(씬 실측: 중심 (87,63), 크기 192x144)
    const float MinX = -9f, MaxX = 183f, MinZ = -9f, MaxZ = 135f;
    const float OuterWidth = 80f;      // 바깥 바닥이 뻗는 폭
    const float SeaFarZ = 280f;        // 바다 바닥이 뻗는 북쪽 끝
    const float SeabedY = -2f;
    const float WallHeight = 1.1f, WallThickness = 0.8f, ColliderHeight = 1.6f, ColliderThickness = 1.2f;
    static readonly Color WallColor = new Color(0.64f, 0.69f, 0.70f); // 옛 콘크리트(.66,.65,.62)와 안개색(.62,.73,.77)의 중간
    const int RockCount = 70;

    [MenuItem("PitCrew/Build Map Boundary")]
    public static void Build()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject(RootName);
        var groundPhys = GameObject.Find("Ground")?.GetComponent<Collider>()?.sharedMaterial;

        var outerMat = Mat("OuterGround", new Color(0.92f, 0.95f, 0.85f), 13, 0.45f, 37, 0.5f, 0.06f);
        var seabedMat = Mat("Seabed", new Color(0.55f, 0.62f, 0.6f), 38, 0.45f);
        // 맵 끝 A안 (과제 1720, 리서치 1655): 노란 가드레일 제거, 덮개·몸통은 안개색(.62,.73,.77) 쪽으로 반쯤 당긴 저채도 한 색.
        // 끝이 "눈에 띄는 선"이 아니라 안개에 묻히는 벽으로 읽히게. 충돌 박스는 그대로.
        var concrete = Mat("BoundaryConcrete", WallColor, 1, 0.9f);
        var rock = Mat("BoundaryRock", new Color(0.5f, 0.5f, 0.47f));

        // --- 바깥 바닥: 윗면 y=0 (Ground 와 같은 높이), 아래로 0.5m 두께 박스 ---
        var ground = new GameObject("OuterGround"); ground.transform.SetParent(root.transform);
        Slab(ground, "Outer_South", MinX - OuterWidth, MaxX + OuterWidth, MinZ - OuterWidth, MinZ, 0f, outerMat, groundPhys);
        Slab(ground, "Outer_West", MinX - OuterWidth, MinX, MinZ, MaxZ, 0f, outerMat, groundPhys);
        Slab(ground, "Outer_East", MaxX, MaxX + OuterWidth, MinZ, MaxZ, 0f, outerMat, groundPhys);
        // 북쪽은 항구 바다 — 바다 수면(HarborSea y=-0.15) 아래 해저. 차는 옹벽 때문에 못 오지만 바닥 없음 칸을 남기지 않는다.
        Slab(ground, "Seabed_North", MinX - OuterWidth, MaxX + OuterWidth, MaxZ, SeaFarZ, SeabedY, seabedMat, groundPhys);

        // --- 옹벽 + 가드레일 (맵 둘레) ---
        var walls = new GameObject("Walls"); walls.transform.SetParent(root.transform);
        float cx = (MinX + MaxX) * 0.5f, cz = (MinZ + MaxZ) * 0.5f, lenX = MaxX - MinX, lenZ = MaxZ - MinZ;
        Wall(walls, "Wall_South", new Vector3(cx, 0, MinZ), Vector3.right, Vector3.forward, lenX, concrete);
        Wall(walls, "Wall_North", new Vector3(cx, 0, MaxZ), Vector3.right, Vector3.back, lenX, concrete);
        Wall(walls, "Wall_West", new Vector3(MinX, 0, cz), Vector3.forward, Vector3.right, lenZ, concrete);
        Wall(walls, "Wall_East", new Vector3(MaxX, 0, cz), Vector3.forward, Vector3.left, lenZ, concrete);

        // --- 장식 바위 (충돌 없음 — 옹벽 바깥이라 차가 못 간다) ---
        var rocks = new GameObject("Rocks"); rocks.transform.SetParent(root.transform);
        var rng = new System.Random(2310);
        for (int i = 0; i < RockCount; i++)
        {
            float x = Lerp(rng, MinX - OuterWidth + 4f, MaxX + OuterWidth - 4f), z = Lerp(rng, MinZ - OuterWidth + 4f, MaxZ - 2f);
            bool insideWall = x > MinX - 3f && x < MaxX + 3f && z > MinZ - 3f; // 맵 안쪽·옹벽 근처 제외
            if (insideWall) { i--; continue; }
            if (z > MaxZ - 2f && (x > MinX - 3f && x < MaxX + 3f)) { i--; continue; }
            float s = Lerp(rng, 0.8f, 2.6f);
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube); r.name = "Rock";
            Object.DestroyImmediate(r.GetComponent<Collider>());
            r.transform.SetParent(rocks.transform);
            r.transform.position = new Vector3(x, s * 0.25f, z);
            r.transform.rotation = Quaternion.Euler(Lerp(rng, -8, 8), Lerp(rng, 0, 360), Lerp(rng, -8, 8));
            r.transform.localScale = new Vector3(s * Lerp(rng, 0.9f, 1.6f), s * 0.7f, s * Lerp(rng, 0.9f, 1.6f));
            r.GetComponent<Renderer>().sharedMaterial = rock;
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[BuildMapBoundary] 맵 경계 생성 완료 — 바깥 바닥 3 + 해저 1 + 옹벽 4 + 바위 " + RockCount);
    }

    [MenuItem("PitCrew/Build Map Boundary And Save")]
    public static void BuildAndSave()
    {
        Build();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    static float Lerp(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    /// <summary>윗면이 topY 인 판. 박스 콜라이더 포함, 렌더는 큐브 윗면(월드 UV 라 Ground 질감과 이어진다).</summary>
    static void Slab(GameObject parent, string name, float x0, float x1, float z0, float z1, float topY, Material mat, PhysicsMaterial phys)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent.transform);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, topY - 0.25f, (z0 + z1) * 0.5f);
        go.transform.localScale = new Vector3(x1 - x0, 0.5f, z1 - z0);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (phys != null) go.GetComponent<Collider>().sharedMaterial = phys;
    }

    /// <summary>옹벽 한 변. axis=벽이 뻗는 방향, inward=맵 안쪽. 몸통(보이는 메쉬) + 윗덮개(같은 머티리얼). 충돌은 별도 박스 하나.</summary>
    static void Wall(GameObject parent, string name, Vector3 center, Vector3 axis, Vector3 inward, float length, Material body)
    {
        var w = new GameObject(name); w.transform.SetParent(parent.transform);
        w.transform.position = center; w.transform.rotation = Quaternion.LookRotation(axis, Vector3.up); // 로컬 z = 뻗는 방향
        float side = Mathf.Sign(Vector3.Dot(inward, w.transform.right)); // 로컬 x 의 어느 쪽이 안쪽인가
        float len = length + WallThickness * 2f;                            // 모서리 겹침

        // 몸통은 해저(−2m)까지 내려간다 — 북쪽은 바다라 벽 밑이 비어 보이지 않게
        Part(w, "Body", new Vector3(0, (WallHeight + SeabedY) * 0.5f, 0), new Vector3(WallThickness, WallHeight - SeabedY, len), body, false);
        Part(w, "Cap", new Vector3(0, WallHeight + 0.06f, 0), new Vector3(WallThickness + 0.2f, 0.12f, len), body, false);

        // 충돌 박스: 메쉬보다 살짝 두껍고 높다(고속 터널링·점프 방지).
        var col = new GameObject("Collider"); col.transform.SetParent(w.transform, false);
        var bc = col.AddComponent<BoxCollider>();
        bc.center = new Vector3(side * 0.0f, ColliderHeight * 0.5f, 0);
        bc.size = new Vector3(ColliderThickness, ColliderHeight, len);
    }

    static void Part(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos; go.transform.localScale = size;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    /// <summary>CoastalTownInstaller.Mat 과 같은 규칙(같은 셰이더·안개·그림자 톤) — 바깥 바닥이 마을 바닥과 이어져 보이게.</summary>
    static Material Mat(string name, Color tint, int tile = -1, float repeat = 0.5f, int tile2 = -1, float patch = 0.55f, float patchScale = 0.09f)
    {
        string path = Dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(tile >= 0 ? "PitCrew/CoastalTown/WorldSurface" : "PitCrew/LofiPilot/Surface");
        if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        m.SetColor("_Tint", tint); m.SetColor("_Ambient", new Color(1, .99f, .95f)); m.SetFloat("_FaceShade", .65f);
        m.SetFloat("_FogStart", 45); m.SetFloat("_FogEnd", 165); m.SetColor("_FogColor", new Color(.62f, .73f, .77f));
        if (tile >= 0)
        {
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "/LofiAtlas.png"));
            m.SetVector("_Tile", new Vector4(tile % 8 / 8f, tile / 8 / 8f, .125f, .125f)); m.SetFloat("_WorldScale", repeat);
            m.SetFloat("_Patch", tile2 >= 0 ? 1 : 0); m.SetFloat("_PatchThreshold", patch); m.SetFloat("_PatchScale", patchScale);
            if (tile2 >= 0) m.SetVector("_Tile2", new Vector4(tile2 % 8 / 8f, tile2 / 8 / 8f, .125f, .125f));
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
