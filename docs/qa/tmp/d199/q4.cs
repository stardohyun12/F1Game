var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder();
foreach (var r in s.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == "Garage") {
  string c = ""; var q = t; while (q != null) { c = q.name + "/" + c; q = q.parent; }
  var mf = t.GetComponent<UnityEngine.MeshFilter>(); var rd = t.GetComponent<UnityEngine.Renderer>();
  sb.Append(c + " active=" + t.gameObject.activeInHierarchy + " mesh=" + (mf && mf.sharedMesh ? mf.sharedMesh.name : "-") + " size=" + (rd ? rd.bounds.size.ToString("F1") : "-") + " prefabInst=" + UnityEditor.PrefabUtility.IsPartOfPrefabInstance(t) + "\n");
}
return sb.ToString();
