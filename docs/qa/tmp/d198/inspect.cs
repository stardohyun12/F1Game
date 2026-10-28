var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder(); sb.Append(sc.path + "\n");
foreach (var r in sc.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(true)) {
  var n = t.name;
  bool hit = (n.StartsWith("Q") && n.Contains("_")) || n.StartsWith("TUT_") || n.StartsWith("Shot_") || n.Contains("Sun") || n.StartsWith("Audio_") || n.StartsWith("Directional");
  if (!hit) continue;
  if (t.parent != null && t.parent.name.StartsWith("TUT_") && !n.StartsWith("TUT_")) continue;
  sb.Append(n + " | par=" + (t.parent ? t.parent.name : "-") + " | p=" + t.position.ToString("F1") + " | e=" + t.eulerAngles.ToString("F0") + " | s=" + t.lossyScale.ToString("F1") + " | act=" + t.gameObject.activeSelf + "\n");
}
return sb.ToString();
