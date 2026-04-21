using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildTestTrack
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";
    const string StraightPath = Root + "road-straight.fbx";
    const string StraightBarrierPath = Root + "road-straight-barrier.fbx";
    const string CurvePath = Root + "road-curve.fbx";
    const string CurveBarrierPath = Root + "road-curve-barrier.fbx";

    const float TileScale = 4f;
    const float TileSize = 4f;
    const float TileY = 0.05f;

    [MenuItem("PitCrew/Build Test Track")]
    public static void Execute()
    {
        var straight = AssetDatabase.LoadAssetAtPath<GameObject>(StraightPath);
        var straightBarrier = AssetDatabase.LoadAssetAtPath<GameObject>(StraightBarrierPath);
        var curve = AssetDatabase.LoadAssetAtPath<GameObject>(CurvePath);
        var curveBarrier = AssetDatabase.LoadAssetAtPath<GameObject>(CurveBarrierPath);
        if (straight == null || curve == null || straightBarrier == null || curveBarrier == null)
        {
            Debug.LogError("BuildTestTrack: missing source asset(s).");
            return;
        }

        var existing = GameObject.Find("Track");
        if (existing != null) Object.DestroyImmediate(existing);

        var track = new GameObject("Track");
        Undo.RegisterCreatedObjectUndo(track, "Build Test Track");

        // Start segment: 10 straights along +Z. rot 90 so tile's road axis aligns with Z (crosswalks on sides).
        for (int i = 0; i < 10; i++)
        {
            var p = new Vector3(0, TileY, i * TileSize);
            Place(straight, track, $"Road_S_{i:00}", p, Quaternion.Euler(0, 90, 0));
        }

        // Curve 1: rot 180 gives connectors at -Z (meets start) and -X (leads to middle).
        float curve1Z = 10.5f * TileSize;
        var c1 = new Vector3(0, TileY, curve1Z);
        Place(curve, track, "Road_C_01", c1, Quaternion.Euler(0, 180, 0));

        // Middle segment: 8 straights along -X. rot 0 so tile's road axis aligns with X (crosswalks on sides).
        for (int i = 1; i <= 8; i++)
        {
            float ex = -(0.5f + i) * TileSize;
            var p = new Vector3(ex, TileY, curve1Z);
            Place(straight, track, $"Road_M_{i:00}", p, Quaternion.identity);
        }

        // Curve 2: rot 90 gives connectors at +X (meets middle's -X end) and -Z (leads to return).
        float curve2X = -10f * TileSize; // = -40, so +X edge at -36
        var c2 = new Vector3(curve2X, TileY, curve1Z);
        Place(curve, track, "Road_C_02", c2, Quaternion.Euler(0, 90, 0));

        // Return segment: 10 straights along -Z (rot 180). Start at z=36 so +Z edge meets curve2's -Z edge at z=38.
        float returnZStart = curve1Z - 1.5f * TileSize; // = 36
        for (int i = 1; i <= 10; i++)
        {
            float ez = returnZStart - (i - 1) * TileSize;
            var p = new Vector3(curve2X, TileY, ez);
            Place(straight, track, $"Road_R_{i:00}", p, Quaternion.Euler(0, 90, 0));
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("BuildTestTrack: road + barrier overlay pairs placed (U-turn layout).");
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
