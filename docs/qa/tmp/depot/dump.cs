// 보관소 계층·경계 덤프(과제 20261102-1515)
var root = GameObject.Find("D198_depot").transform;
var sb = new System.Text.StringBuilder();
var b = new Bounds(root.position, Vector3.zero); foreach (var r in root.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
sb.AppendLine("bounds c=" + b.center.ToString("F1") + " size=" + b.size.ToString("F1") + " rotY=" + root.eulerAngles.y.ToString("F1"));
foreach (var t in root.GetComponentsInChildren<Transform>(true)) {
  int dep = 0; for (var p = t; p != root && p != null; p = p.parent) dep++;
  if (dep > 3) continue;
  var col = t.GetComponent<Collider>(); var rr = t.GetComponent<Renderer>();
  string bb = rr != null ? " rb c=" + rr.bounds.center.ToString("F1") + " s=" + rr.bounds.size.ToString("F1") : "";
  string cb = col != null ? " col=" + col.GetType().Name + (col.enabled ? "" : "(off)") + " cb s=" + col.bounds.size.ToString("F1") : "";
  sb.AppendLine(new string(' ', dep * 2) + t.name + bb + cb + (t.gameObject.activeInHierarchy ? "" : " [off]"));
}
return sb.ToString();
