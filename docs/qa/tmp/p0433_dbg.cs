// 과제 0433: 보관소 발밑 뜬 칸 — 무엇에 맞는지
var t = GameObject.Find("TownV6_Gray/D198_Buildings/D198_depot").transform; var sb = new System.Text.StringBuilder();
Vector3 mn = Vector3.one * 1e9f, mx = -mn;
foreach (var f in t.GetComponentsInChildren<MeshFilter>()) { var b = f.sharedMesh.bounds; for (int k = 0; k < 8; k++) { var p = t.InverseTransformPoint(f.transform.TransformPoint(new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 4) == 0 ? b.min.y : b.max.y, (k & 2) == 0 ? b.min.z : b.max.z))); mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); } }
sb.Append("local " + mn.ToString("F2") + " " + mx.ToString("F2") + "\n");
var tmc = GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
for (float x = mn.x + .5f; x <= mx.x - .5f; x += 1f) for (float z = mn.z + .5f; z <= mx.z - .5f; z += 1f) {
  var w = t.TransformPoint(new Vector3(x, 0, z)); var hits = Physics.RaycastAll(new Vector3(w.x, t.position.y + 50f, w.z), Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);
  float best = 1e9f; string nm = ""; float gy = 0; foreach (var h in hits) if (!h.transform.IsChildOf(t) && h.distance < best) { best = h.distance; gy = h.point.y; nm = h.collider.name; }
  RaycastHit th; float ty = tmc.Raycast(new Ray(new Vector3(w.x, 200, w.z), Vector3.down), out th, 400) ? th.point.y : -99;
  if (Mathf.Abs(gy - t.position.y) > .1f) sb.Append("(" + w.x.ToString("F1") + "," + w.z.ToString("F1") + ") hit " + nm + " " + gy.ToString("F2") + " terrCol " + ty.ToString("F2") + "\n"); }
return sb.ToString();
