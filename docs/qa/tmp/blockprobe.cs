var sb = new System.Text.StringBuilder();
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var p = d.Player.transform.position;
sb.AppendLine("player " + p.ToString("F2"));
var to = new UnityEngine.Vector3(272.2f, 0, -37.4f);
var dirv = new UnityEngine.Vector3(to.x - p.x, 0, to.z - p.z).normalized;
for (float s = 0f; s <= 8f; s += 0.5f) {
  var q = p + dirv * s;
  string g = "-";
  if (UnityEngine.Physics.Raycast(new UnityEngine.Vector3(q.x, p.y + 3f, q.z), UnityEngine.Vector3.down, out var h, 8f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) g = h.point.y.ToString("F2") + " " + h.collider.name;
  sb.AppendLine("s=" + s + " ground " + g);
}
for (float hy = 0.2f; hy <= 1.6f; hy += 0.4f)
  if (UnityEngine.Physics.Raycast(p + UnityEngine.Vector3.up * hy, dirv, out var hh, 3f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) sb.AppendLine("ray y+" + hy + " hit " + hh.collider.name + " d=" + hh.distance.ToString("F2"));
return sb.ToString();
