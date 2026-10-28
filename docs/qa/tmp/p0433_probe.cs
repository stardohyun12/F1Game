// 과제 0433: 길 위로 지형이 튀어나온 곳(전 맵) + 건물 발밑 지형 범위
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); var sb = new System.Text.StringBuilder(); sb.Append(sc.path + "\n");
var terr = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var tmc = terr.GetComponent<MeshCollider>();
sb.Append("terrMesh " + UnityEditor.AssetDatabase.GetAssetPath(terr.GetComponent<MeshFilter>().sharedMesh) + "\n");
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) {
  var c = r.centerline; if (c == null || c.Length < 2) continue; int bad = 0, n = 0; float worst = 0; Vector3 wp = Vector3.zero;
  for (int i = 1; i < c.Length; i++) { var d = c[i] - c[i - 1]; var side = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z).normalized);
    for (float o = -r.width * .5f + .2f; o <= r.width * .5f - .2f; o += .5f) { var p = c[i] + side * o; n++;
      RaycastHit h; if (tmc.Raycast(new Ray(p + Vector3.up * 5, Vector3.down), out h, 10f)) { float dy = h.point.y - p.y; if (dy > .02f) { bad++; if (dy > worst) { worst = dy; wp = p; } } } } }
  var mr = r.GetComponent<MeshRenderer>(); string mat = mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "-";
  if (bad > 0 || r.roadId.StartsWith("ridge")) sb.Append(r.roadId + " " + r.kind + " mat=" + mat + " poke " + bad + "/" + n + " worst " + worst.ToString("F2") + " at (" + wp.x.ToString("F0") + "," + wp.z.ToString("F0") + ")\n"); }
foreach (var g in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
  if (!(g.name.StartsWith("D198_") || g.name.StartsWith("D203_") || g.name.ToLower().Contains("garage"))) continue;
  if (g.parent != null && (g.parent.name.StartsWith("D198_") || g.parent.name.StartsWith("D203_") || g.parent.name.ToLower().Contains("garage"))) continue;
  var rs = g.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
  float mn = 1e9f, mx = -1e9f; for (float x = b.min.x; x <= b.max.x; x += 1f) for (float z = b.min.z; z <= b.max.z; z += 1f) { RaycastHit h; if (tmc.Raycast(new Ray(new Vector3(x, 200, z), Vector3.down), out h, 400)) { mn = Mathf.Min(mn, h.point.y); mx = Mathf.Max(mx, h.point.y); } }
  sb.Append(g.name + " parent=" + (g.parent ? g.parent.name : "-") + " pos=" + g.position.ToString("F2") + " rot=" + g.eulerAngles.y.ToString("F0") + " bminY=" + b.min.y.ToString("F2") + " size=" + b.size.ToString("F1") + " terr " + mn.ToString("F2") + ".." + mx.ToString("F2") + "\n"); }
return sb.ToString();
