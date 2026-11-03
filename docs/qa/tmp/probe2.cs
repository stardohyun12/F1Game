var sb = new System.Text.StringBuilder();
var q = UnityEngine.GameObject.Find("q3_depot");
if (q == null) return "no q3_depot";
sb.AppendLine("q3_depot " + q.transform.position.ToString("F1") + " parent=" + (q.transform.parent != null ? q.transform.parent.name : "-"));
var seen = new System.Collections.Generic.HashSet<string>();
foreach (var t in q.GetComponentsInChildren<UnityEngine.Transform>(true)) {
  string k = System.Text.RegularExpressions.Regex.Replace(t.name, "[0-9]+$", "");
  if (!seen.Add(k)) continue;
  var r = t.GetComponent<UnityEngine.Renderer>();
  sb.AppendLine(t.name + " " + t.position.ToString("F1") + (r != null ? " mat=" + r.sharedMaterial.name : ""));
}
return sb.ToString();
