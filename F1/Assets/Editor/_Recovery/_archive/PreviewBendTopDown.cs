using UnityEditor;
using UnityEngine;

public static class PreviewBendTopDown
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";

    public static void Execute()
    {
        var host = GameObject.Find("BendPreview");
        if (host != null) Object.DestroyImmediate(host);
        host = new GameObject("BendPreview");
        host.transform.position = new Vector3(0, 0, -20);

        // Each rotation 0/90/180/270 at x = 0, 4, 8, 12
        for (int i = 0; i < 4; i++)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "road-bend.fbx");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            inst.name = $"bend_r{i * 90}";
            inst.transform.SetParent(host.transform, false);
            inst.transform.localPosition = new Vector3(i * 4, 0, 0);
            inst.transform.localRotation = Quaternion.Euler(0, i * 90, 0);
            inst.transform.localScale = Vector3.one * 4f;
        }

        // Set scene view to top-down orthographic on host
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.orthographic = true;
            sv.LookAt(host.transform.position + new Vector3(6, 0, 0), Quaternion.Euler(90, 0, 0), 10f, true);
            sv.Repaint();
        }
    }
}
