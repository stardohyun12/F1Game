using UnityEditor;
using UnityEngine;

// Single-menu rebuild: road + buildings from the first TrackLayout SO found.
public static class BuildFromLayoutMenu
{
    [MenuItem("PitCrew/Build Everything From Layout")]
    public static void Build()
    {
        var guids = AssetDatabase.FindAssets("t:TrackLayout");
        if (guids.Length == 0) { Debug.LogError("No TrackLayout SO found. Create one via Assets > Create > PitCrew > Track Layout."); return; }
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(path);
        Debug.Log($"BuildFromLayout: using {path}");
        var track = TileMapAuto.BuildFromLayout(layout);
        BuildingPlacer.Build(layout);
        FrameSceneViewTopDown(track);
    }

    // Snap the SceneView to strict top-down orthographic on the built track so the
    // user can immediately visually verify placement without manual camera work.
    static void FrameSceneViewTopDown(GameObject track)
    {
        if (track == null) return;
        var renderers = track.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        sv.orthographic = true;
        sv.LookAt(b.center, Quaternion.Euler(90f, 0f, 0f), Mathf.Max(b.size.x, b.size.z) * 0.6f, true);
        sv.Repaint();
    }
}
