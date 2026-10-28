var sb = new System.Text.StringBuilder();
foreach (var rsf in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) {
  if (!rsf.roadId.StartsWith("ridge_up_")) continue;
  var c = rsf.centerline; int top = 0, n = 0, blocked = 0, buried = 0, over12 = 0; float gMax = 0, L = 0, prevY = 0; Vector3 prev = Vector3.zero; string at = "";
  for (int i = 0; i < c.Length; i += 2) {
    RaycastHit h; n++;
    bool hit = Physics.Raycast(c[i] + Vector3.up * 8f, Vector3.down, out h, 20f);
    if (hit && h.collider.name.StartsWith("ROAD_")) top++; else { buried++; sb.Append("  notRoad i=" + i + "/" + c.Length + " " + (hit ? h.collider.name + " y" + h.point.y.ToString("F2") + " cl" + c[i].y.ToString("F2") : "nohit") + "\n"); }
    float y = h.point.y;
    if (i > 0) { float d = new Vector2(c[i].x - c[i-1].x, c[i].z - c[i-1].z).magnitude + new Vector2(c[i-1].x - prev.x, c[i-1].z - prev.z).magnitude; L += d; float g = Mathf.Abs(c[i].y - prev.y) / d; if (g > .12f) sb.Append("  steep " + c[i].x.ToString("F0") + "," + c[i].z.ToString("F0") + " " + (g*100).ToString("F0") + "% hitY-cl " + (y - c[i].y).ToString("F2") + "\n"); if (g > .12f) over12++; if (g > gMax) { gMax = g; at = c[i].x.ToString("F0") + "," + c[i].z.ToString("F0"); } }
    prev = c[i]; prevY = y;
    var f = (c[Mathf.Min(i + 1, c.Length - 1)] - c[Mathf.Max(i - 1, 0)]); f.y = 0;
    foreach (var col in Physics.OverlapBox(c[i] + Vector3.up * 1.0f, new Vector3(1.0f, .7f, 2f), Quaternion.LookRotation(f.normalized))) {
      var nm = col.name; if (nm.StartsWith("ROAD_") || nm.StartsWith("TownV6_Terrain") || col.isTrigger) continue; blocked++; }
  }
  sb.Append(rsf.roadId + " start " + c[0].ToString("F1") + " end " + c[c.Length-1].ToString("F1") + " width " + rsf.width + " len " + L.ToString("F0") + " samples " + n + " topRoad " + top + " buried " + buried + " slopeMax " + (gMax*100).ToString("F1") + "%@" + at + " over12 " + over12 + " blocked " + blocked + "\n");
}
var t = GameObject.Find("TownV6_Gray/TownV6_Terrain"); sb.Append("terrainMesh " + UnityEditor.AssetDatabase.GetAssetPath(t.GetComponent<MeshFilter>().sharedMesh));
return sb.ToString();
