var sb = new System.Text.StringBuilder();
var scn = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
foreach (var go in scn.GetRootGameObjects()) {
  foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true)) {
    string n = t.name.ToLower();
    if (n.Contains("sign") || n.Contains("간판") || n.Contains("shutter") || n.Contains("셔터") || n.Contains("parcel") || n.Contains("택배") || n.Contains("icon"))
      sb.AppendLine(t.name + " " + t.position.ToString("F1") + " act=" + t.gameObject.activeInHierarchy + " parent=" + (t.parent != null ? t.parent.name : "-"));
  }
}
return sb.ToString();
