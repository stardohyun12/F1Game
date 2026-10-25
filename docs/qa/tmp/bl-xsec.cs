// 본선 (92,-58) 근처 단면: 옆으로 -7..7m 0.25 간격, 레이 첫 히트 + 그 점을 덮는 콜라이더 없는 렌더러 이름·재질
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "main" && r.centerline.Length > 0);
var P = rs.centerline; var q0 = new Vector3(92, 0, -58); int bi = 0; float bd = 1e9f;
for (int i = 0; i < P.Length; i++) { var dd = P[i] - q0; dd.y = 0; if (dd.sqrMagnitude < bd) { bd = dd.sqrMagnitude; bi = i; } }
var c = P[bi]; var f = P[(bi + 2) % P.Length] - P[(bi - 2 + P.Length) % P.Length]; f.y = 0; f.Normalize(); var lat = new Vector3(f.z, 0, -f.x);
var rends = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => (r.bounds.center - c).sqrMagnitude < 900f).ToList();
var o = new System.Text.StringBuilder("c=" + c.ToString("F1") + " ");
string last = "";
for (float t = -7f; t <= 7f; t += 0.25f)
{
    var p = c + lat * t;
    RaycastHit h; string hit = Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out h, 60f) ? h.collider.name + "@" + h.point.y.ToString("F2") : "none";
    var cov = rends.Where(r => r.GetComponent<Collider>() == null && r.bounds.min.x <= p.x && r.bounds.max.x >= p.x && r.bounds.min.z <= p.z && r.bounds.max.z >= p.z && r.bounds.size.y < 0.5f).Select(r => r.name + ":" + (r.sharedMaterial ? r.sharedMaterial.name : "")).Take(3);
    var s = hit + " [" + string.Join(",", cov) + "]";
    if (s != last) { o.Append(t.ToString("F2") + " " + s + " | "); last = s; }
}
return o.ToString();
