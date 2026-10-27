// 과제 1331 탐색: a3_M1_mound 가 길 위를 덮는 범위 — 길 격자(0.5m)에서 Road 보다 0.05m 넘게 높은 mound 면이 있는 점 수·범위·최대 높이차, 렌더러/콜라이더 종류
var sb = new System.Text.StringBuilder(); var m = GameObject.Find("a3_M1_mound");
if (!m) return "no mound";
sb.Append(m.name + " path " + m.transform.parent?.name + " comps "); foreach (var c in m.GetComponents<Component>()) sb.Append(c.GetType().Name + ","); var mf = m.GetComponent<MeshFilter>(); if (mf) sb.Append(" mesh " + mf.sharedMesh.name + " v" + mf.sharedMesh.vertexCount + " rend " + (m.GetComponent<Renderer>()?.enabled)); sb.Append(" bounds " + m.GetComponent<Collider>().bounds.ToString("F1") + "\n");
var road = GameObject.Find("Road"); var rb = road.GetComponent<Collider>().bounds; int cov = 0, tot = 0; float maxD = 0; var mn = Vector3.one * 999; var mx = -mn;
for (float x = rb.min.x; x <= rb.max.x; x += 0.5f) for (float z = rb.min.z; z <= rb.max.z; z += 0.5f) {
  float ry = float.NaN, my = float.NaN; foreach (var h in Physics.RaycastAll(new Vector3(x, 60, z), Vector3.down, 200)) { if (h.collider.gameObject == road) ry = h.point.y; else if (h.collider.gameObject == m) my = Mathf.Max(float.IsNaN(my) ? -999 : my, h.point.y); }
  if (float.IsNaN(ry)) continue; tot++; if (!float.IsNaN(my) && my > ry + 0.05f) { cov++; maxD = Mathf.Max(maxD, my - ry); var pt = new Vector3(x, ry, z); mn = Vector3.Min(mn, pt); mx = Vector3.Max(mx, pt); } }
sb.Append("road pts " + tot + " covered " + cov + " maxDiff " + maxD.ToString("F1") + " range " + mn.ToString("F1") + "~" + mx.ToString("F1"));
return sb.ToString();
