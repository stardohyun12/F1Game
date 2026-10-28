var sb = new System.Text.StringBuilder();
foreach (var p in new[] { new Vector2(185, -1), new Vector2(185, -200), new Vector2(0, 0), new Vector2(185, -61) }) {
  var hs = Physics.RaycastAll(new Vector3(p.x, 300, p.y), Vector3.down, 600, ~0, QueryTriggerInteraction.Collide);
  sb.Append(p + " hits " + hs.Length + ": " + string.Join(", ", hs.Select(h => h.collider.name + "@" + h.point.y.ToString("F1") + (h.collider.isTrigger ? "T" : ""))) + "\n");
}
var eye = new Vector3(185, 28.8f, -61);
foreach (var d in new[] { new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(1, 0, 0) }) {
  var t = eye + d * 300; t.y = 0.5f;
  if (Physics.Linecast(eye, t, out var hit)) sb.Append("LC " + d + " hit " + hit.collider.name + " d " + hit.distance.ToString("F1") + " at " + hit.point.ToString("F1") + "\n"); else sb.Append("LC " + d + " clear\n");
}
sb.Append("autoSync " + Physics.autoSyncTransforms + " simMode " + Physics.simulationMode + "\n");
return sb.ToString();
