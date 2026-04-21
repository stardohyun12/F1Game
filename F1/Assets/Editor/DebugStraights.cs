using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DebugStraights
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";

    [MenuItem("PitCrew/Debug Straights")]
    public static void Execute()
    {
        string[] variants = {
            "tile-low.fbx",
            "tile-high.fbx",
            "road-straight-half.fbx",
            "road-straight-barrier-half.fbx",
        };

        var existing = GameObject.Find("DebugStraights");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("DebugStraights");
        root.transform.position = new Vector3(80f, 0f, -40f);

        for (int i = 0; i < variants.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + variants[i]);
            if (prefab == null) { Debug.LogWarning("Missing: " + variants[i]); continue; }
            // Place 3 in a row along Z to see how they tile.
            for (int k = 0; k < 3; k++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = $"{variants[i]}_{k}";
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(i * 6f, 0, k * 4f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * 4f;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("DebugStraights placed (4 variants × 3 tiles each).");
    }
}
