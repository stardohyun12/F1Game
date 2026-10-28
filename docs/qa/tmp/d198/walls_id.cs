// Walls_0 정체: 어느 오브젝트·어떤 콜라이더·어떤 메시인지, 같은 이름이 몇 개인지
var sb = new System.Text.StringBuilder();
foreach (var c in GameObject.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.name.StartsWith("Walls") || c.name.StartsWith("Roofs"))) {
  var t = c.transform; string path = t.name; while (t.parent) { t = t.parent; path = t.name + "/" + path; }
  var mc = c as MeshCollider; var mf = c.GetComponent<MeshFilter>();
  sb.Append(path + " | " + c.GetType().Name + " enabled=" + c.enabled + " active=" + c.gameObject.activeInHierarchy + " layer=" + LayerMask.LayerToName(c.gameObject.layer)
    + " colMesh=" + (mc && mc.sharedMesh ? mc.sharedMesh.name + " tris" + mc.sharedMesh.triangles.Length / 3 + " " + UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh) : "-")
    + " renderMesh=" + (mf && mf.sharedMesh ? mf.sharedMesh.name + " " + UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh) : "-") + "\n");
}
return sb.ToString();
