using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DecorateTrack
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";
    const string BarrierPath = Root + "construction-barrier.fbx";
    const string ConePath = Root + "construction-cone.fbx";
    const string SignPath = Root + "sign-highway.fbx";

    const float Scale = 4f;
    const float RoadHalfWidth = 1.9f;
    const float DecorY = 0.05f;

    [MenuItem("PitCrew/Decorate Track")]
    public static void Execute()
    {
        var track = GameObject.Find("Track");
        if (track == null)
        {
            Debug.LogError("DecorateTrack: 'Track' not found. Run Build Test Track first.");
            return;
        }

        var existing = GameObject.Find("TrackDecor");
        if (existing != null) Object.DestroyImmediate(existing);

        var barrier = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierPath);
        var cone = AssetDatabase.LoadAssetAtPath<GameObject>(ConePath);
        var sign = AssetDatabase.LoadAssetAtPath<GameObject>(SignPath);
        if (barrier == null || cone == null || sign == null)
        {
            Debug.LogError($"DecorateTrack: missing asset. barrier={barrier} cone={cone} sign={sign}");
            return;
        }

        var decor = new GameObject("TrackDecor");
        Undo.RegisterCreatedObjectUndo(decor, "Decorate Track");

        int barrierCount = 0;
        int signCount = 0;
        int coneCount = 0;

        foreach (Transform tile in track.transform)
        {
            string n = tile.name;
            bool isStraight = n.StartsWith("Road_S_") || n.StartsWith("Road_M_") || n.StartsWith("Road_R_");
            if (!isStraight) continue;

            Vector3 p = tile.position;
            Quaternion tileRot = tile.rotation;
            // Road-straight tile's natural road axis is local +X, so local +Z (tile.forward) is perpendicular to road.
            // Use tile.forward for sides and tile.right for along-road offsets.
            Vector3 sideDir = tile.forward;
            Vector3 alongDir = tile.right;

            // Continuous side barriers: 4 per tile along the road axis to eliminate gaps.
            // Barrier's natural long axis is local +Z; rotate +90° relative to tile so its long axis aligns with tile's local +X (road axis).
            Quaternion barrierRot = tileRot * Quaternion.Euler(0, 90, 0);
            float[] alongOffsets = { -1.5f, -0.5f, 0.5f, 1.5f };
            for (int s = 0; s < alongOffsets.Length; s++)
            {
                float fo = alongOffsets[s];
                Place(barrier, decor, $"Barrier_{n}_L{s}", p + sideDir * RoadHalfWidth + alongDir * fo + Vector3.up * DecorY, barrierRot);
                Place(barrier, decor, $"Barrier_{n}_R{s}", p - sideDir * RoadHalfWidth + alongDir * fo + Vector3.up * DecorY, barrierRot);
                barrierCount += 2;
            }

            int idx = int.Parse(n.Substring(n.Length - 2));

            // Overhead sign gantry every 5 tiles on start straights only.
            // Sign's natural long axis is local Z (perpendicular to tile's road axis +X), so tileRot gives correct spanning orientation.
            if (n.StartsWith("Road_S_") && idx > 0 && idx % 5 == 0)
            {
                Place(sign, decor, $"Sign_{n}", p + Vector3.up * DecorY, tileRot);
                signCount++;
            }

            // Start-line cones across Road_S_00.
            if (n == "Road_S_00")
            {
                for (int k = -2; k <= 2; k++)
                {
                    Vector3 offset = sideDir * (k * 0.7f) + alongDir * 0.4f;
                    Place(cone, decor, $"Cone_Start_{coneCount++}", p + offset + Vector3.up * DecorY, tileRot);
                }
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"DecorateTrack: {barrierCount} barriers, {signCount} signs, {coneCount} cones.");
    }

    static void Place(GameObject source, GameObject parent, string name, Vector3 worldPos, Quaternion worldRot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = name;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.position = worldPos;
        go.transform.rotation = worldRot;
        go.transform.localScale = Vector3.one * Scale;
    }
}
