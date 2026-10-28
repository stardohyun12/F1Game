// D-203 보정: 테라스가 길을 덮지/띄우지 않게 — 길 정점 4m 안 지형은 길 높이 기준으로 제한. 원본 대비 비교 출력
float MARGIN = 1.5f, BLEND = 8f, PROT = 4f;
var sb = new System.Text.StringBuilder();
var root = GameObject.Find("TownV6_Gray").transform;
var terT = root.Find("TownV6_Terrain"); var terMF = terT.GetComponent<MeshFilter>(); var terMC = terT.GetComponent<MeshCollider>();
var orig = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_loopfix.asset");
var tm = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_d203.asset");
var parent = root.Find("D198_Buildings");
Bounds BB(Transform t) { var b = new Bounds(); bool f = true; foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); } return b; }
var pads = new List<(Rect r, float floor)>();
foreach (var n in new[] { "house_home", "D198_postoffice", "D198_depot", "D198_garage" }) { var t = parent.Find(n); var b = BB(t); pads.Add((Rect.MinMaxRect(b.min.x - MARGIN, b.min.z - MARGIN, b.max.x + MARGIN, b.max.z + MARGIN), t.position.y)); }
float RectDist(Rect r, float x, float z) { float dx = Mathf.Max(r.xMin - x, 0, x - r.xMax), dz = Mathf.Max(r.yMin - z, 0, z - r.yMax); return Mathf.Sqrt(dx * dx + dz * dz); }
// 발자국 안이면 그 패드 높이 고정, 밖이면 먼 패드부터 섞어 가장 가까운 패드가 마지막에 이김
float NewY(float x, float z, float y) { foreach (var p in pads) if (RectDist(p.r, x, z) <= 0) return p.floor; foreach (var p in pads.OrderByDescending(q => RectDist(q.r, x, z))) { float d = RectDist(p.r, x, z); if (d >= BLEND) continue; float t = d / BLEND; float w = 1 - t * t * (3 - 2 * t); y = Mathf.Lerp(y, p.floor, w); } return y; }
bool Near(float x, float z, float pad) => pads.Any(p => RectDist(p.r, x, z) < BLEND + pad);
// 길 정점(블렌드+보호 범위 안)
var rv = new List<Vector3>();
foreach (Transform r in root.Find("Roads")) { var mf = r.GetComponent<MeshFilter>(); if (!mf) continue; foreach (var lv in mf.sharedMesh.vertices) { var w = r.TransformPoint(lv); if (Near(w.x, w.z, PROT) && !pads.Any(p => p.r.Contains(new Vector2(w.x, w.z)))) rv.Add(w); } }   // 건물 발자국 안 길(정비소 밑 샛길)은 건물이 차지 — 보호 안 함
// 원본 지형 기준 길-지형 관계(묻힘)
terMC.sharedMesh = orig; Physics.SyncTransforms();
float G(float x, float z) { terMC.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out var h, 600); return h.point.y; }
var road0 = rv.Select(w => G(w.x, w.z) - w.y).ToArray();
int b0 = road0.Count(d => d > 0.02f);
// 새 지형: 길 정점 PROT 안에선 원본과 길 높이 관계 유지 — y_new ≤ max(원본, 길y-0.1) 그리고 ≥ min(원본, 길y-0.6)
var ov = orig.vertices; var nv = new Vector3[ov.Length]; int changed = 0, clamped = 0; float maxD = 0;
for (int i = 0; i < ov.Length; i++) {
  var w = terT.TransformPoint(ov[i]); float ny = NewY(w.x, w.z, w.y);
  if (Mathf.Abs(ny - w.y) > 0.001f) {
    float cap = float.MaxValue, flo = float.MinValue; bool near = false;
    foreach (var r in rv) { float dx = r.x - w.x, dz = r.z - w.z; if (dx * dx + dz * dz > PROT * PROT) continue; near = true; cap = Mathf.Min(cap, Mathf.Max(w.y, r.y - 0.1f)); flo = Mathf.Max(flo, Mathf.Min(w.y, r.y - 0.6f)); }
    if (near && !pads.Any(p => RectDist(p.r, w.x, w.z) <= 0)) { float c = Mathf.Clamp(ny, flo, Mathf.Max(flo, cap)); if (Mathf.Abs(c - ny) > 0.01f) clamped++; ny = c; }
    if (Mathf.Abs(ny - w.y) > 0.001f) { changed++; maxD = Mathf.Max(maxD, Mathf.Abs(ny - w.y)); }
  }
  nv[i] = terT.InverseTransformPoint(new Vector3(w.x, ny, w.z));
}
tm.vertices = nv; tm.RecalculateNormals(); tm.RecalculateBounds(); EditorUtility.SetDirty(tm);
terMC.sharedMesh = null; terMC.sharedMesh = tm; terMF.sharedMesh = tm; Physics.SyncTransforms();
var road1 = rv.Select(w => G(w.x, w.z) - w.y).ToArray();
int b1 = road1.Count(d => d > 0.02f); int worse = 0; float worst = 0;
for (int i = 0; i < rv.Count; i++) { float d = road1[i] - road0[i]; if (Mathf.Abs(d) > 0.05f) { worse++; worst = Mathf.Max(worst, Mathf.Abs(d)); } }
sb.Append("road verts " + rv.Count + " buriedBefore " + b0 + " buriedAfter " + b1 + " relChanged>5cm " + worse + " worst " + worst.ToString("F2") + "\n");
sb.Append("terrain changed " + changed + " clamped " + clamped + " maxDelta " + maxD.ToString("F2") + "\n");
foreach (var n in new[] { "house_home", "D198_postoffice", "D198_depot", "D198_garage" }) { var t = parent.Find(n); var b = BB(t); float mx = 0; for (float x = b.min.x; x <= b.max.x; x += 1) for (float z = b.min.z; z <= b.max.z; z += 1) mx = Mathf.Max(mx, Mathf.Abs(G(x, z) - t.position.y)); sb.Append(n + " max|ground-floor| in foot " + mx.ToString("F2") + "\n"); }
AssetDatabase.SaveAssets();
return sb.ToString();
