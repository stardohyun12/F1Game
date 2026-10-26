var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var f = car.transform.forward; f.y = 0; f.Normalize(); var rt = Vector3.Cross(Vector3.up, f); var s = new System.Text.StringBuilder();
foreach (float lat in new[] { -1.2f, 0f, 1.2f })
{
  s.Append($"lat{lat}: ");
  for (float d = -3f; d <= 6f; d += .5f)
  {
    var o = rb.position + f * d + rt * lat + Vector3.up * 5f;
    if (Physics.Raycast(o, Vector3.down, out var h, 12f, ~0, QueryTriggerInteraction.Ignore)) s.Append($"{h.point.y - rb.position.y:F2}{(h.collider.name.StartsWith("ROAD") ? "" : "*" + h.collider.name)} ");
    else s.Append("x ");
  }
  s.AppendLine();
}
foreach (float hh in new[] { .2f, .4f, .7f }) { if (Physics.Raycast(rb.position + Vector3.up * hh, f, out var h2, 6f, ~0, QueryTriggerInteraction.Ignore)) s.Append($"fwd@{hh}: {h2.collider.name} {h2.distance:F2} n{h2.normal} | "); }
return s.ToString();
