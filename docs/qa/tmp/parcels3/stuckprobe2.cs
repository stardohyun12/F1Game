// 0141: 끼임 자리 3 m 둘레 — 길 표면 vs 지형 높이(지형이 길 위로 솟았나)
string r = "";
foreach (var pt in new[] { new UnityEngine.Vector2(142, -86), new UnityEngine.Vector2(-180, -70) }) {
  float worst = -9; string at = "";
  for (float dx = -4; dx <= 4; dx += 1) for (float dz = -4; dz <= 4; dz += 1) {
    float yr = float.NaN, yt = float.NaN;
    foreach (var h in UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(pt.x + dx, 60, pt.y + dz), UnityEngine.Vector3.down, 120)) {
      if (h.collider.name.StartsWith("ROAD")) yr = float.IsNaN(yr) ? h.point.y : UnityEngine.Mathf.Max(yr, h.point.y);
      if (h.collider.name == "TownV6_Terrain") yt = h.point.y; }
    if (!float.IsNaN(yr) && !float.IsNaN(yt) && yt - yr > worst) { worst = yt - yr; at = (pt.x + dx) + "," + (pt.y + dz); } }
  r += pt + " terrain-road max " + worst.ToString("F2") + " @" + at + "\n"; }
return r;
