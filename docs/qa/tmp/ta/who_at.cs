// 그림3 칼날 자리(279, 31.2, -25) 둘레 3 m 에 걸친 렌더러·지형 메시 상태
var p = new Vector3(279f, 31.2f, -25f); var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    var b = r.bounds; b.Expand(3f); if (!b.Contains(p)) continue;
    var mf = r.GetComponent<MeshFilter>(); var m = mf != null ? mf.sharedMesh : null;
    sb.Append(r.name).Append(" | mesh ").Append(m != null ? m.name + " v" + m.vertexCount : "-").Append(" | mat ").Append(string.Join(",", System.Linq.Enumerable.Select(r.sharedMaterials, x => x == null ? "null" : x.name))).Append(" | ").Append(UnityEditor.AssetDatabase.GetAssetPath(m)).Append('\n');
}
var t = GameObject.Find("TownV6_Gray/TownV6_Terrain");
var mc = t.GetComponent<MeshCollider>(); var tf = t.GetComponent<MeshFilter>();
sb.Append("terrain filter v").Append(tf.sharedMesh.vertexCount).Append(" tris ").Append(tf.sharedMesh.triangles.Length / 3).Append(" · collider v").Append(mc.sharedMesh.vertexCount).Append(" same=").Append(mc.sharedMesh == tf.sharedMesh);
return sb.ToString();
