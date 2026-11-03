var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
  if (r.name.ToLower().Contains("sea") || r.name.ToLower().Contains("ocean") || r.name.ToLower().Contains("water"))
    sb.Append("R " + r.name + " b=" + r.bounds.center + " " + r.bounds.size + " col=" + (r.GetComponent<Collider>() != null) + " layer=" + r.gameObject.layer + "\n");
foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)) sb.Append("T " + t.name + " pos=" + t.transform.position + " size=" + t.terrainData.size + "\n");
bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
var pts = new System.Collections.Generic.List<(string, Vector3)>();
foreach (var p in DebugPlaces.All) if (p.feet != Vector3.zero) pts.Add((p.name, p.feet));
pts.Add(("depot_in?", new Vector3(229.4f, 27f, -57.6f)));
foreach (var (n, f) in pts) {
  Vector3 g = f; RaycastHit h;
  if (Physics.Raycast(f + Vector3.up * 1.5f, Vector3.down, out h, 6f, ~0, QueryTriggerInteraction.Ignore)) g = h.point;
  Vector3 e = g + Vector3.up * 1.6f;
  sb.Append(n + " ground=" + g.ToString("F1") + " gnd=" + (h.collider ? h.collider.name : "-"));
  string roof = "-"; if (Physics.Raycast(e + Vector3.up * 40f, Vector3.down, out h, 39.7f, ~0, QueryTriggerInteraction.Ignore)) roof = h.collider.name + "@" + (h.point.y - e.y).ToString("F1");
  string up = "-"; if (Physics.Raycast(e, Vector3.up, out h, 40f, ~0, QueryTriggerInteraction.Ignore)) up = h.collider.name + "@" + h.distance.ToString("F1");
  sb.Append(" roofDown=" + roof + " up=" + up + " H:");
  for (int i = 0; i < 8; i++) { var d = Quaternion.Euler(0, i * 45, 0) * Vector3.forward; sb.Append(Physics.Raycast(e, d, out h, 30f, ~0, QueryTriggerInteraction.Ignore) ? h.distance.ToString("F1") + "(" + h.collider.name + ") " : "x "); }
  sb.Append("\n");
}
Physics.queriesHitBackfaces = bf;
return sb.ToString();
