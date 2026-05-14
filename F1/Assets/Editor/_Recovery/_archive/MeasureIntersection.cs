using UnityEditor;
using UnityEngine;

// Drops road-intersection.fbx at origin (rot 0, scale matches tile CellSize of 6m)
// so we can capture it top-down and determine which side is the "missing" leg of the T.
public static class MeasureIntersection
{
    public static void Execute()
    {
        var old = GameObject.Find("__IntersectionProbe");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("__IntersectionProbe");
        string path = "Assets/Model/kenney_city-kit-roads/Models/FBX format/road-intersection.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogError($"MISS: {path}"); return; }

        // CellSize = 6m, native ~1m → scale 6.
        // Place probe far outside the map so it doesn't overlap existing tiles.
        Vector3 probePos = new Vector3(-60f, 0f, -60f);
        root.transform.position = probePos;

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.SetParent(root.transform, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one * 6f;

        // Add axis markers: red +X, blue +Z for visual reference
        var xm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        xm.name = "AxisMarker_PlusX";
        xm.transform.SetParent(root.transform);
        xm.transform.localPosition = new Vector3(4f, 0.5f, 0f);
        xm.transform.localScale = new Vector3(1f, 1f, 0.3f);
        var xmr = xm.GetComponent<MeshRenderer>();
        if (xmr != null) xmr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.red };

        var zm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zm.name = "AxisMarker_PlusZ";
        zm.transform.SetParent(root.transform);
        zm.transform.localPosition = new Vector3(0f, 0.5f, 4f);
        zm.transform.localScale = new Vector3(0.3f, 1f, 1f);
        var zmr = zm.GetComponent<MeshRenderer>();
        if (zmr != null) zmr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.blue };

        // Top-down view framing the probe
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.orthographic = true;
            sv.size = 5f;
            sv.pivot = probePos;
            sv.rotation = Quaternion.Euler(90f, 0f, 0f);
            sv.Repaint();
        }
        Debug.Log("MeasureIntersection: placed road-intersection at origin (rot 0). Red = +X (east), Blue = +Z (north).");
    }

    public static void Cleanup()
    {
        var old = GameObject.Find("__IntersectionProbe");
        if (old != null) Object.DestroyImmediate(old);
        Debug.Log("MeasureIntersection: probe cleaned.");
    }
}
