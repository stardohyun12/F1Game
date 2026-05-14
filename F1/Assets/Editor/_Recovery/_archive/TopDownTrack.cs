using UnityEditor;
using UnityEngine;

// Sets the SceneView camera to strict top-down orthographic over the active track.
public static class TopDownTrack
{
    public static void Execute()
    {
        var track = GameObject.Find("Track");
        if (track == null) return;
        var renderers = track.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        sv.orthographic = true;
        sv.LookAt(b.center, Quaternion.Euler(90f, 0f, 0f), Mathf.Max(b.size.x, b.size.z) * 0.55f, true);
        sv.Repaint();
    }
}
