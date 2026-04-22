using UnityEditor;
using UnityEngine;

// Place several straight-road variants side-by-side for visual inspection.
public static class PreviewStraightVariants
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";

    public static void Execute()
    {
        var host = GameObject.Find("StraightPreview");
        if (host != null) Object.DestroyImmediate(host);
        host = new GameObject("StraightPreview");
        host.transform.position = new Vector3(0, 0, -30);

        string[] files = { "road-straight.fbx", "road-crossroad-line.fbx", "road-square.fbx", "road-straight-half.fbx" };
        for (int i = 0; i < files.Length; i++)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + files[i]);
            if (asset == null) { Debug.LogWarning($"missing {files[i]}"); continue; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            inst.name = files[i];
            inst.transform.SetParent(host.transform, false);
            inst.transform.localPosition = new Vector3(i * 4, 0, 0);
            inst.transform.localScale = Vector3.one * 4f;
        }
    }
}
