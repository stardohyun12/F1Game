using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates a new scene "ComplexTrack.unity" with a more complex track layout:
// 9-wide x 7-tall double-loop (one horizontal road through the middle => figure-8-ish).
// Also creates a ComplexTrackLayout SO with matching building mask.
public static class SetupComplexScene
{
    const string ScenePath = "Assets/ComplexTrack.unity";
    const string LayoutPath = "Assets/Data/ComplexTrackLayout.asset";

    [MenuItem("PitCrew/Create Complex Track Scene")]
    public static void Run()
    {
        // 1. Layout SO
        var layout = LoadOrCreate<TrackLayout>(LayoutPath);
        // 13x9 grid with shortcuts added at row 2 (south suburban) + row 6 (north suburban).
        // Breaks the perfectly uniform 3x4 grid (session 11 D-034 "오픈월드 체감" Tier A).
        layout.roadMask = new[]
        {
            "#############", // row 0 — horizontal south
            "#...#...#...#", // row 1
            "#.###...###.#", // row 2 — shortcut W + shortcut E cut through suburban/industrial blocks
            "#...#...#...#", // row 3
            "#############", // row 4 — horizontal middle
            "#...#...#...#", // row 5
            "#.###...###.#", // row 6 — mirrored shortcuts
            "#...#...#...#", // row 7
            "#############", // row 8 — horizontal north
        };
        // Building mask — Lynch districts + D-034 오픈월드 Tier A:
        //   Left (cols 1-3): 교외 S + P plaza (북쪽/남쪽 블록 남단 광장)
        //   Middle (cols 5-7): 상업(하)·Downtown(상) = K + H + T landmark
        //   Right (cols 9-11): 산업 W
        // 지름길 2곳(row 2 col 2-3, row 6 col 2-3; row 2 col 9-10, row 6 col 9-10)이 roadMask에서
        // road로 변환됐으므로 그 cell들은 '.'로 비움.
        layout.buildingMask = new[]
        {
            ".............", // row 0 — edge
            ".SSS.CCC.WWW.", // row 1 — 교외 / 상업 / 산업 (1x1 filler)
            ".S...KKC...W.", // row 2 — 지름길(col 2,3 / 9,10) 비움. K anchor (5,2), W col 11
            ".PPP.KKC.WWW.", // row 3 — P 광장(교외 남서 하단) / K footprint / 산업
            ".............", // row 4 — road
            ".SSS.HTH.WWW.", // row 5 — Downtown south (H/T landmark)
            ".S...HKK...W.", // row 6 — 지름길 + Downtown K anchor (6,6)
            ".PPP.HKK.WWW.", // row 7 — P 광장(교외 북서 하단) / K footprint / 산업
            ".............", // row 8 — edge
        };
        var catalogGuids = AssetDatabase.FindAssets("t:BuildingCatalog");
        if (catalogGuids.Length > 0)
            layout.catalog = AssetDatabase.LoadAssetAtPath<BuildingCatalog>(AssetDatabase.GUIDToAssetPath(catalogGuids[0]));
        layout.startCell = new Vector2Int(1, 0);
        layout.startYawDeg = 0f; // facing +X along bottom straight
        // 7 delivery nodes at major intersections, co-located with district landmarks.
        // Burnout "종점 집중" + Lynch Node. Count=7 matches Order_C_Risky.pointCount.
        layout.deliveries = new List<TrackLayout.DeliveryCell>
        {
            new TrackLayout.DeliveryCell { col = 6, row = 4, label = "DowntownHub" },   // Downtown 중앙 교차로 (K 2×2 남쪽)
            new TrackLayout.DeliveryCell { col = 6, row = 8, label = "NorthGate" },     // 북단 Downtown 위
            new TrackLayout.DeliveryCell { col = 12, row = 2, label = "FactoryEast" },  // 산업지역 남동
            new TrackLayout.DeliveryCell { col = 12, row = 6, label = "IndustryNorth" },// 산업지역 북동
            new TrackLayout.DeliveryCell { col = 0, row = 6, label = "SuburbWest" },    // 교외지역 북서
            new TrackLayout.DeliveryCell { col = 0, row = 2, label = "SuburbSouth" },   // 교외지역 남서
            new TrackLayout.DeliveryCell { col = 8, row = 0, label = "SouthGate" },     // 남단 Start 옆
        };
        EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. New scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Re-load layout after scene switch (reference may be invalidated).
        layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(LayoutPath);
        if (layout == null) { Debug.LogError("SetupComplexScene: failed to reload layout after scene switch"); return; }

        // 3. Camera — static top-down at layout center
        float cx = (layout.Width - 1) * 6f * 0.5f;
        float cz = (layout.Height - 1) * 6f * 0.5f;
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(cx, 40f, cz);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var c = cam.GetComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 32f;
        c.backgroundColor = new Color(0.2f, 0.2f, 0.22f);

        // 4. Light
        var light = new GameObject("Directional Light", typeof(Light));
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var l = light.GetComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.0f;

        // 5. Ground plane (large, covers layout)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(cx, 0f, cz);
        ground.transform.localScale = Vector3.one * 10f; // 100x100m, covers 13x9 grid * 6m
        // Apply GroundPhysics material if present so the car won't stick if ported here.
        var groundPhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/GroundPhysicsMat.physicMaterial");
        if (groundPhys != null)
        {
            var col = ground.GetComponent<Collider>();
            if (col != null) col.sharedMaterial = groundPhys;
        }

        // 6. Build road + buildings from layout
        TileMapAuto.BuildFromLayout(layout);
        BuildingPlacer.Build(layout);

        // 7. Save scene
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"SetupComplexScene: created {ScenePath} with layout {layout.Width}x{layout.Height}.");
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
}
