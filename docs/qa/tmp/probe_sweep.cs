var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var r = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(x => x.roadId == "camo_309369480_161");
var at = new Vector3(626, 34.1f, -111); int bi = 0; float bd = 1e9f;
for (int i = 0; i < r.centerline.Length; i++) { float d = (r.centerline[i] - at).sqrMagnitude; if (d < bd) { bd = d; bi = i; } }
var s = new System.Text.StringBuilder();
foreach (int back in new[] { 4, 2 })
{
  var p = r.centerline[bi - back]; var f = r.centerline[bi - back + 2] - p; f.y = 0; f.Normalize();
  var up = Vector3.Cross(f, Vector3.Cross(Vector3.up, f)).normalized; // 대략 노면 기울기 무시
  rb.position = p + Vector3.up * .05f; rb.rotation = Quaternion.LookRotation(r.centerline[bi - back + 2] - p); car.transform.SetPositionAndRotation(rb.position, rb.rotation); Physics.SyncTransforms();
  var hits = rb.SweepTestAll(rb.rotation * Vector3.forward, 4f, QueryTriggerInteraction.Ignore);
  s.Append($"back{back} p{p} : ");
  foreach (var h in hits.OrderBy(h => h.distance)) s.Append($"{h.collider.name} d{h.distance:F2} n{h.normal} pt{h.point - p} ; ");
  s.AppendLine();
}
var c = car.GetComponentInChildren<BoxCollider>(); s.Append($"box world bottom offset {(c.transform.TransformPoint(c.center - Vector3.up * c.size.y / 2) - car.transform.position)} contactOffset {c.contactOffset} defaultContactOffset {Physics.defaultContactOffset}");
return s.ToString();
