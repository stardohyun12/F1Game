var sb = new System.Text.StringBuilder();
var gray = GameObject.Find("TownV6_Gray").transform;
foreach (Transform c in gray) {
  sb.Append(c.name + " n=" + c.childCount + "\n");
  int k = 0; foreach (Transform cc in c) { if (k++ > 6) break; var mf = cc.GetComponent<MeshFilter>(); var mc = cc.GetComponent<MeshCollider>();
    sb.Append("   " + cc.name + " ch=" + cc.childCount + (mf && mf.sharedMesh ? " mesh=" + mf.sharedMesh.name + " v=" + mf.sharedMesh.vertexCount + " sub=" + mf.sharedMesh.subMeshCount + " " + UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh) : "") + (mc ? " MC=" + (mc.sharedMesh ? mc.sharedMesh.name : "null") : "") + " " + string.Join(",", cc.GetComponents<Component>().Select(x => x.GetType().Name)) + "\n"); }
}
var room = GameObject.Find("D198_room").transform; sb.Append("room pos=" + room.position.ToString("F2") + " rot=" + room.eulerAngles.ToString("F1") + "\n");
var t = GameObject.Find("TownV6_Terrain"); var tm = t.GetComponent<MeshFilter>().sharedMesh; sb.Append("terrain " + tm.name + " v=" + tm.vertexCount + " bounds=" + tm.bounds + " pos=" + t.transform.position + " scale=" + t.transform.lossyScale + " asset=" + UnityEditor.AssetDatabase.GetAssetPath(tm) + " MC=" + (t.GetComponent<MeshCollider>()?.sharedMesh?.name) + "\n");
return sb.ToString();
