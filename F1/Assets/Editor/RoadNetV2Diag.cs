using System.Linq;
using System.Text;
using UnityEngine;

// 과제 1302 진단: 사각형 안 RoadsV2 물체(이름·재질·범위·정점·윗면 넓이)
public static partial class BuildBayLoop
{
    public static string RoadNetV2Diag(float x0, float z0, float x1, float z1)
    {
        var top = GameObject.Find("TownV6_Gray").transform.Find("RoadsV2"); var sb = new StringBuilder();
        foreach (Transform t in top)
        {
            var mr = t.GetComponent<MeshRenderer>(); if (mr == null) continue; var b = mr.bounds;
            if (b.max.x < x0 || b.min.x > x1 || b.max.z < z0 || b.min.z > z1) continue;
            var mc = t.GetComponent<MeshCollider>(); var m = mc != null && mc.sharedMesh != null ? mc.sharedMesh : t.GetComponent<MeshFilter>().sharedMesh;
            var v = m.vertices.Select(q => t.TransformPoint(q)).ToArray(); var tri = m.triangles; float area = 0;
            for (int i = 0; i < tri.Length; i += 3) area += Mathf.Abs(Vector3.Cross(v[tri[i + 1]] - v[tri[i]], v[tri[i + 2]] - v[tri[i]]).y) * .5f;
            sb.AppendLine($"{t.name} {mr.sharedMaterial?.name} ({b.min.x:F0},{b.min.z:F0})~({b.max.x:F0},{b.max.z:F0}) y {b.min.y:F1}~{b.max.y:F1} v{v.Length} 넓이 {area:F0}");
        }
        return sb.ToString();
    }
}
