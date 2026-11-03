var sb = new System.Text.StringBuilder();
sb.Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path).Append('\n');
foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
  sb.Append(r.name).Append(r.activeSelf?"":" [off]").Append(" ch=").Append(r.transform.childCount).Append('\n');
  if (r.name=="TownV6_Gray"||r.name.Contains("Terr")||r.name.Contains("Walk")) foreach (Transform c in r.transform) {
    var mf=c.GetComponent<MeshFilter>(); var mc=c.GetComponent<MeshCollider>(); var mr=c.GetComponent<MeshRenderer>();
    sb.Append("  ").Append(c.name).Append(c.gameObject.activeInHierarchy?"":" [off]").Append(" ch=").Append(c.childCount);
    if (mf&&mf.sharedMesh) sb.Append(" mesh=").Append(UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh)).Append(":").Append(mf.sharedMesh.name).Append(" v=").Append(mf.sharedMesh.vertexCount);
    if (mc) sb.Append(" col=").Append(mc.sharedMesh?mc.sharedMesh.name:"null");
    if (mr) { sb.Append(" mats="); foreach (var m in mr.sharedMaterials) sb.Append(m?m.name:"NULL").Append(','); }
    sb.Append('\n');
  }
}
return sb.ToString();
