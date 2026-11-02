using System.IO;
using PitCrew.PackBench;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 포장 작업대 회색 박스 시제품 씬(D-250 ②, 과제 20261102-1108)을 새로 만든다 → Assets/Scenes/Proto_PackBench.unity.
/// 맵(Map_TownV6)에는 넣지 않는다. 머티리얼은 씬 전용 회색 사본(Assets/Scenes/Proto_PackBench_Mats/) — 공유 .mat 안 건드림.
/// 배치(세계 좌표, m): 눈 (0, 1.6, 0) +z 를 보고 48° 내려다봄 · 작업대 윗면 y 0.9, z 0.3~1.0, 폭 1.4 · 뒷벽 z 1.3
///   · 선반 두 단(y 1.15 / 1.6) 위 상자 S(−0.45)·M(0)·L(+0.5) · 작업 자리 (0.05, 0.9, 0.62)
///   · 왼쪽 앞 달걀 한 판, 왼쪽 뒤 신문지 묶음·라벨 묶음, 오른쪽 뽁뽁이 롤·테이프.
/// 상자 크기(속): S 0.32×0.10×0.32(완충재 들어갈 자리 없음) · M 0.315×0.15×0.315(맞는 상자) · L 0.50×0.30×0.50(헐렁) — 가설.
/// </summary>
public static class PackBenchBuilder
{
    public const string ScenePath = "Assets/Scenes/Proto_PackBench.unity";
    const string MatDir = "Assets/Scenes/Proto_PackBench_Mats";

    [MenuItem("PitCrew/Proto/Build PackBench (포장 작업대 회색 박스)")]
    static void Menu() => Debug.Log("[PackBenchBuilder] " + Build());

    public static string Build()
    {
        if (EditorApplication.isPlaying) return "Play 중 — 멈추고 다시";
        var cur = SceneManager.GetActiveScene();
        if (cur.isDirty && cur.path != ScenePath) return $"열린 씬 {cur.path} 에 저장 안 된 변경이 있음 — 중단(덮지 않음)";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Proto_PackBench_Mats");
        var mFloor = Mat("PB_Floor", 0.34f, 0.1f);
        var mWall = Mat("PB_Wall", 0.52f, 0.05f);
        var mBench = Mat("PB_Bench", 0.42f, 0.15f);
        var mShelf = Mat("PB_Shelf", 0.46f, 0.1f);
        var mBox = Mat("PB_Box", 0.60f, 0.05f);
        var mTray = Mat("PB_Tray", 0.70f, 0.05f);
        var mEgg = Mat("PB_Egg", 0.80f, 0.2f);
        var mPaper = Mat("PB_Paper", 0.78f, 0.0f);
        var mBubble = Mat("PB_Bubble", 0.66f, 0.6f);
        var mTape = Mat("PB_Tape", 0.50f, 0.5f);
        var mLabel = Mat("PB_Label", 0.88f, 0.1f);

        // ── 방 ──
        var room = new GameObject("Room").transform;
        Block("Floor", room, new Vector3(0f, -0.05f, 0.6f), new Vector3(4f, 0.1f, 3.6f), mFloor);
        Block("Wall_Back", room, new Vector3(0f, 1.4f, 1.35f), new Vector3(4f, 2.8f, 0.1f), mWall);
        Block("Wall_L", room, new Vector3(-1.4f, 1.4f, 0.4f), new Vector3(0.1f, 2.8f, 2.4f), mWall);
        Block("Wall_R", room, new Vector3(1.4f, 1.4f, 0.4f), new Vector3(0.1f, 2.8f, 2.4f), mWall);
        Block("Bench", room, new Vector3(0f, 0.45f, 0.65f), new Vector3(1.4f, 0.9f, 0.7f), mBench);
        Block("Shelf_Low", room, new Vector3(0f, 1.14f, 1.15f), new Vector3(1.6f, 0.02f, 0.3f), mShelf);
        Block("Shelf_High", room, new Vector3(0f, 1.59f, 1.15f), new Vector3(1.6f, 0.02f, 0.3f), mShelf);
        Block("Shelf_PostL", room, new Vector3(-0.79f, 1.1f, 1.15f), new Vector3(0.03f, 0.8f, 0.3f), mShelf);
        Block("Shelf_PostR", room, new Vector3(0.79f, 1.1f, 1.15f), new Vector3(0.03f, 0.8f, 0.3f), mShelf);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, 25f, 0f);
        var fill = new GameObject("Lamp").AddComponent<Light>();
        fill.type = LightType.Point; fill.range = 3f; fill.intensity = 0.8f; fill.shadows = LightShadows.None;
        fill.transform.position = new Vector3(0f, 2.3f, 0.5f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.44f);

        // ── 눈(카메라) ──
        var eye = new GameObject("Eye", typeof(Camera), typeof(AudioListener));
        eye.tag = "MainCamera";
        eye.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(48f, 0f, 0f));
        var cam = eye.GetComponent<Camera>();
        cam.fieldOfView = 60f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 30f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.2f, 0.21f);
        var low = eye.AddComponent<LowResRenderTest>();
        low.targetHeight = 360; low.smooth = false; low.msaa = 1;   // 걷기·주행과 같은 360 도트 설정

        // ── 상자 셋(선반) ──
        var shelf = new GameObject("ShelfSlots").transform;
        var boxes = new GameObject("Boxes").transform;
        var bS = MakeBox("Box_S", boxes, shelf, "S", new Vector3(0.32f, 0.10f, 0.32f), new Vector3(-0.45f, 1.15f, 1.15f), mBox, mTape, mLabel);
        var bM = MakeBox("Box_M", boxes, shelf, "M", new Vector3(0.315f, 0.15f, 0.315f), new Vector3(0f, 1.15f, 1.15f), mBox, mTape, mLabel);
        var bL = MakeBox("Box_L", boxes, shelf, "L", new Vector3(0.50f, 0.30f, 0.50f), new Vector3(0.5f, 1.15f, 1.15f), mBox, mTape, mLabel);

        var spot = new GameObject("BenchSpot").transform;
        spot.SetPositionAndRotation(new Vector3(0.05f, 0.9f, 0.62f), Quaternion.identity);

        // ── 재료 더미 ──
        var sup = new GameObject("Supplies").transform;
        Supply("Newspaper", sup, PrimitiveType.Cube, new Vector3(-0.55f, 0.93f, 0.75f), Quaternion.Euler(0f, 8f, 0f), new Vector3(0.30f, 0.06f, 0.22f), mPaper, PackSupplyKind.Newspaper);
        Supply("BubbleRoll", sup, PrimitiveType.Cylinder, new Vector3(0.55f, 0.98f, 0.55f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.16f, 0.16f, 0.16f), mBubble, PackSupplyKind.Bubble);
        Supply("Tape", sup, PrimitiveType.Cylinder, new Vector3(0.42f, 0.9125f, 0.95f), Quaternion.identity, new Vector3(0.10f, 0.0125f, 0.10f), mTape, PackSupplyKind.Tape);
        Supply("Labels", sup, PrimitiveType.Cube, new Vector3(-0.32f, 0.91f, 0.95f), Quaternion.Euler(0f, -6f, 0f), new Vector3(0.10f, 0.02f, 0.07f), mLabel, PackSupplyKind.Label);

        // ── 달걀 한 판(CargoTypePresets Eggs 대역 — 회색) ──
        MakeTray(new Vector3(-0.45f, 0.9f + 0.0325f + 0.002f, 0.45f), mTray, mEgg);

        // ── 손(컨트롤러) + 커서 ──
        var bench = eye.AddComponent<PackBench>();
        bench.cam = cam; bench.benchSpot = spot; bench.paperMat = mPaper; bench.bubbleMat = mBubble;
        var cur2 = new GameObject("Cursor").AddComponent<PackBenchCursor>();
        var so = new SerializedObject(cur2);
        so.FindProperty("bench").objectReferenceValue = bench;
        so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
        so.FindProperty("smallFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri9.ttf");
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
        return $"{(ok ? "저장" : "저장 실패")} {ScenePath} — 상자 {bS.sizeName}/{bM.sizeName}/{bL.sizeName}, 머티리얼 {MatDir}";
    }

    static Material Mat(string name, float grey, float smooth)
    {
        string p = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        var c = new Color(grey, grey, grey * 0.98f);
        m.SetColor("_BaseColor", c); m.color = c;
        m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform Block(string name, Transform parent, Vector3 pos, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.isStatic = true;
        g.transform.SetParent(parent, false);
        g.transform.position = pos; g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }

    static PackBox MakeBox(string name, Transform parent, Transform slots, string size, Vector3 inner, Vector3 shelfPos, Material box, Material tape, Material label)
    {
        var slot = new GameObject("Slot_" + size).transform;
        slot.SetParent(slots, false);
        slot.SetPositionAndRotation(shelfPos, Quaternion.identity);
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.SetPositionAndRotation(shelfPos, Quaternion.identity);
        var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
        var b = g.AddComponent<PackBox>();
        b.inner = inner; b.sizeName = size; b.boxMat = box; b.tapeMat = tape; b.labelMat = label;
        b.shelfSlot = slot; b.startClosed = true;
        b.Build();
        return b;
    }

    static void Supply(string name, Transform parent, PrimitiveType type, Vector3 pos, Quaternion rot, Vector3 scale, Material m, PackSupplyKind kind)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (type == PrimitiveType.Cylinder)   // 기본 캡슐 콜라이더 대신 상자(롤이 굴러보이지 않게, 조준만 맞으면 됨)
        {
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(1f, 2f, 1f);
        }
        g.AddComponent<PackSupply>().kind = kind;
    }

    /// <summary>달걀 한 판: 충돌 상자 0.30×0.065×0.30(중심 = 피벗) + 보기용 판·달걀 5×6(콜라이더 없음).</summary>
    static void MakeTray(Vector3 pos, Material tray, Material egg)
    {
        var g = new GameObject("EggTray");
        g.transform.SetPositionAndRotation(pos, Quaternion.identity);
        var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(0.30f, 0.065f, 0.30f);
        var pm = new PhysicsMaterial("EggTray") { dynamicFriction = 0.5f, staticFriction = 0.6f, bounciness = 0.05f };
        string pmPath = $"{MatDir}/PB_EggTray.physicMaterial";
        var saved = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (saved == null) { AssetDatabase.CreateAsset(pm, pmPath); saved = pm; }
        bc.sharedMaterial = saved;
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(slab.GetComponent<Collider>());
        slab.name = "Slab"; slab.transform.SetParent(g.transform, false);
        slab.transform.localPosition = new Vector3(0f, -0.0175f, 0f); slab.transform.localScale = new Vector3(0.30f, 0.03f, 0.30f);
        slab.GetComponent<Renderer>().sharedMaterial = tray;
        for (int x = 0; x < 5; x++)
            for (int z = 0; z < 6; z++)
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(e.GetComponent<Collider>());
                e.name = $"Egg_{x}{z}"; e.transform.SetParent(g.transform, false);
                e.transform.localPosition = new Vector3((x - 2f) * 0.058f, 0.008f, (z - 2.5f) * 0.048f);
                e.transform.localScale = new Vector3(0.042f, 0.05f, 0.042f);
                e.GetComponent<Renderer>().sharedMaterial = egg;
            }
        var rb = g.AddComponent<Rigidbody>();
        rb.mass = 2.0f; rb.linearDamping = 0.1f; rb.angularDamping = 0.5f;   // 달걀 30개 × 60 g + 판
        PackBench.Setup(rb);
        g.AddComponent<PackItem>().kind = PackItemKind.Eggs;
    }
}
