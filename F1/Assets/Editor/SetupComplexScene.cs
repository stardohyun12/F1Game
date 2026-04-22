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
        // 13x9 grid with 3 horizontal + 4 vertical roads → 6 interior blocks (3x3 each) for T-junction variety.
        layout.roadMask = new[]
        {
            "#############", // row 0 — horizontal south
            "#...#...#...#", // row 1
            "#...#...#...#", // row 2
            "#...#...#...#", // row 3
            "#############", // row 4 — horizontal middle
            "#...#...#...#", // row 5
            "#...#...#...#", // row 6
            "#...#...#...#", // row 7
            "#############", // row 8 — horizontal north
        };
        // Building mask: S/C = 1x1 suburban/commercial, K/X = 2x2 big commercial/suburban.
        // Each 3x3 block holds one 2x2 anchor (top-left) + 5 1x1 fillers. K/X alternate between rows for variety.
        layout.buildingMask = new[]
        {
            ".............", // row 0
            ".KKC.XXS.KKC.", // row 1 — K(1), 1x1 C, X(5), 1x1 S, K(9), 1x1 C
            ".KKS.XXC.KKS.", // row 2 — K/X rows occupied; col 3/7/11 are 1x1 fillers
            ".SCS.CSC.SCS.", // row 3 — all 1x1 row (K/X cannot anchor here — would overlap row 4 road)
            ".............", // row 4 (road)
            ".XXC.KKS.XXC.", // row 5 — mirrored: X, K, X anchors
            ".XXS.KKC.XXS.", // row 6
            ".CSC.SCS.CSC.", // row 7 — all 1x1 row
            ".............", // row 8
        };
        var catalogGuids = AssetDatabase.FindAssets("t:BuildingCatalog");
        if (catalogGuids.Length > 0)
            layout.catalog = AssetDatabase.LoadAssetAtPath<BuildingCatalog>(AssetDatabase.GUIDToAssetPath(catalogGuids[0]));
        layout.startCell = new Vector2Int(1, 0);
        layout.startYawDeg = 0f; // facing +X along bottom straight
        layout.deliveries = new List<TrackLayout.DeliveryCell>
        {
            new TrackLayout.DeliveryCell { col = 6, row = 0, label = "Delivery_Bottom" },
            new TrackLayout.DeliveryCell { col = 12, row = 4, label = "Delivery_RightMid" },
            new TrackLayout.DeliveryCell { col = 6, row = 8, label = "Delivery_Top" },
            new TrackLayout.DeliveryCell { col = 0, row = 4, label = "Delivery_LeftMid" },
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
