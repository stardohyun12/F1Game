using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildTestTrack
{
    const string StraightPath = "Assets/Model/kenney_city-kit-roads/Models/FBX format/road-straight.fbx";
    const string CurvePath = "Assets/Model/kenney_city-kit-roads/Models/FBX format/road-curve.fbx";
    const float TileScale = 4f;
    const float TileSize = 4f;
    const float TileY = 0.05f;

    public static void Execute()
    {
        var straight = AssetDatabase.LoadAssetAtPath<GameObject>(StraightPath);
        var curve = AssetDatabase.LoadAssetAtPath<GameObject>(CurvePath);
        if (straight == null || curve == null)
        {
            Debug.LogError($"Missing asset. straight={straight} curve={curve}");
            return;
        }

        var existing = GameObject.Find("Track");
        if (existing != null) Object.DestroyImmediate(existing);

        var track = new GameObject("Track");
        Undo.RegisterCreatedObjectUndo(track, "Build Test Track");

        // 8 straights +Z, starting at Race origin
        for (int i = 0; i < 8; i++)
        {
            Place(straight, track, $"Road_S_{i:00}", new Vector3(0, TileY, i * TileSize), Quaternion.identity);
        }

        // Curve is 2x2 tile bounds. Center at z = (8 + 0.5) * TileSize so it sits after last straight.
        float curveCenterZ = 8.5f * TileSize; // = 34
        Place(curve, track, "Road_C_00", new Vector3(0, TileY, curveCenterZ), Quaternion.Euler(0, 180, 0));

        // 4 straights +X after curve (curve rotated 180° exits to +X).
        for (int i = 1; i <= 4; i++)
        {
            float ex = (0.5f + i) * TileSize;
            Place(straight, track, $"Road_E_{i:00}", new Vector3(ex, TileY, curveCenterZ), Quaternion.Euler(0, 90, 0));
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("BuildTestTrack: placed 8 straights + 1 curve + 4 straights.");
    }

    static void Place(GameObject source, GameObject parent, string name, Vector3 pos, Quaternion rot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = name;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one * TileScale;
    }
}
