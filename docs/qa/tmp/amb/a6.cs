var sb = new System.Text.StringBuilder();
bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
foreach (var f in new[]{ new Vector3(244f, 64.8f, 118f), new Vector3(320f, 48.8f, 78f) }) {
  RaycastHit h; Vector3 g = f;
  if (Physics.Raycast(f + Vector3.up * 3f, Vector3.down, out h, 10f, ~0, QueryTriggerInteraction.Ignore)) g = h.point;
  float slope = Vector3.Angle(h.normal, Vector3.up);
  var p = g + Vector3.up * 1.6f;
  sb.Append(f + " ground=" + g.ToString("F1") + " slope=" + slope.ToString("F0") + " H:");
  for (int i = 0; i < 8; i++) { var d = Quaternion.Euler(0, i * 45, 0) * Vector3.forward; sb.Append(Physics.Raycast(p, d, out h, 30f, ~0, QueryTriggerInteraction.Ignore) ? h.distance.ToString("F1") + "(" + h.collider.name + ") " : "x "); }
  sb.Append("\n");
}
Physics.queriesHitBackfaces = bf;
return sb.ToString();
