// D-203: 능선에 핵심 4채 배치 + 테라스 + 겹치는 OSM 집 삭제 (에디트 모드, 씬 저장은 따로)
// 줄 배치: 집(185,-61, yaw 190) 동쪽으로 우체국·보관소(yaw 180, 정면 남쪽=길) → 정비소(yaw 90, 정면 동쪽=북동 길). 앞선(남쪽 끝)을 집에 맞춘다.
string H = "D:/Github/F1Game/docs/qa/tmp/d203/";
float MARGIN = 1.5f, BLEND = 8f, GAP = 3f;
var sb = new System.Text.StringBuilder();
var root = GameObject.Find("TownV6_Gray").transform;
var terT = root.Find("TownV6_Terrain"); var terMF = terT.GetComponent<MeshFilter>(); var terMC = terT.GetComponent<MeshCollider>();
var parent = root.Find("D198_Buildings");
Transform Get(string n) => parent.Find(n);
var home = Get("house_home"); var po = Get("D198_postoffice"); var dp = Get("D198_depot"); var gr = Get("D198_garage");
float Ground(float x, float z) { terMC.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600); return h.point.y; }
Bounds BB(Transform t) { var b = new Bounds(t.position, Vector3.zero); bool f = true; foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); } return b; }
// 1) 회전 + 수평 위치
home.rotation = Quaternion.Euler(0, 190, 0); home.position = new Vector3(185, home.position.y, -61);
float zFront = BB(home).min.z; float cursor = BB(home).max.x + GAP;
void PlaceRow(Transform t, float yaw) { t.rotation = Quaternion.Euler(0, yaw, 0); t.position = new Vector3(0, t.position.y, 0); var b = BB(t); t.position += new Vector3(cursor - b.min.x, 0, zFront - b.min.z); cursor = BB(t).max.x + GAP; }
PlaceRow(po, 180); PlaceRow(dp, 180); PlaceRow(gr, 90);
// 2) 바닥 높이 = 발자국 안 지형 평균(원본 지형)
var pads = new List<(string n, Transform t, Rect r, float floor)>();
foreach (var t in new[] { home, po, dp, gr }) {
  var b = BB(t); float s = 0; int n = 0; float lo = 999, hi = -999;
  for (float x = b.min.x; x <= b.max.x; x += 1f) for (float z = b.min.z; z <= b.max.z; z += 1f) { float g = Ground(x, z); s += g; n++; lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
  float fl = Mathf.Round(s / n * 10f) / 10f;
  t.position = new Vector3(t.position.x, fl, t.position.z);
  var r = Rect.MinMaxRect(b.min.x - MARGIN, b.min.z - MARGIN, b.max.x + MARGIN, b.max.z + MARGIN);
  pads.Add((t.name, t, r, fl));
  sb.Append(t.name + " pos " + t.position.ToString("F1") + " yaw " + t.eulerAngles.y.ToString("F0") + " foot x" + b.min.x.ToString("F1") + ".." + b.max.x.ToString("F1") + " z" + b.min.z.ToString("F1") + ".." + b.max.z.ToString("F1") + " floor " + fl + " origGround " + lo.ToString("F1") + ".." + hi.ToString("F1") + " cut " + (hi - fl).ToString("F1") + " fill " + (fl - lo).ToString("F1") + " distHome " + Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(185, -61)).ToString("F1") + "\n");
}
for (int i = 0; i < pads.Count; i++) for (int j = i + 1; j < pads.Count; j++) { var a = BB(pads[i].t); var c = BB(pads[j].t); bool ov = a.min.x < c.max.x && c.min.x < a.max.x && a.min.z < c.max.z && c.min.z < a.max.z; if (ov) sb.Append("OVERLAP " + pads[i].n + " x " + pads[j].n + "\n"); }
// 지형 변형 함수(패드 순차 적용)
float RectDist(Rect r, float x, float z) { float dx = Mathf.Max(r.xMin - x, 0, x - r.xMax), dz = Mathf.Max(r.yMin - z, 0, z - r.yMax); return Mathf.Sqrt(dx * dx + dz * dz); }
float NewY(float x, float z, float y) { foreach (var p in pads) { float d = RectDist(p.r, x, z); if (d >= BLEND) continue; float t = d / BLEND; float w = 1 - t * t * (3 - 2 * t); y = Mathf.Lerp(y, p.floor, w); } return y; }
// 3) 지형 사본
var tm = UnityEngine.Object.Instantiate(terMF.sharedMesh); tm.name = "Mesh_TownV6_Terrain_d203";
var tv = tm.vertices; int changed = 0; float maxD = 0;
for (int i = 0; i < tv.Length; i++) { var w = terT.TransformPoint(tv[i]); float ny = NewY(w.x, w.z, w.y); if (Mathf.Abs(ny - w.y) > 0.001f) { changed++; maxD = Mathf.Max(maxD, Mathf.Abs(ny - w.y)); tv[i] = terT.InverseTransformPoint(new Vector3(w.x, ny, w.z)); } }
tm.vertices = tv; tm.RecalculateNormals(); tm.RecalculateBounds();
string MP = "Assets/Scenes/Map_TownV6_Mats/";
AssetDatabase.CreateAsset(tm, MP + "Mesh_TownV6_Terrain_d203.asset");
float Delta(float x, float z) { float y = Ground(x, z); return NewY(x, z, y) - y; }   // 원본 콜라이더 기준(교체 전)
// 4) OSM: 패드(+여유) 와 겹치면 삭제, 블렌드 안이면 집 단위로 min(delta) 만큼 이동
var polys = System.IO.File.ReadAllLines(H + "polys.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return (id: a[0], pts: a.Skip(2).Select(s => { var q = s.Split(','); return new Vector2(float.Parse(q[0]), float.Parse(q[1])); }).ToArray()); }).ToList();
bool In(Vector2[] p, float x, float z) { bool c = false; for (int i = 0, j = p.Length - 1; i < p.Length; j = i++) if (((p[i].y > z) != (p[j].y > z)) && (x < (p[j].x - p[i].x) * (z - p[i].y) / (p[j].y - p[i].y) + p[i].x)) c = !c; return c; }
bool Hits(Vector2[] p, Rect r) { if (p.Any(q => r.Contains(q))) return true; foreach (var c in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), r.center }) if (In(p, c.x, c.y)) return true; return false; }
var del = new HashSet<int>(); var off = new float[polys.Count];
for (int b = 0; b < polys.Count; b++) { if (pads.Any(pd => Hits(polys[b].pts, pd.r))) { del.Add(b); continue; } off[b] = polys[b].pts.Min(q => Delta(q.x, q.y)); }
sb.Append("OSM deleted " + del.Count + ": " + string.Join(" ", del.Select(b => polys[b].id)) + "\n");
sb.Append("OSM shifted: " + string.Join(" ", Enumerable.Range(0, polys.Count).Where(b => !del.Contains(b) && Mathf.Abs(off[b]) > 0.01f).Select(b => polys[b].id + ":" + off[b].ToString("F2"))) + "\n");
int Owner(float x, float z) { for (int b = 0; b < polys.Count; b++) foreach (var q in polys[b].pts) if (Mathf.Abs(q.x - x) < 0.05f && Mathf.Abs(q.y - z) < 0.05f) return b; for (int b = 0; b < polys.Count; b++) if (In(polys[b].pts, x, z)) return b; int best = -1; float bd = 2.5f; for (int b = 0; b < polys.Count; b++) foreach (var q in polys[b].pts) { float d = Vector2.Distance(q, new Vector2(x, z)); if (d < bd) { bd = d; best = b; } } return best; }
foreach (var nm in new[] { "Walls_0", "Roofs_0" }) {
  var t = root.Find("Buildings/" + nm); var mf = t.GetComponent<MeshFilter>(); var mc = t.GetComponent<MeshCollider>();
  var m = UnityEngine.Object.Instantiate(mf.sharedMesh); m.name = "Mesh_" + nm + "_d203"; var v = m.vertices; var own = new int[v.Length];
  for (int i = 0; i < v.Length; i++) { var w = t.TransformPoint(v[i]); own[i] = (w.x - 205) * (w.x - 205) + (w.z + 58) * (w.z + 58) < 100 * 100 ? Owner(w.x, w.z) : -1; if (own[i] >= 0 && !del.Contains(own[i]) && off[own[i]] != 0) v[i] = t.InverseTransformPoint(w + Vector3.up * off[own[i]]); }
  m.vertices = v; int removed = 0;
  for (int s = 0; s < m.subMeshCount; s++) { var tri = m.GetTriangles(s); var keep = new List<int>(); for (int k = 0; k < tri.Length; k += 3) { bool dd = del.Contains(own[tri[k]]) && del.Contains(own[tri[k + 1]]) && del.Contains(own[tri[k + 2]]); if (dd) removed++; else { keep.Add(tri[k]); keep.Add(tri[k + 1]); keep.Add(tri[k + 2]); } } m.SetTriangles(keep, s); }
  m.RecalculateBounds(); AssetDatabase.CreateAsset(m, MP + "Mesh_" + nm + "_d203.asset");
  mf.sharedMesh = m; if (mc) mc.sharedMesh = m; EditorUtility.SetDirty(mf); if (mc) EditorUtility.SetDirty(mc);
  sb.Append(nm + " tris removed " + removed + "\n");
}
terMF.sharedMesh = tm; terMC.sharedMesh = tm; EditorUtility.SetDirty(terMF); EditorUtility.SetDirty(terMC);
Physics.SyncTransforms();
sb.Append("terrain verts changed " + changed + " maxDelta " + maxD.ToString("F2") + "\n");
// 5) 길 검사: 패드 블렌드 안 길 정점이 새 지형 아래 묻혔나
int roadIn = 0, buried = 0; float worst = 0;
foreach (Transform r in root.Find("Roads")) { var mf = r.GetComponent<MeshFilter>(); if (!mf) continue; foreach (var lv in mf.sharedMesh.vertices) { var w = r.TransformPoint(lv); if (!pads.Any(p => RectDist(p.r, w.x, w.z) < BLEND)) continue; roadIn++; float g = Ground(w.x, w.z); if (g > w.y + 0.02f) { buried++; worst = Mathf.Max(worst, g - w.y); } } }
sb.Append("road verts in blend " + roadIn + " buried " + buried + " worst " + worst.ToString("F2") + "\n");
// 6) 바닥 대비 지형(발자국 모서리) 확인
foreach (var p in pads) { var b = BB(p.t); float mx = 0; foreach (var c in new[] { new Vector2(b.min.x, b.min.z), new Vector2(b.max.x, b.min.z), new Vector2(b.min.x, b.max.z), new Vector2(b.max.x, b.max.z) }) mx = Mathf.Max(mx, Mathf.Abs(Ground(c.x, c.y) - p.floor)); sb.Append(p.n + " corner|ground-floor| " + mx.ToString("F2") + "\n"); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
return sb.ToString();
