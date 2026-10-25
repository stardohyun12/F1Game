var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>(); var sb = new System.Text.StringBuilder();
var p = car.transform.position;
foreach (var a in new[] { -60, -30, 0, 30, 60 })
  foreach (var hgt in new[] { 0.2f, 0.6f })
  {
    var d = Quaternion.Euler(0, a, 0) * car.transform.forward; d.y = 0; d.Normalize();
    if (Physics.Raycast(p + Vector3.up * hgt, d, out var h, 6f) && h.collider.attachedRigidbody != rb) sb.Append($"ang{a} h{hgt}: {h.collider.name} d={h.distance:F2} n={h.normal:F2} pt={h.point:F2}\n");
  }
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "main");
for (int i = 140; i <= 160; i += 2) sb.Append(i + ":" + rs.centerline[i].ToString("F1") + " ");
sb.Append("\nkinematic=" + rb.isKinematic + " constraints=" + rb.constraints + " sleeping=" + rb.IsSleeping());
return sb.ToString();
