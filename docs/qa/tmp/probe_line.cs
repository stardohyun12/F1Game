// 한 길의 중심선 근처 점들에서 아래로 레이 — 맞은 콜라이더와 높이, 그 점의 중심선 y
var id = "posi_190982121_315"; var c0 = new Vector2(-196.5f, 432f); var s = new System.Text.StringBuilder();
RoadSurface rs = null; foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) if (r.roadId == id) rs = r;
for (int i = 0; i < rs.centerline.Length; i++)
{
  var p = rs.centerline[i]; if ((new Vector2(p.x, p.z) - c0).magnitude > 6f) continue;
  var hits = Physics.RaycastAll(new Vector3(p.x, p.y + 5, p.z), Vector3.down, 12f);
  System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
  s.Append($"[{i}] ({p.x:F2},{p.z:F2}) cl.y={p.y:F3} :");
  foreach (var h in hits) s.Append($" {h.collider.name}={h.point.y:F3}");
  s.Append("\n");
}
return s.ToString();
