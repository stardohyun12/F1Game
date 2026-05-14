using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// Measures footprint (xz bounds) of each building FBX so we can pick 1x1-friendly candidates.
// Prints size.x, size.z, max(x,z) — max is what matters for cell-fit scaling.
public static class MeasureBuildings
{
    static readonly (string label, string root, string[] files)[] Kits =
    {
        ("SUBURBAN", "Assets/Model/kenney_city-kit-suburban_20/Models/FBX format/", new[]
        {
            "building-type-a.fbx","building-type-b.fbx","building-type-c.fbx","building-type-d.fbx",
            "building-type-e.fbx","building-type-f.fbx","building-type-g.fbx","building-type-h.fbx",
            "building-type-i.fbx","building-type-j.fbx","building-type-k.fbx","building-type-l.fbx",
            "building-type-m.fbx","building-type-n.fbx","building-type-o.fbx","building-type-p.fbx",
            "building-type-q.fbx","building-type-r.fbx","building-type-s.fbx","building-type-t.fbx",
            "building-type-u.fbx",
        }),
        ("COMMERCIAL", "Assets/Model/kenney_city-kit-commercial_2.1/Models/FBX format/", new[]
        {
            "building-a.fbx","building-b.fbx","building-c.fbx","building-d.fbx",
            "building-e.fbx","building-f.fbx","building-g.fbx","building-h.fbx",
            "building-i.fbx","building-j.fbx","building-k.fbx","building-l.fbx",
            "building-m.fbx","building-n.fbx",
        }),
        ("SKYSCRAPER", "Assets/Model/kenney_city-kit-commercial_2.1/Models/FBX format/", new[]
        {
            "building-skyscraper-a.fbx","building-skyscraper-b.fbx","building-skyscraper-c.fbx",
            "building-skyscraper-d.fbx","building-skyscraper-e.fbx",
        }),
    };

    [MenuItem("PitCrew/Measure Buildings (log to console)")]
    public static void Execute()
    {
        var sb = new StringBuilder();
        foreach (var (label, root, files) in Kits)
        {
            sb.AppendLine($"=== {label} ===");
            var rows = new List<(string name, float x, float z, float max)>();
            foreach (var f in files)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(root + f);
                if (asset == null) { sb.AppendLine($"  {f}: NOT FOUND"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                var renderers = inst.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0) { Object.DestroyImmediate(inst); continue; }
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                rows.Add((f, b.size.x, b.size.z, Mathf.Max(b.size.x, b.size.z)));
                Object.DestroyImmediate(inst);
            }
            rows.Sort((a, b) => a.max.CompareTo(b.max));
            foreach (var r in rows)
                sb.AppendLine($"  {r.name,-30} x={r.x:0.00}  z={r.z:0.00}  max={r.max:0.00}");
        }
        Debug.Log(sb.ToString());
    }
}
