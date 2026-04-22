using System.Text;
using UnityEditor;
using UnityEngine;

// Records pivot offset = (bounds.center) since we instantiate at origin with scale 1.
// If bounds.center != (0, y, 0), the prefab's pivot is off-center in xz and we need to
// translate when placing to put mesh center on the cell center.
public static class MeasureBuildingPivots
{
    static readonly (string label, string root, string[] files)[] Kits =
    {
        ("SUBURBAN", "Assets/Model/kenney_city-kit-suburban_20/Models/FBX format/", new[]
        {
            "building-type-k.fbx","building-type-r.fbx","building-type-l.fbx",
            "building-type-q.fbx","building-type-a.fbx","building-type-g.fbx",
        }),
        ("COMMERCIAL", "Assets/Model/kenney_city-kit-commercial_2.1/Models/FBX format/", new[]
        {
            "building-a.fbx","building-d.fbx","building-m.fbx","building-l.fbx",
        }),
    };

    public static void Execute()
    {
        var sb = new StringBuilder();
        foreach (var (label, root, files) in Kits)
        {
            sb.AppendLine($"=== {label} ===");
            foreach (var f in files)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(root + f);
                if (asset == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                var rs = inst.GetComponentsInChildren<MeshRenderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    sb.AppendLine($"  {f,-28} center=({b.center.x:0.00},{b.center.y:0.00},{b.center.z:0.00}) min.y={b.min.y:0.00}");
                }
                Object.DestroyImmediate(inst);
            }
        }
        Debug.Log(sb.ToString());
    }
}
