using UnityEditor;
using UnityEngine;

public static class TopDownDiagnose
{
    public static void Execute()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) { Debug.LogError("No SceneView"); return; }

        var track = GameObject.Find("Track");
        if (track == null) { Debug.LogError("Track not found"); return; }

        // Compute bounds of all tiles
        var bounds = new Bounds(track.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in track.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        // Top-down orthographic
        sv.orthographic = true;
        sv.size = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f;
        sv.pivot = bounds.center;
        sv.rotation = Quaternion.Euler(90f, 0f, 0f);
        sv.Repaint();

        Debug.Log($"TopDown ready. bounds center={bounds.center} size={bounds.size}");
    }
}
