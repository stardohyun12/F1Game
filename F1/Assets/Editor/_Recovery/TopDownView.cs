using UnityEditor;
using UnityEngine;

public static class TopDownView
{
    public static void Execute()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) { Debug.LogError("No SceneView"); return; }
        sv.pivot = new Vector3(12f, 0f, 8f);
        sv.rotation = Quaternion.LookRotation(Vector3.down, Vector3.up);
        sv.size = 18f;
        sv.orthographic = true;
        sv.Repaint();
    }
}
