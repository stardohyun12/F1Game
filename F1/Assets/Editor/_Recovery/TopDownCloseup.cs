using UnityEditor;
using UnityEngine;

public static class TopDownCloseup
{
    public static void Execute()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        // bottom-left corner area
        sv.pivot = new Vector3(2f, 0f, 2f);
        sv.rotation = Quaternion.LookRotation(Vector3.down, Vector3.up);
        sv.size = 5f;
        sv.orthographic = true;
        sv.Repaint();
    }

    public static void Right()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        sv.pivot = new Vector3(22f, 0f, 2f);
        sv.rotation = Quaternion.LookRotation(Vector3.down, Vector3.up);
        sv.size = 5f;
        sv.orthographic = true;
        sv.Repaint();
    }
}
