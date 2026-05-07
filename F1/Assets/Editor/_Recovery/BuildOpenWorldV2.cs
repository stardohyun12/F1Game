using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 세션 17: OpenWorld_v2 신규 씬 + OpenWorldLayout 빌드.
// 1) 신규 빈 씬 생성 + Assets/Scenes/OpenWorld_v2.unity 저장
// 2) TileMapAuto.BuildFromLayout — 도로 (heightMask 있으면 Y offset)
// 3) BuildingPlacer.Build — 건물 (heightMask 평균 높이)
// 4) Race.prefab 배치 + StartCell 위치
// 5) Phase 1 Full Integration Setup 자동 호출
public static class BuildOpenWorldV2
{
    const string LayoutPath = "Assets/Data/OpenWorldLayout.asset";
    const string ScenePath = "Assets/Scenes/OpenWorld_v2.unity";
    const string RacePrefabPath = "Assets/Prefabs/Race.prefab";
    const float CellSize = 6f;

    [MenuItem("PitCrew/Layout/Build OpenWorld_v2 (Full)")]
    public static void Execute()
    {
        // 1) 신규 빈 씬 먼저 (씬 전환 후 SO 참조가 끊기는 케이스 회피)
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[OpenWorldV2] (1/5) created scene {ScenePath}");

        // Layout은 씬 생성 후 로드 (씬 전환 시 SO 참조가 invalidate됨)
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(LayoutPath);
        if (layout == null || layout.roadMask == null || layout.roadMask.Length == 0)
        {
            Debug.LogError($"Layout invalid after scene creation: {LayoutPath} (roadMask null/empty)");
            return;
        }

        // 2) Track tiles
        TileMapAuto.BuildFromLayout(layout);
        Debug.Log("[OpenWorldV2] (2/5) road tiles built");

        // 3) Buildings
        BuildingPlacer.Build(layout);
        Debug.Log("[OpenWorldV2] (3/5) buildings built");

        // 4) Race.prefab 배치
        var racePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacePrefabPath);
        if (racePrefab == null) { Debug.LogWarning($"[OpenWorldV2] Race.prefab missing at {RacePrefabPath}"); }
        else
        {
            var existing = GameObject.Find("Race");
            if (existing != null) Object.DestroyImmediate(existing);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(racePrefab);
            inst.name = "Race";
            float startX = layout.startCell.x * CellSize;
            float startZ = layout.startCell.y * CellSize;
            inst.transform.position = new Vector3(startX, 0.5f, startZ);
            inst.transform.rotation = Quaternion.Euler(0f, layout.startYawDeg, 0f);
            Debug.Log($"[OpenWorldV2] (4/5) Race placed at ({startX:0.0}, 0.5, {startZ:0.0}), yaw={layout.startYawDeg}");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        // 5) Phase 1 setup
        SetupPhase1Integration.Execute();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[OpenWorldV2] (5/5) Phase 1 Full Integration done — saved.");

        // 6) Race 재배치 — SetupPhase1Integration 안의 SetupRaceInScene이 layout 휴리스틱 실패 시
        //    DefaultTrackLayout(legacy)의 startCell (1,0)을 사용. layout.startCell로 강제 덮어쓰기.
        var raceFinal = GameObject.Find("Race");
        if (raceFinal != null && layout != null)
        {
            float fx = layout.startCell.x * CellSize;
            float fz = layout.startCell.y * CellSize;
            raceFinal.transform.position = new Vector3(fx, 0.5f, fz);
            raceFinal.transform.rotation = Quaternion.Euler(0f, layout.startYawDeg, 0f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log($"[OpenWorldV2] (6/6) Race 재배치 → ({fx:0.0}, 0.5, {fz:0.0}) cell({layout.startCell.x},{layout.startCell.y})");
        }
        Debug.Log("[OpenWorldV2] === DONE === Open scene + Play to verify.");
    }
}
