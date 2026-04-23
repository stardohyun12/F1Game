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
        // 15x9 grid — 세션 13 확장.
        //  - 동쪽 2 col(13-14) 추가: ㅗ자 dead-end 돌출 (격자 단조 해결)
        //  - 'X' = pedestrian crossing (straight 시각 변종). 남/북/중앙 main road에 각 3개
        //  - Dead-end 자동 생성: (14,3), (14,5) — road-end-round.fbx로 교외 느낌
        //  - 기존 지름길(row 2,6) 유지
        layout.roadMask = new[]
        {
            // 15×9 완전 ring — 세션 13 fix: col 14를 동쪽 수직 main으로 승격,
            // 이전 버전의 "cols 13-14 건물 2블록 고립" 문제 해결.
            // col 13은 row 4만 road (L 꺾임 1칸), 나머지는 건물/빈 자리.
            "#X###X###X###X#", // row 0 — south main full ring + Crossing cols 1/5/9/13
            "#...#...#...#.#", // row 1
            "#.###...###.#.#", // row 2 — shortcuts + east vertical main (col 14)
            "#...#...#...#.#", // row 3
            "#X###X###X#####", // row 4 — mid main + Crossings + east full connect (cols 13-14)
            "#...#...#...#.#", // row 5
            "#.###...###.#.#", // row 6 — mirrored shortcuts + east
            "#...#...#...#.#", // row 7
            "#X###X###X###X#", // row 8 — north main full ring + Crossings
        };
        // Building mask — Lynch districts + 세션 13 동쪽 확장:
        //   Left (cols 1-3): 교외 S + P plaza
        //   Middle (cols 5-7): 상업(하)·Downtown(상) = K + H + T landmark
        //   Right (cols 9-11): 산업 W
        //   East extension (cols 13-14): 북쪽 H 확장 + 남쪽 S 확장 (road L 우회)
        layout.buildingMask = new[]
        {
            // col 13 = 건물 자리 (동쪽 수직 main인 col 14 서쪽 인도),
            // col 14 = road (buildingMask는 '.').
            // col 13 row 4 = road (L 꺾임), 나머지 col 13은 건물 배치 가능.
            "...............", // row 0 — edge
            ".SSS.CCC.WWW.S.", // row 1 — 교외/상업/산업 + 동쪽 col 13 교외 확장
            ".S...KKC...W.S.", // row 2 — 지름길 비움 + 동쪽 col 13
            ".PPP.KKC.WWW.P.", // row 3 — 광장 + 동쪽 col 13 광장 확장
            "...............", // row 4 — road row
            ".SSS.HTH.WWW.H.", // row 5 — Downtown + 동쪽 col 13 H 확장
            ".S...HKK...W.H.", // row 6 — 지름길 + 동쪽 col 13
            ".PPP.HKK.WWW.H.", // row 7 — 광장 + 동쪽 col 13 H
            "...............", // row 8 — edge
        };
        var catalogGuids = AssetDatabase.FindAssets("t:BuildingCatalog");
        if (catalogGuids.Length > 0)
            layout.catalog = AssetDatabase.LoadAssetAtPath<BuildingCatalog>(AssetDatabase.GUIDToAssetPath(catalogGuids[0]));
        layout.startCell = new Vector2Int(1, 0);
        layout.startYawDeg = 0f; // facing +X along bottom straight
        // 7 delivery nodes — 세션 13 확장 반영. 15 cols 기준 재배치.
        // Burnout "종점 집중" + Lynch Node. Count=7 matches Order_C_Risky.pointCount.
        layout.deliveries = new List<TrackLayout.DeliveryCell>
        {
            new TrackLayout.DeliveryCell { col = 6, row = 4, label = "DowntownHub" },   // 중앙 교차로
            new TrackLayout.DeliveryCell { col = 6, row = 8, label = "NorthGate" },     // 북단 Downtown
            new TrackLayout.DeliveryCell { col = 12, row = 2, label = "FactoryEast" },  // 산업 남동
            new TrackLayout.DeliveryCell { col = 14, row = 4, label = "EastBranchEnd" },// 세션 13: 동쪽 L 돌출 T 교차점
            new TrackLayout.DeliveryCell { col = 0, row = 6, label = "SuburbWest" },    // 교외 북서
            new TrackLayout.DeliveryCell { col = 0, row = 2, label = "SuburbSouth" },   // 교외 남서
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
