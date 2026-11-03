var sb = new System.Text.StringBuilder();
Vector3 bestRoad = Vector3.zero, bestT = Vector3.zero; string rn = "";
for (float x = -200; x <= 320; x += 4) for (float z = -330; z <= 150; z += 4) {
  RaycastHit h; if (!Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out h, 400, ~0, QueryTriggerInteraction.Ignore)) continue;
  string n = h.collider.name;
  if ((n.StartsWith("ROAD") || n.Contains("Walkway")) && h.point.y > bestRoad.y) { bestRoad = h.point; rn = n; }
  if (n == "TownV6_Terrain" && h.point.y > bestT.y && Mathf.Abs(x) < 250 && z > -300 && z < 120) bestT = h.point;
}
sb.Append("road max " + bestRoad.ToString("F1") + " " + rn + "\nterrain max " + bestT.ToString("F1") + "\n");
var g = GameObject.Find("garage_v5_combo"); sb.Append("garage children up-probe: ");
bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
foreach (var p in new[]{ new Vector3(216f, 28.3f, -55.5f), new Vector3(219f, 28.3f, -53f) }) {
  RaycastHit h; string roof = Physics.Raycast(p + Vector3.up * 40, Vector3.down, out h, 39.7f, ~0, QueryTriggerInteraction.Ignore) ? h.collider.name + "@" + (h.point.y - p.y).ToString("F1") : "-";
  sb.Append(p + " roof=" + roof + " H:");
  for (int i = 0; i < 8; i++) { var d = Quaternion.Euler(0, i * 45, 0) * Vector3.forward; sb.Append(Physics.Raycast(p, d, out h, 30f, ~0, QueryTriggerInteraction.Ignore) ? h.distance.ToString("F1") + "(" + h.collider.name + ") " : "x "); }
  sb.Append("\n");
}
Physics.queriesHitBackfaces = bf;
return sb.ToString();
