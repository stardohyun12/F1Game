using UnityEditor;
using UnityEngine;

public static class SideView
{
    public static void Execute()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) { Debug.LogError("No SceneView"); return; }

        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogError("Track not found"); return; }

        var bounds = new Bounds(track.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in track.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        // Encapsulate tall buildings too
        var buildings = GameObject.Find("Buildings");
        if (buildings != null)
            foreach (var r in buildings.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

        // Low-angle perspective from south-east, looking at Downtown
        sv.orthographic = false;
        sv.pivot = bounds.center;
        sv.rotation = Quaternion.Euler(20f, -30f, 0f);
        sv.size = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.5f;
        sv.Repaint();

        Debug.Log($"SideView ready. center={bounds.center} size={bounds.size}");
    }
}
