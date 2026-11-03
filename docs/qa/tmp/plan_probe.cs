var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder();
sb.Append("scene=" + s.path + " dirty=" + s.isDirty + " play=" + UnityEditor.EditorApplication.isPlaying + "\n");
foreach (var r in s.GetRootGameObjects()) {
  sb.Append("ROOT " + r.name + " active=" + r.activeSelf + " ch=" + r.transform.childCount + "\n");
}
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) {
  var go = t.gameObject;
  if (go.scene != s) continue;
  string pp = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
  bool isRoot = UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(go);
  if ((isRoot && pp.Contains("Art/Library/Buildings")) || go.name.StartsWith("TUT") || go.name.Contains("Tutorial") || go.name.Contains("Opening")) {
    string path = go.name; var p = t.parent; while (p != null) { path = p.name + "/" + path; p = p.parent; }
    sb.Append(path + " | " + pp + " | " + t.position.ToString("F2") + " yaw=" + t.eulerAngles.y.ToString("F1") + " act=" + go.activeInHierarchy + " ch=" + t.childCount + " comps=" + string.Join(",", System.Linq.Enumerable.Select(go.GetComponents<UnityEngine.Component>(), c => c == null ? "null" : c.GetType().Name)) + "\n");
  }
}
return sb.ToString();
