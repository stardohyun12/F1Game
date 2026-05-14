using System.Text;
using UnityEditor;
using UnityEngine;

public static class MeasureTiles
{
    const string Root = "Assets/Model/kenney_city-kit-roads/Models/FBX format/";

    public static void Execute()
    {
        var sb = new StringBuilder();
        Measure(sb, "road-straight.fbx");
        Measure(sb, "road-curve.fbx");
        Measure(sb, "road-intersection.fbx");
        Measure(sb, "road-crossroad.fbx");
        Measure(sb, "road-end.fbx");
        Debug.Log(sb.ToString());
    }

    static void Measure(StringBuilder sb, string filename)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + filename);
        if (asset == null) { sb.AppendLine($"{filename}: NOT FOUND"); return; }

        // Instantiate with scale 1 to read native size
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        var renderers = instance.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) { sb.AppendLine($"{filename}: no MeshRenderers"); Object.DestroyImmediate(instance); return; }

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);

        sb.AppendLine($"=== {filename} (scale 1) ===");
        sb.AppendLine($"  bounds.center = {combined.center}");
        sb.AppendLine($"  bounds.size   = {combined.size}");
        sb.AppendLine($"  bounds.min    = {combined.min}");
        sb.AppendLine($"  bounds.max    = {combined.max}");
        sb.AppendLine($"  pivot offset (transform vs bounds.center) = {Vector3.zero - combined.center}");

        Object.DestroyImmediate(instance);
    }
}
