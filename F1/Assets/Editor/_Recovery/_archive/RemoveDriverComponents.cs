using UnityEditor;
using UnityEngine;

public static class RemoveDriverComponents
{
    static void RemoveByName(GameObject go, string typeName)
    {
        if (go == null) return;
        var comps = go.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c == null) continue; // 스크립트 삭제로 missing 된 컴포넌트
            if (c.GetType().Name == typeName)
            {
                Object.DestroyImmediate(c);
                Debug.Log($"[RemoveDriverComponents] Removed {typeName} from {go.name}");
            }
        }
    }

    public static void Execute()
    {
        // 1. 씬 HUD 에서 DriverHUD 제거 (스크립트 missing 시 자동 정리)
        var hud = GameObject.Find("HUD");
        if (hud != null)
        {
            // Missing script도 정리
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(hud);
            RemoveByName(hud, "DriverHUD");
        }

        // 2. Race 프리팹에서 DriverResource 제거
        var prefabPath = "Assets/Prefabs/Race.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(instance);
            RemoveByName(instance, "DriverResource");
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        // 3. 씬의 Race 인스턴스에도 (만약 분리됐다면)
        var raceInScene = GameObject.Find("Race");
        if (raceInScene != null)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(raceInScene);
            RemoveByName(raceInScene, "DriverResource");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[RemoveDriverComponents] done.");
    }
}
