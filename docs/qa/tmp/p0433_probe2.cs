// 과제 0433: D198 건물 4채 발밑 + 능선 길 끝이 기존 길과 이어지는지
var sb = new System.Text.StringBuilder();
var tmc = GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
var root = GameObject.Find("TownV6_Gray/D198_Buildings").transform;
foreach (Transform g in root) {
  if (!g.gameObject.activeInHierarchy) continue;
  var rs = g.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
  var hs = new System.Collections.Generic.List<float>();
  for (float x = b.min.x + .5f; x <= b.max.x; x += 1f) for (float z = b.min.z + .5f; z <= b.max.z; z += 1f) { RaycastHit h; if (tmc.Raycast(new Ray(new Vector3(x, 200, z), Vector3.down), out h, 400)) hs.Add(h.point.y); }
  hs.Sort(); int over = 0, under = 0; foreach (var y in hs) { if (y - g.position.y > .1f) over++; if (g.position.y - y > .1f) under++; }
  sb.Append(g.name + " pos=" + g.position.ToString("F2") + " yaw=" + g.eulerAngles.y.ToString("F0") + " bmin=" + b.min.ToString("F2") + " bmax=" + b.max.ToString("F2") + " terr min/med/max " + hs[0].ToString("F2") + "/" + hs[hs.Count / 2].ToString("F2") + "/" + hs[hs.Count - 1].ToString("F2") + " 묻힘칸 " + over + " 뜸칸 " + under + "/" + hs.Count + "\n"); }
var all = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None);
foreach (var r in all) { if (!r.roadId.StartsWith("ridge")) continue;
  foreach (var e in new[] { r.centerline[0], r.centerline[r.centerline.Length - 1] }) {
    float best = 1e9f; string who = ""; float oy = 0;
    foreach (var o in all) { if (o == r || o.centerline == null) continue; var c = o.centerline;
      for (int i = 1; i < c.Length; i++) { var a = c[i - 1]; var ab = c[i] - a; ab.y = 0; var ap = e - a; ap.y = 0; float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); var q = Vector3.Lerp(a, c[i], t); var d = e - q; d.y = 0; float dd = d.magnitude - o.width * .5f; if (dd < best) { best = dd; who = o.roadId + "(" + o.kind + ",w" + o.width + ")"; oy = q.y; } } }
    sb.Append(r.roadId + " end " + e.ToString("F1") + " → " + who + " 가장자리까지 " + best.ToString("F2") + " m, 높이차 " + (e.y - oy).ToString("F2") + "\n"); } }
return sb.ToString();
