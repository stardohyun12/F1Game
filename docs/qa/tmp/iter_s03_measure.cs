// artlive 1532: 보관소(기단·간판판·소품 포함) ↔ 이웃 벽 블록 겹침·간격 측정 + 같은 자리 안 이동·회전 후보 탐색(읽기만, 씬 안 바꿈)
// 이웃 = TownV6_Gray/Buildings/Walls_0(마을 벽 전체가 메시 하나) 안의 연결 덩어리(섬). 섬마다 XZ 볼록 껍질 × 높이 범위로 본다.
// 결과 각 후보: 겹친 부피(m³, 0.25m 격자) · 최소 간격(m, 음수 = 파묻힘). 후보 Δ = 현재 자세 기준 (dx, dz 로컬 m, dyaw 본체 가운데 축)
var log = new System.Text.StringBuilder();
var root = GameObject.Find("ITER_Depot").transform;
// 보관소 로컬 AABB
var lb = new Bounds(); bool f0 = false;
foreach (var mf in root.GetComponentsInChildren<MeshFilter>()) foreach (var v in mf.sharedMesh.vertices) { var p = root.InverseTransformPoint(mf.transform.TransformPoint(v)); if (!f0) { lb = new Bounds(p, Vector3.zero); f0 = true; } else lb.Encapsulate(p); }
log.Append("depot local box " + lb.min.ToString("F2") + ".." + lb.max.ToString("F2") + " (world y " + (root.position.y + lb.min.y).ToString("F2") + ".." + (root.position.y + lb.max.y).ToString("F2") + ")\n");
var wt = GameObject.Find("TownV6_Gray").transform.Find("Buildings/Walls_0"); var wm = wt.GetComponent<MeshFilter>().sharedMesh; var wv = wm.vertices; var wtri = wm.triangles;
var cen = root.TransformPoint(new Vector3(0, 0, -3.5f));
// 섬: 1cm 용접 + 삼각형 연결(보관소 25m 안 삼각형만)
var key = new System.Collections.Generic.Dictionary<Vector3Int, int>(); var par = new System.Collections.Generic.List<int>(); var wpos = new System.Collections.Generic.List<Vector3>();
System.Func<int, int> Find = null; Find = i => { while (par[i] != i) { par[i] = par[par[i]]; i = par[i]; } return i; };
System.Func<Vector3, int> Id = w => { var k = new Vector3Int(Mathf.RoundToInt(w.x * 100), Mathf.RoundToInt(w.y * 100), Mathf.RoundToInt(w.z * 100)); if (!key.TryGetValue(k, out int id)) { id = par.Count; key[k] = id; par.Add(id); wpos.Add(w); } return id; };
for (int t = 0; t < wtri.Length; t += 3) { var a = wt.TransformPoint(wv[wtri[t]]); if (new Vector2(a.x - cen.x, a.z - cen.z).magnitude > 25f) continue;
  int ia = Id(a), ib = Id(wt.TransformPoint(wv[wtri[t + 1]])), ic = Id(wt.TransformPoint(wv[wtri[t + 2]])); par[Find(ib)] = Find(ia); par[Find(ic)] = Find(ia); }
var isl = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Vector3>>(); for (int i = 0; i < wpos.Count; i++) { int r = Find(i); if (!isl.ContainsKey(r)) isl[r] = new System.Collections.Generic.List<Vector3>(); isl[r].Add(wpos[i]); }
// 볼록 껍질(XZ, monotone chain)
System.Func<System.Collections.Generic.List<Vector3>, Vector2[]> Hull = pts => { var p = pts.Select(q => new Vector2(q.x, q.z)).Distinct().OrderBy(q => q.x).ThenBy(q => q.y).ToList(); if (p.Count < 3) return p.ToArray();
  System.Func<Vector2, Vector2, Vector2, float> Cr = (o, a, b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x); var h = new Vector2[2 * p.Count]; int k = 0;
  for (int i = 0; i < p.Count; i++) { while (k >= 2 && Cr(h[k - 2], h[k - 1], p[i]) <= 0) k--; h[k++] = p[i]; } for (int i = p.Count - 2, tt = k + 1; i >= 0; i--) { while (k >= tt && Cr(h[k - 2], h[k - 1], p[i]) <= 0) k--; h[k++] = p[i]; } return h.Take(k - 1).ToArray(); };
System.Func<Vector2[], Vector2, bool> In = (h, q) => { for (int i = 0; i < h.Length; i++) { var a = h[i]; var b = h[(i + 1) % h.Length]; if ((b.x - a.x) * (q.y - a.y) - (b.y - a.y) * (q.x - a.x) < 0) return false; } return true; };
var blocks = new System.Collections.Generic.List<(string nm, Vector2[] h, float y0, float y1, Vector3 size)>();
foreach (var kv in isl) { var pts = kv.Value; if (pts.Count < 4) continue; float y0 = pts.Min(q => q.y), y1 = pts.Max(q => q.y); var h = Hull(pts); if (h.Length < 3) continue;
  var bmin = new Vector3(pts.Min(q => q.x), y0, pts.Min(q => q.z)); var bmax = new Vector3(pts.Max(q => q.x), y1, pts.Max(q => q.z)); var c2 = (bmin + bmax) / 2f;
  blocks.Add(("blk@" + c2.x.ToString("F1") + "," + c2.z.ToString("F1"), h, y0, y1, bmax - bmin)); }
// 후보 자세에서 측정
System.Func<float, float, float, (float vol, float gap, string who)> Meas = (dx, dz, dyaw) => {
  var q = root.rotation * Quaternion.Euler(0, dyaw, 0); var pc = root.TransformPoint(new Vector3(dx, 0, -3.5f + dz)); var o = pc - q * new Vector3(0, 0, -3.5f);   // 본체 가운데 축
  float vol = 0, gap = 999; string who = "";
  foreach (var b in blocks) { float yo = Mathf.Min(b.y1, o.y + lb.max.y) - Mathf.Max(b.y0, o.y + lb.min.y); if (yo <= 0) continue;
    int nIn = 0; for (float x = lb.min.x + 0.125f; x < lb.max.x; x += 0.25f) for (float z = lb.min.z + 0.125f; z < lb.max.z; z += 0.25f) { var w = o + q * new Vector3(x, 0, z); if (In(b.h, new Vector2(w.x, w.z))) nIn++; }
    float g = 999; for (int i = 0; i < b.h.Length; i++) { var a = b.h[i]; var c = b.h[(i + 1) % b.h.Length]; for (float s = 0; s <= 1.0001f; s += 0.02f) { var w = Vector2.Lerp(a, c, s); var lp = Quaternion.Inverse(q) * (new Vector3(w.x, o.y, w.y) - o);
      float ddx = Mathf.Max(lb.min.x - lp.x, 0, lp.x - lb.max.x), ddz = Mathf.Max(lb.min.z - lp.z, 0, lp.z - lb.max.z); float d = Mathf.Sqrt(ddx * ddx + ddz * ddz);
      if (d == 0) d = -Mathf.Min(Mathf.Min(lp.x - lb.min.x, lb.max.x - lp.x), Mathf.Min(lp.z - lb.min.z, lb.max.z - lp.z)); g = Mathf.Min(g, d); } }
    if (nIn > 0) g = Mathf.Min(g, -0.01f); float v = nIn * 0.0625f * yo; vol += v; if (g < gap) { gap = g; who = b.nm; } }
  return (vol, gap, who); };
// 전: 지금 자세에서 섬별 표
log.Append("NOW per block (gap<6m):\n");
{ var q = root.rotation; var o = root.position;
  foreach (var b in blocks) { var save = blocks; } }
var now = Meas(0, 0, 0); log.Append("NOW total overlap " + now.vol.ToString("F1") + " m3, min gap " + now.gap.ToString("F2") + " m (" + now.who + ")\n");
foreach (var b in blocks) { var one = blocks; } // (섬별 상세는 아래 후보 탐색과 같은 함수로)
foreach (var b in blocks.ToList()) { var keep = blocks.ToList(); blocks.Clear(); blocks.Add(b); var m = Meas(0, 0, 0); blocks.Clear(); blocks.AddRange(keep); if (m.gap < 6f) log.Append("  " + b.nm + " size " + b.size.ToString("F1") + " y " + b.y0.ToString("F1") + ".." + b.y1.ToString("F1") + " overlap " + m.vol.ToString("F1") + " m3 gap " + m.gap.ToString("F2") + "\n"); }
// 길 침범: 정면 0.3m 앞 3점이 ROAD 면 탈락. 길에서 보이는 정도: Cam_IT_road 에서 정면 점 55개
var cam = GameObject.Find("ITER_Cams").transform.Find("Cam_IT_road").position;
System.Func<Vector3, Collider> GC = p => { Collider best = null; float y = -999; foreach (var h in Physics.RaycastAll(new Vector3(p.x, 60f, p.z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore)) if (h.point.y > y && !h.transform.IsChildOf(root)) { y = h.point.y; best = h.collider; } return best; };
var res = new System.Collections.Generic.List<(float dx, float dz, float dy, float vol, float gap, float vis)>();
foreach (var dy in new[] { -20f, -15f, -10f, -5f, 0f, 5f, 10f }) foreach (var dx in new[] { -3f, -2.5f, -2f, -1.5f, -1f, -0.5f, 0f, 0.5f, 1f, 1.5f, 2f, 2.5f, 3f }) foreach (var dz in new[] { -2f, -1.5f, -1f, -0.5f, 0f, 0.5f, 1f }) {
  var m = Meas(dx, dz, dy); if (m.gap < 1.5f) continue;
  var q = root.rotation * Quaternion.Euler(0, dy, 0); var o = root.TransformPoint(new Vector3(dx, 0, -3.5f + dz)) - q * new Vector3(0, 0, -3.5f);
  bool road = false; foreach (var lx in new[] { -5f, 0f, 5f }) { var c = GC(o + q * new Vector3(lx, 0, 0.3f)); if (c && c.name.StartsWith("ROAD")) road = true; } if (road) continue;
  var fwd = q * Vector3.forward; int vis = 0; for (float x = -5f; x <= 5.01f; x += 1f) for (float y = 0.6f; y <= 3.41f; y += 0.7f) { var wp = o + q * new Vector3(x, y, 0.05f); var d = cam - wp; if (Vector3.Dot(d, fwd) <= 0) continue; if (!Physics.Raycast(wp, d.normalized, d.magnitude - 0.2f, ~0, QueryTriggerInteraction.Ignore)) vis++; }
  var tc = cam - o; tc.y = 0; res.Add((dx, dz, dy, m.vol, m.gap, vis * (0.3f + Mathf.Max(0f, Vector3.Dot(fwd, tc.normalized))))); }
log.Append("candidates ok (gap>=1.5, no road): " + res.Count + "\n");
foreach (var r in res.OrderBy(r => Mathf.Abs(r.dx) + Mathf.Abs(r.dz) + Mathf.Abs(r.dy) / 10f).Take(12)) log.Append("  dx " + r.dx + " dz " + r.dz + " dyaw " + r.dy + " gap " + r.gap.ToString("F2") + " vis " + r.vis.ToString("F1") + "\n");
return log.ToString();
