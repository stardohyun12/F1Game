var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var cam = Camera.main.transform;
var sb = new System.Text.StringBuilder();
var ps = d.PackSpot;
sb.AppendLine("cam=" + cam.position.ToString("F2") + " fwd=" + cam.forward.ToString("F2") + " packTop=" + ps.Top.ToString("F2") + " packActive=" + ps.gameObject.activeInHierarchy);
foreach (var c in ps.GetComponentsInChildren<Collider>(true)) sb.AppendLine(" col " + c.name + " en=" + c.enabled + " b=" + c.bounds.center.ToString("F2") + " s=" + c.bounds.size.ToString("F2") + " layer=" + c.gameObject.layer);
var dirv = (ps.Top - cam.position).normalized;
foreach (var v in new[] { cam.forward, dirv })
{
  var hits = Physics.RaycastAll(cam.position, v, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
  System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
  sb.Append("ray " + v.ToString("F2") + ": ");
  foreach (var h in hits) sb.Append(h.collider.name + "@" + h.distance.ToString("F2") + " ");
  sb.AppendLine();
}
// 앞 마당 단면: x=270, z -38 → -33
for (float z = -38f; z <= -32.9f; z += 0.25f) { RaycastHit h; if (Physics.Raycast(new Vector3(270f, 34f, z), Vector3.down, out h, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) sb.Append("z" + z.ToString("F2") + "=" + h.collider.name + ":" + h.point.y.ToString("F2") + " "); }
return sb.ToString();
