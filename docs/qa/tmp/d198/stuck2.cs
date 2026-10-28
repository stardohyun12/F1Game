// 정방향 끼임 (93,16,-15): 길 61·16 높이 단차 조사 (1 m 격자)
var sb = new System.Text.StringBuilder();
for (int dz = -4; dz <= 4; dz += 1) { for (int dx = -4; dx <= 4; dx += 1) {
  float x = 93 + dx, z = -15 + dz;
  var hs = Physics.RaycastAll(new Vector3(x, 200, z), Vector3.down, 400).OrderBy(h => h.distance).ToList();
  string s = hs.Count == 0 ? "-" : hs[0].point.y.ToString("F2") + (hs[0].collider.name.StartsWith("ROAD_") ? hs[0].collider.name.Substring(hs[0].collider.name.LastIndexOf('_')) : ":" + hs[0].collider.name.Substring(0, 3));
  sb.Append(s.PadRight(12)); }
  sb.Append("  z=" + (-15 + dz) + "\n"); }
return sb.ToString();
