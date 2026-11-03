// 보관소 안·앞 레이 측정(과제 20261102-1515): 바닥 높이, 셔터 구멍이 뚫렸나, 서쪽 벽, 앞 땅·길
var sb = new System.Text.StringBuilder();
System.Func<Vector3, string> Down = (p) => { RaycastHit h; return Physics.Raycast(p, Vector3.down, out h, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.collider.name + " y=" + h.point.y.ToString("F2") : "none"; };
// 바닥 격자
for (float z = -36f; z <= -25f; z += 1.5f) { sb.Append("z=" + z.ToString("F1") + ": "); for (float x = 266f; x <= 278f; x += 1.5f) sb.Append(Down(new Vector3(x, 33.2f, z)) + " | "); sb.AppendLine(); }
// 셔터 구멍: 앞(z -36)에서 북쪽으로 눈높이 레이
foreach (float x in new[] { 268.5f, 269.5f, 270.5f, 271.5f, 275.2f }) { RaycastHit h; var o = new Vector3(x, 32.5f, -37f); sb.AppendLine("north ray x=" + x + " -> " + (Physics.Raycast(o, Vector3.forward, out h, 15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.collider.name + " z=" + h.point.z.ToString("F2") : "none")); }
// 안에서 서쪽/동쪽/북쪽 벽
foreach (var d in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back }) { RaycastHit h; var o = new Vector3(271.5f, 32.6f, -29.5f); sb.AppendLine("from in " + d + " -> " + (Physics.Raycast(o, d, out h, 15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.collider.name + " " + h.point.ToString("F2") : "none")); }
// 앞쪽 길
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { if (r.centerline == null) continue; float best = 1e9f; Vector3 bp = Vector3.zero; foreach (var p in r.centerline) { float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(271.9f, -34f)); if (d < best) { best = d; bp = p; } } if (best < 30f) sb.AppendLine("road " + r.roadId + " kind=" + r.kind + " nearest " + bp.ToString("F1") + " d=" + best.ToString("F1")); }
return sb.ToString();
