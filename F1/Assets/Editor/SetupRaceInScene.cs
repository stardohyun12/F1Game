using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-time: creates Assets/Prefabs/Race.prefab from the SampleScene's Race GameObject,
// then instantiates it into the currently open scene at TrackLayout.startCell.
// Also replaces the scene's static top-down camera with a FollowCamera setup targeting the car.
public static class SetupRaceInScene
{
    const string SamplePath = "Assets/SampleScene.unity";
    const string PrefabPath = "Assets/Prefabs/Race.prefab";

    [MenuItem("PitCrew/Place Car In Current Scene")]
    public static void PlaceInCurrent()
    {
        string targetScenePath = EditorSceneManager.GetActiveScene().path;
        if (targetScenePath == SamplePath) { Debug.LogError("Already in SampleScene — open the destination scene first."); return; }
        if (string.IsNullOrEmpty(targetScenePath)) { Debug.LogError("Active scene is unsaved — save it first."); return; }

        // 1. Ensure prefab exists (build from SampleScene if missing).
        EnsurePrefab();

        // 2. Re-open target scene (EnsurePrefab switched to SampleScene).
        var targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);

        // 3. Load TrackLayout to know startCell.
        var layout = FindLayoutForScene(targetScenePath);
        if (layout == null) { Debug.LogError("SetupRaceInScene: could not find TrackLayout for this scene"); return; }

        // 4. Instantiate Race prefab at start cell.
        var existing = GameObject.Find("Race");
        if (existing != null) Object.DestroyImmediate(existing);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("Race prefab missing"); return; }
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.name = "Race";
        inst.transform.position = new Vector3(layout.startCell.x * 6f, 1f, layout.startCell.y * 6f);
        inst.transform.rotation = Quaternion.Euler(0f, layout.startYawDeg, 0f);

        // 5. Swap camera to FollowCamera targeting the car.
        var camGO = GameObject.Find("Main Camera");
        if (camGO != null)
        {
            var cam = camGO.GetComponent<Camera>();
            if (cam != null) { cam.orthographic = false; cam.fieldOfView = 55f; cam.backgroundColor = new Color(0.45f, 0.55f, 0.65f); }
            // Remove existing FollowCamera (fresh config).
            var existingFc = camGO.GetComponent<FollowCamera>();
            if (existingFc != null) Object.DestroyImmediate(existingFc);
            var fc = camGO.AddComponent<FollowCamera>();

            // Assign serialized private fields via SerializedObject (reflection on private [SerializeField] needs binding flags).
            var so = new SerializedObject(fc);
            so.FindProperty("target").objectReferenceValue = inst.transform;
            var carProp = so.FindProperty("car");
            if (carProp != null) carProp.objectReferenceValue = inst.GetComponent<ArcadeCarController>();
            so.ApplyModifiedProperties();

            camGO.transform.position = inst.transform.position + new Vector3(0, 4, -6);
            camGO.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
        }

        // 6. Ensure Ground has low-friction physics material (session-3 bug repro fix).
        var groundGO = GameObject.Find("Ground");
        if (groundGO != null)
        {
            var col = groundGO.GetComponent<Collider>();
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/GroundPhysicsMat.physicMaterial");
            if (col != null && mat != null) col.sharedMaterial = mat;
            else if (mat == null) Debug.LogWarning("SetupRaceInScene: GroundPhysics material not found — car may stick to ground.");
        }

        EditorSceneManager.SaveScene(targetScene);
        Debug.Log($"SetupRaceInScene: placed Race + configured camera in {targetScenePath} at cell ({layout.startCell.x},{layout.startCell.y}).");
    }

    static void EnsurePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;

        Directory.CreateDirectory("Assets/Prefabs");
        var sampleScene = EditorSceneManager.OpenScene(SamplePath, OpenSceneMode.Single);
        var race = GameObject.Find("Race");
        if (race == null) { Debug.LogError("EnsurePrefab: no 'Race' in SampleScene"); return; }
        PrefabUtility.SaveAsPrefabAssetAndConnect(race, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(sampleScene);
        Debug.Log($"EnsurePrefab: created {PrefabPath}");
    }

    static TrackLayout FindLayoutForScene(string scenePath)
    {
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        var guids = AssetDatabase.FindAssets("t:TrackLayout");
        // Heuristic: prefer a layout whose name contains the scene name.
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p).ToLower().Contains(sceneName.ToLower().Replace("track", "")))
                return AssetDatabase.LoadAssetAtPath<TrackLayout>(p);
        }
        // Fall back to first found.
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<TrackLayout>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}
