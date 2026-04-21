using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DebugCurve
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";
    const string StraightPath = Root + "road-straight.fbx";
    const string CurvePath = Root + "road-curve.fbx";

    // Test each hypothesis for curve rot=0 connectors by placing 4 curves in a row,
    // each surrounded by 4 properly-oriented straights at ±X and ±Z.
    // For each side, the straight is oriented so its ROAD axis points INTO the curve tile edge.
    // The one that visually connects seamlessly reveals the curve's real connector edges.

    [MenuItem("PitCrew/Debug Curve")]
    public static void Execute()
    {
        var straight = AssetDatabase.LoadAssetAtPath<GameObject>(StraightPath);
        var curve = AssetDatabase.LoadAssetAtPath<GameObject>(CurvePath);

        var existing = GameObject.Find("DebugCurve");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("DebugCurve");
        root.transform.position = new Vector3(60f, 0f, -40f);

        // Single curve rot=0 at origin. Straights on 4 sides, each oriented so their road
        // perpendicular-enters that edge of the curve (so the one that visually continues
        // the road shows that edge is a connector).
        Place(curve, root, "Curve", Vector3.zero, Quaternion.identity);

        // +X side: place at (6, 0, 0). Road should flow along X to enter curve's +X edge.
        // Straight rot 0 has road along Z. To make road along X, rot 90.
        Place(straight, root, "Test_PosX", new Vector3(6, 0, 0), Quaternion.Euler(0, 90, 0));
        // -X side: same idea.
        Place(straight, root, "Test_NegX", new Vector3(-6, 0, 0), Quaternion.Euler(0, 90, 0));
        // +Z side: road along Z. rot 0.
        Place(straight, root, "Test_PosZ", new Vector3(0, 0, 6), Quaternion.identity);
        // -Z side: road along Z. rot 0.
        Place(straight, root, "Test_NegZ", new Vector3(0, 0, -6), Quaternion.identity);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("DebugCurve placed.");
    }

    static void Place(GameObject source, GameObject parent, string name, Vector3 localPos, Quaternion rot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = name;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one * 4f;
    }
}
