var sb = new System.Text.StringBuilder();
foreach (var n in new[]{"GARAGE_shell","HOUSE_shell","SHUTTER_01","TownV6_Terrain","DOOR_01"}) {
  var go = GameObject.Find(n); if (go == null) { sb.Append(n + " none\n"); continue; }
  var c = go.GetComponent<Collider>(); var r = go.GetComponent<Renderer>();
  sb.Append(n + " col=" + (c ? c.GetType().Name + " " + c.bounds.center.ToString("F1") + " " + c.bounds.size.ToString("F1") : "-") + " ren=" + (r ? r.bounds.center.ToString("F1") + " " + r.bounds.size.ToString("F1") : "-") + " parent=" + (go.transform.parent ? go.transform.parent.name : "-") + "\n");
  if (n=="GARAGE_shell" && go.transform.parent) foreach (Transform ch in go.transform.parent) { var cc = ch.GetComponent<Collider>(); sb.Append("  " + ch.name + (cc ? " C " + cc.bounds.center.ToString("F1") + " " + cc.bounds.size.ToString("F1") : "") + "\n"); }
}
return sb.ToString();
