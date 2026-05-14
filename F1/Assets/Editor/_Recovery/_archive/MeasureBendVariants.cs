using System.Text;
using UnityEditor;
using UnityEngine;

public static class MeasureBendVariants
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";

    public static void Execute()
    {
        var sb = new StringBuilder();
        string[] files = {
            "road-straight.fbx",
            "road-curve.fbx",
            "road-bend.fbx",
            "road-bend-sidewalk.fbx",
            "road-bend-square.fbx",
            "road-curve-pavement.fbx",
            "road-crossing.fbx",
        };
        foreach (var f in files) Measure(sb, f);
        Debug.Log(sb.ToString());
    }

    static void Measure(StringBuilder sb, string filename)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + filename);
        if (asset == null) { sb.AppendLine($"{filename}: NOT FOUND"); return; }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        var renderers = instance.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) { sb.AppendLine($"{filename}: no MeshRenderers"); Object.DestroyImmediate(instance); return; }
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        sb.AppendLine($"{filename}: size={b.size}");
        Object.DestroyImmediate(instance);
    }
}
