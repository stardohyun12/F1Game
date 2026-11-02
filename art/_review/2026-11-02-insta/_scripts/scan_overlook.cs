// 시선이 트인 비탈 자리 찾기: 지형(TownV6_Terrain)·도로 위 1.7 m 에서 남쪽(-z)±35° 로 150 m 가 안 막히고, 바다 쪽(앞 150 m 의 아래 지면 y<2)이 보이는 점. 높이 순.
var res = new System.Collections.Generic.List<(float y, Vector3 p, float yaw, float clear)>();
for (float x = 60; x <= 300; x += 12) for (float z = -230; z <= 110; z += 12) {
  RaycastHit h; if (!Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out h, 400f)) continue;
  string cn = h.collider.name; if (!(cn.Contains("Terrain") || cn.Contains("Road") || cn.Contains("Walk") || cn.Contains("road"))) continue;
  if (h.point.y < 14f) continue;
  var eye = h.point + Vector3.up * 1.7f;
  float best = 0, byaw = 0;
  for (float yaw = 150; yaw <= 210; yaw += 10) {     // 0 = +z, 180 = -z(남쪽)
    var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward; dir = (dir + Vector3.down * 0.06f).normalized;
    RaycastHit hh; float dist = Physics.Raycast(eye, dir, out hh, 300f) ? hh.distance : 300f;
    if (dist > best) { best = dist; byaw = yaw; }
  }
  if (best >= 250f) res.Add((eye.y, eye, byaw, best));
}
res.Sort((a, b) => b.y.CompareTo(a.y));
var sb = new System.Text.StringBuilder("candidates=" + res.Count + "\n");
for (int i = 0; i < Mathf.Min(14, res.Count); i++) sb.Append(res[i].p.ToString("F1") + " yaw=" + res[i].yaw + " clear=" + res[i].clear.ToString("F0") + "\n");
return sb.ToString();
