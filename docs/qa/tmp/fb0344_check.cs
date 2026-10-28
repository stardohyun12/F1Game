var sb = new System.Text.StringBuilder();
foreach (var rsf in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) {
  if (!rsf.roadId.StartsWith("ridge_up_")) continue;
  var c = rsf.centerline; int top = 0, n = 0, blocked = 0; float gMax = 0, gSum = 0, prevY = 0; Vector3 prev = Vector3.zero; string at = ""; var blk = new System.Collections.Generic.List<string>();
  for (int i = 0; i < c.Length; i += 2) {
    RaycastHit h; n++;
    if (Physics.Raycast(c[i] + Vector3.up * 8f, Vector3.down, out h, 20f) && h.collider.name.StartsWith("ROAD_")) top++;
    float y = h.point.y;
    if (i > 0) { float d = new Vector2(c[i].x - prev.x, c[i].z - prev.z).magnitude; float g = Mathf.Abs(y - prevY) / d; if (g > gMax) { gMax = g; at = i + "/" + c.Length + "@" + c[i].x.ToString("F0") + "," + c[i].z.ToString("F0") + " " + h.collider.name + " dy=" + (y - prevY).ToString("F2"); } }
    prev = c[i]; prevY = y;
    var f = (c[Mathf.Min(i + 1, c.Length - 1)] - c[Mathf.Max(i - 1, 0)]); f.y = 0;
    foreach (var col in Physics.OverlapBox(c[i] + Vector3.up * 1.0f, new Vector3(1.0f, .7f, 2f), Quaternion.LookRotation(f.normalized))) {
      var nm = col.name; if (nm.StartsWith("ROAD_") || nm.StartsWith("TownV6_Terrain") || col.isTrigger) continue;
      blocked++; if (blk.Count < 4) blk.Add(nm + "@" + c[i].x.ToString("F0") + "," + c[i].z.ToString("F0"));
    }
  }
  sb.Append(rsf.roadId + " 위에서 첫 충돌=길 " + top + "/" + n + " 콜라이더경사max " + (gMax * 100).ToString("F1") + "% [" + at + "] 차상자 막힘 " + blocked + " " + string.Join(";", blk) + "\n");
}
return sb.ToString();
