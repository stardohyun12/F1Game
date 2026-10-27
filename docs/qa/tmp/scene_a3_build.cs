// 과제 1124 — Scene_A3_Town = A2f 복사, 카메라 B 그대로. 6항목, 전부 토글(docs/qa/tmp/scene_a3_caps.cs 가 켜고 끔). 값 = 가설.
//  재질: SA2f_* → SA3_* (셰이더 PitCrew/SceneA3/Look — R1 _Ramp · R2 _R2 · R3 _Outline 을 재질 값으로 켬)
//  메쉬: A3_M1(둔덕·바위·지평선 언덕) / A3_M2(접지 데칼·길가 풀덩이·연석 조각·작은 바위 군집; 길 폭별 edge5·edge4) / A3_M3(길 4 m·담 당김 + 덤불 줄·울타리·나무 줄·소품 무리)
//  A3_M3off = 원래 길·담·마른 풀 띠(M3 끄면 이쪽이 켜짐). 텍스처 = docs/qa/tmp/a3_tex.py
const string D = "Assets/Art/SceneA3/", G = "Assets/Art/SceneA3/Gen/", S = "Assets/Scenes/Scene_A3_Town.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A2f_Town.unity", S);
AssetDatabase.CopyAsset("Assets/Art/SceneA2/GenF/Gen/road_a2f.png", G + "road_r2.png");
AssetDatabase.Refresh();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
AssetDatabase.DeleteAsset(D + "Mats"); AssetDatabase.CreateFolder("Assets/Art/SceneA3", "Mats");
var sh = Shader.Find("PitCrew/SceneA3/Look"); var shC = Shader.Find("PitCrew/SceneA3/Contact");
if (!sh || !shC) return "셰이더 없음 " + sh + " " + shC;
var R = TextureWrapMode.Repeat; var C = TextureWrapMode.Clamp;
System.Func<string, TextureWrapMode, TextureWrapMode, Texture2D> Tex = (p, wu, wv) => {
  AssetDatabase.ImportAsset(p); var ti = (TextureImporter)AssetImporter.GetAtPath(p);
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192; ti.alphaIsTransparency = true;
  ti.npotScale = TextureImporterNPOTScale.None; ti.wrapModeU = wu; ti.wrapModeV = wv; ti.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(p); };
var map = new System.Collections.Generic.Dictionary<Material, Material>(); var log = new System.Text.StringBuilder();
System.Func<Material, Material> Conv = src => {
  if (src == null || !src.name.StartsWith("SA2f_")) return src; if (map.TryGetValue(src, out var mm)) return mm;
  var m = new Material(src) { name = "SA3_" + src.name.Substring(5) }; m.shader = sh; string n = m.name;
  bool card = n.Contains("autumn_tree") || n.Contains("wire") || n.Contains("sky") || n.Contains("sea") || n.Contains("dry_strip");
  m.SetFloat("_R1Ok", card ? 0 : 1); m.SetFloat("_R3Ok", (card || n.Contains("grass_tile") || n.Contains("asphalt") || n.Contains("yard_ground") || n.Contains("far_terrace")) ? 0 : 1);
  Texture2D r2 = null;
  if (n.Contains("autumn_tree")) { string k = n.Substring(n.IndexOf("autumn_tree")).Replace("_soft", ""); r2 = Tex(G + k + "_r2.png", R, R); }
  else if (n.Contains("dry_strip")) r2 = Tex(G + "dry_strip_r2.png", C, R);
  else if (n.Contains("sky")) r2 = Tex(G + "sky_r2.png", R, C);
  else if (n.Contains("asphalt")) r2 = Tex(G + "road_r2.png", C, R);
  if (r2) { m.SetTexture("_BaseMapR2", r2); m.SetFloat("_HasR2", 1); }
  AssetDatabase.CreateAsset(m, D + "Mats/" + n + ".mat"); map[src] = m; return m; };
int rn = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{ var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Conv(ms[i]); r.sharedMaterials = ms; rn++; }
var root = GameObject.Find("SceneA2f"); root.name = "SceneA3";
System.Func<string, Material> M = n => AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA3_" + n + ".mat");
var grassM = M("grass_tile_soft");
System.Func<string, Material, string, Color, float, Material> NewMat = (n, baseM, tex, tint, r3) => {
  var m = new Material(baseM) { name = "SA3_" + n }; if (tex != null) m.SetTexture("_BaseMap", Tex(tex, R, R)); m.SetColor("_Tint", tint);
  m.SetFloat("_R1Ok", 1); m.SetFloat("_R3Ok", r3); m.SetFloat("_HasR2", 0); m.SetFloat("_Cutoff", 0); AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); return m; };
var matOf = new System.Collections.Generic.Dictionary<string, Material>();
matOf["mound"] = NewMat("mound", grassM, null, Color.white, 1);
matOf["clump"] = NewMat("clump", grassM, null, new Color(0.84f, 0.92f, 0.80f), 0);
matOf["rock"] = NewMat("rock", grassM, G + "rock.png", Color.white, 1);
matOf["bush"] = NewMat("bush", grassM, G + "bush.png", Color.white, 1);
matOf["wood"] = NewMat("wood", M("pole_wood"), null, Color.white, 1);
var uvs = new System.Collections.Generic.Dictionary<string, float> { { "mound", 1f / 32f }, { "clump", 1f / 32f }, { "rock", 0.5f }, { "bush", 0.5f }, { "wood", 1f } };   // uv = m × 32 / 텍스처 px  (R2 기준 32 텍셀/m)
var decalM = new Material(shC) { name = "SA3_contact" }; AssetDatabase.CreateAsset(decalM, D + "Mats/SA3_contact.mat");

// ---- 땅 높이: 임시 MeshCollider(땅·길·마당)로 위에서 쏨, 저장 전 지움
var temp = new System.Collections.Generic.HashSet<Collider>();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
  if (r.name == "Terrain" || r.name == "Headland" || r.name == "Road" || r.name == "yard_ground") temp.Add(r.gameObject.AddComponent<MeshCollider>());
Physics.SyncTransforms(); int gMiss = 0;
System.Func<float, float, float> Ground = (x, z) => { float best = float.NegativeInfinity;
  foreach (var h in Physics.RaycastAll(new Vector3(x, 60f, z), Vector3.down, 200f)) if (temp.Contains(h.collider)) best = Mathf.Max(best, h.point.y);
  if (best == float.NegativeInfinity) { gMiss++; return -0.5f; } return best; };

// ---- 길 줄: 가운데·옆 방향(+ = 정점 0 쪽)
var roadGo = GameObject.Find("Road"); var rv = roadGo.GetComponent<MeshFilter>().sharedMesh.vertices; var lw = roadGo.transform.localToWorldMatrix; int rows = rv.Length / 6;
var cs = new Vector3[rows]; var ls = new Vector3[rows];
for (int j = 0; j < rows; j++) { Vector3 a = lw.MultiplyPoint3x4(rv[j * 6]), b = lw.MultiplyPoint3x4(rv[j * 6 + 5]); cs[j] = (a + b) * 0.5f; var d = a - b; d.y = 0; ls[j] = d.normalized; }
System.Func<int, float, Vector3> Side = (j, s) => { var p = cs[j] + ls[j] * s; p.y = Ground(p.x, p.z); return p; };

// ---- 막는 것(나무·집·소품) 경계 — 새 덩어리를 이 위에 안 얹음
var block = new System.Collections.Generic.List<Bounds>(); var props = new System.Collections.Generic.List<Renderer>();
string[] propN = { "tires_", "drum_", "pallet_", "pot_", "cone_", "bench_", "old_car", "bicycle", "streetlight", "transformer" };
foreach (Transform ch in root.transform) { var r = ch.GetComponent<Renderer>(); string n = ch.name;
  if (n.StartsWith("autumn_tree") || n.StartsWith("garage") || n.StartsWith("background_building") || n.StartsWith("Pole_") || n == "Kart" || n == "yard_ground") { var b = r ? r.bounds : new Bounds(ch.position, Vector3.one * 2); if (n.StartsWith("autumn_tree")) b = new Bounds(new Vector3(b.center.x, 0, b.center.z), new Vector3(3, 1, 3)); block.Add(b); }
  foreach (var p in propN) if (n.StartsWith(p) && r) { props.Add(r); block.Add(r.bounds); } }
System.Func<Vector3, float, bool> Free = (p, rad) => { foreach (var b in block) { var c = b.ClosestPoint(new Vector3(p.x, b.center.y, p.z)); c.y = p.y; if (Vector3.Distance(c, p) < rad) return false; } return true; };

// ---- 평평한 면 메쉬 만들기(삼각형마다 정점 따로 = 면 평평, 바깥쪽 감김 맞춤)
var BV = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Vector3>>(); var BU = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Vector2>>();
System.Action<string, Vector3, Vector3, Vector3, Vector3> Tri = (key, a, b, c, ctr) => {
  var n = Vector3.Cross(b - a, c - a); if (n.sqrMagnitude < 1e-10f) return; if (Vector3.Dot(n, (a + b + c) / 3f - ctr) < 0) { var t0 = b; b = c; c = t0; n = -n; } n.Normalize();
  if (!BV.ContainsKey(key)) { BV[key] = new System.Collections.Generic.List<Vector3>(); BU[key] = new System.Collections.Generic.List<Vector2>(); }
  string mk = key.Substring(key.IndexOf('/') + 1); float s = uvs[mk];
  Vector3 t = Mathf.Abs(n.y) > 0.6f ? Vector3.right : Vector3.Cross(Vector3.up, n).normalized; Vector3 bt = Mathf.Abs(n.y) > 0.6f ? Vector3.forward : Vector3.up;
  foreach (var p in new[] { a, b, c }) { BV[key].Add(p); BU[key].Add(new Vector2(Vector3.Dot(p, t), Vector3.Dot(p, bt)) * s); } };
// 둔덕: 바닥 고리(땅 따라, 0.25 m 묻음) → 중간 고리 → 꼭대기
System.Action<string, Vector3, float, float, float, int, int> Mound = (key, c, rx, rz, h, segs, seed) => {
  var rnd = new System.Random(seed); var r0 = new Vector3[segs]; var r1 = new Vector3[segs]; float baseY = c.y; float rot = (float)rnd.NextDouble() * 6.28f;
  for (int i = 0; i < segs; i++) { float a = rot + i * 6.2832f / segs + (float)(rnd.NextDouble() - 0.5) * 0.5f; float k = 0.85f + (float)rnd.NextDouble() * 0.3f;
    float x = c.x + Mathf.Cos(a) * rx * k, z = c.z + Mathf.Sin(a) * rz * k; r0[i] = new Vector3(x, Ground(x, z) - 0.25f, z);
    float k1 = 0.45f + (float)rnd.NextDouble() * 0.25f; r1[i] = new Vector3(c.x + Mathf.Cos(a) * rx * k1, baseY + h * (0.55f + (float)rnd.NextDouble() * 0.3f), c.z + Mathf.Sin(a) * rz * k1); }
  var top = new Vector3(c.x + (float)(rnd.NextDouble() - 0.5) * rx * 0.3f, baseY + h, c.z + (float)(rnd.NextDouble() - 0.5) * rz * 0.3f); var ctr = new Vector3(c.x, baseY + h * 0.2f, c.z);
  for (int i = 0; i < segs; i++) { int i1 = (i + 1) % segs; Tri(key, r0[i], r0[i1], r1[i], ctr); Tri(key, r1[i], r0[i1], r1[i1], ctr); Tri(key, r1[i], r1[i1], top, ctr); } };
System.Action<string, Vector3, Vector3, Vector3, Vector3> Box = (key, c, ax, ay, az) => {   // 가운데 c, 반축 ax·ay·az
  var v = new Vector3[8]; for (int i = 0; i < 8; i++) v[i] = c + ax * ((i & 1) == 0 ? -1 : 1) + ay * ((i & 2) == 0 ? -1 : 1) + az * ((i & 4) == 0 ? -1 : 1);
  int[] q = { 0, 1, 3, 2, 4, 5, 7, 6, 0, 1, 5, 4, 2, 3, 7, 6, 0, 2, 6, 4, 1, 3, 7, 5 };
  for (int f = 0; f < 6; f++) { Tri(key, v[q[f * 4]], v[q[f * 4 + 1]], v[q[f * 4 + 2]], c); Tri(key, v[q[f * 4]], v[q[f * 4 + 2]], v[q[f * 4 + 3]], c); } };
var decals = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Vector3>>();
System.Action<string, Vector3, float> Decal = (grp, p, size) => { if (!decals.ContainsKey(grp)) decals[grp] = new System.Collections.Generic.List<Vector3>(); decals[grp].Add(new Vector3(p.x, Ground(p.x, p.z) + 0.03f, p.z)); decals[grp].Add(new Vector3(size, 0, 0)); };

var gM1 = new GameObject("A3_M1"); var gM2 = new GameObject("A3_M2"); var gM3 = new GameObject("A3_M3"); var gOff = new GameObject("A3_M3off");
var gE5 = new GameObject("edge5"); var gE4 = new GameObject("edge4");
foreach (var g in new[] { gM1, gM2, gM3, gOff }) g.transform.SetParent(root.transform, true); gE5.transform.SetParent(gM2.transform, true); gE4.transform.SetParent(gM2.transform, true);
var rng = new System.Random(1124); System.Func<float, float, float> Rn = (a, b) => a + (float)rng.NextDouble() * (b - a);
int nMound = 0, nRock = 0, nHill = 0, nClump = 0, nCurb = 0, nBush = 0, nPost = 0, nTree = 0, nClone = 0, nDecal = 0;

// ===== M1 지형 기복: 길 양옆 둔덕(1~3 m) · 바위 덩어리 · 지평선 언덕
for (int j = 0; j < rows; j += 2) foreach (int sd in new[] { -1, 1 }) {
  if (rng.NextDouble() > 0.75) continue; float rad = Rn(2.5f, 5.5f), lat = 4.2f + rad + Rn(0.5f, 6f); var p = Side(j, sd * lat);
  if (!Free(p, rad * 0.8f)) continue; Mound("M1/mound", p, rad, rad * Rn(0.7f, 1.2f), Rn(1f, 3f), rng.Next(6, 9), rng.Next()); nMound++;
  if (rng.NextDouble() < 0.35) { var q = Side(j, sd * (lat - rad * 0.6f)); float rr = Rn(0.7f, 1.5f); if (Free(q, rr)) { Mound("M1/rock", q, rr, rr * Rn(0.7f, 1f), rr * Rn(0.8f, 1.3f), rng.Next(5, 7), rng.Next()); nRock++; } } }
var camB = GameObject.Find("Cam_B_Game").transform;
for (int i = 0; i < 10; i++) { float z = camB.position.z - Rn(55f, 170f); float x = (i % 2 == 0 ? -1 : 1) * Rn(22f, 95f); float rad = Rn(16f, 34f);
  var p = new Vector3(x, Ground(x, z), z); Mound("M1/mound", p, rad, rad * Rn(0.6f, 1f), Rn(6f, 18f), rng.Next(7, 10), rng.Next()); nHill++; }

// ===== M2 접지·경계: 소품 밑 접지 데칼 / 길가 풀덩이·연석 조각(길 폭 5 m·4 m 따로) / 작은 바위 군집
foreach (var r in props) { var b = r.bounds; Decal("M2", b.center, Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) * 1.5f + 0.3f, 0.7f, 5f)); nDecal++; }
foreach (Transform ch in root.transform) { if (ch.name.StartsWith("autumn_tree")) { Decal("M2", ch.position, 2.6f); nDecal++; } if (ch.name.StartsWith("Pole_")) { Decal("M2", ch.position, 1.0f); nDecal++; } if (ch.name == "Kart") { Decal("M2", ch.position, 2.0f); nDecal++; } }
foreach (var hw in new[] { 2.5f, 2.0f }) { string ek = hw > 2.2f ? "E5" : "E4"; var er = new System.Random(77);
  foreach (int sd in new[] { -1, 1 }) for (float j = 0; j < rows - 1; j += 0.6f + (float)er.NextDouble() * 1.2f) {
    int jj = (int)j; var p = Side(jj, sd * (hw + (float)(er.NextDouble() * 0.6 - 0.25)));
    if (er.NextDouble() < 0.16) { var ax = ls[jj] * 0.12f; var az = Vector3.Cross(ls[jj], Vector3.up) * (0.22f + (float)er.NextDouble() * 0.2f); Box(ek + "/rock", p + Vector3.up * 0.04f, ax, Vector3.up * 0.08f + ls[jj] * 0.02f, az); if (ek == "E5") nCurb++; }
    else { float rr = 0.25f + (float)er.NextDouble() * 0.4f; Mound(ek + "/clump", p, rr, rr * 0.8f, 0.15f + (float)er.NextDouble() * 0.3f, 5, er.Next()); if (ek == "E5") nClump++; } } }
for (int k = 0; k < 8; k++) { int j = rng.Next(2, rows - 4); int sd = rng.NextDouble() < 0.5 ? -1 : 1; var c0 = Side(j, sd * Rn(3.2f, 6f)); int n = rng.Next(3, 6);
  for (int i = 0; i < n; i++) { var q = new Vector3(c0.x + Rn(-0.8f, 0.8f), 0, c0.z + Rn(-0.8f, 0.8f)); q.y = Ground(q.x, q.z); float rr = Rn(0.18f, 0.5f); Mound("M2/rock", q, rr, rr * 0.8f, rr * Rn(0.7f, 1.2f), 5, rng.Next()); nRock++; } }

// ===== M3 밀도·스케일: 길 5 → 4 m(담·마른 풀 띠도 0.5 m 안으로), 덤불 줄·울타리·나무 줄, 소품 무리
System.Func<GameObject, float, float, float, GameObject> Pinch = (src, lo, hi, amt) => {   // |옆 거리| 가 lo~hi 인 정점을 가운데로 amt 당김 (amt < 0 이면 비율 = 길 자체)
  var cp = UnityEngine.Object.Instantiate(src, src.transform.parent); cp.name = src.name; var mf = cp.GetComponent<MeshFilter>(); var mm = UnityEngine.Object.Instantiate(mf.sharedMesh); mm.name = "a3_" + src.name;
  var vv = mm.vertices; var l2w = cp.transform.localToWorldMatrix; var w2l = cp.transform.worldToLocalMatrix;
  for (int i = 0; i < vv.Length; i++) { var p = l2w.MultiplyPoint3x4(vv[i]); int bj = 0; float bd = float.MaxValue;
    for (int j = 0; j < rows; j++) { float dd = (new Vector2(p.x - cs[j].x, p.z - cs[j].z)).sqrMagnitude; if (dd < bd) { bd = dd; bj = j; } }
    float s = Vector3.Dot(p - cs[bj], ls[bj]); if (Mathf.Abs(s) < lo || Mathf.Abs(s) > hi) continue;
    p -= ls[bj] * (amt < 0 ? s * -amt : Mathf.Sign(s) * amt); vv[i] = w2l.MultiplyPoint3x4(p); }
  mm.vertices = vv; mm.RecalculateBounds(); AssetDatabase.CreateAsset(mm, D + "Mats/a3_" + src.name + "_" + src.GetInstanceID() + ".asset"); mf.sharedMesh = mm; return cp; };
foreach (var nm in new[] { "Road", "Wall_L", "Wall_R", "DryVerge" }) { var src = root.transform.Find(nm).gameObject;
  var cp = nm == "Road" ? Pinch(src, 0f, 3f, -0.2f) : Pinch(src, 1.9f, 4.8f, 0.5f); cp.transform.SetParent(gM3.transform, true); src.transform.SetParent(gOff.transform, true); }
// 덤불 줄(담 바로 바깥) — 끊기는 곳 조금, 막는 것 피함
for (float j = 0; j < rows - 1; j += 0.8f + (float)rng.NextDouble() * 0.9f) foreach (int sd in new[] { -1, 1 }) {
  int jj = (int)j; if (rng.NextDouble() < 0.18) continue; float rr = Rn(0.55f, 1.05f); var p = Side(jj, sd * (2.6f + rr + Rn(0f, 0.8f)));
  if (!Free(p, rr * 0.7f)) continue; Mound("M3/bush", p, rr, rr * Rn(0.7f, 1f), Rn(0.7f, 1.4f), rng.Next(6, 8), rng.Next()); nBush++; }
// 울타리: 오른쪽(+옆) 길가 구간 2곳, 기둥 2 m 간격 + 가로대 2줄
foreach (var seg in new[] { new Vector2Int(4, 16), new Vector2Int(24, 38) }) { Vector3 prev = Vector3.zero; for (int j = seg.x; j <= seg.y; j++) {
  var p = Side(j, 2.9f + 0.9f); var fw = Vector3.Cross(ls[j], Vector3.up).normalized; Box("M3/wood", p + Vector3.up * 0.5f, ls[j] * 0.06f, Vector3.up * 0.55f, fw * 0.06f); nPost++;
  if (j > seg.x) foreach (var hy in new[] { 0.45f, 0.85f }) { var a = prev + Vector3.up * hy; var b = p + Vector3.up * hy; var mid = (a + b) * 0.5f; var dirv = (b - a) * 0.5f; Box("M3/wood", mid, ls[j] * 0.04f, Vector3.up * 0.05f, dirv); }
  prev = p; } }
// 나무 줄: 기존 단풍 나무 4종을 복제, 옆 6~9 m, 약 10 m 마다 좌우 번갈아
var trees = new System.Collections.Generic.List<GameObject>(); foreach (Transform ch in root.transform) if (ch.name.StartsWith("autumn_tree")) trees.Add(ch.gameObject);
for (int j = 3, k = 0; j < rows - 2; j += rng.Next(4, 7), k++) { int sd = k % 2 == 0 ? 1 : -1; var p = Side(j, sd * Rn(6.5f, 9f)); if (!Free(p, 2.2f)) continue;
  var src = trees[rng.Next(trees.Count)]; var t = UnityEngine.Object.Instantiate(src, gM3.transform); t.name = src.name + "_row"; var off = src.transform.position - src.GetComponent<Renderer>().bounds.center; off.y = 0;
  t.transform.position = new Vector3(p.x, p.y + (src.transform.position.y - Ground(src.transform.position.x, src.transform.position.z)), p.z); t.transform.localScale = src.transform.localScale * Rn(0.75f, 1.05f);
  block.Add(new Bounds(p, new Vector3(3, 1, 3))); Decal("M3", p, 2.6f); nTree++; nDecal++; }
// 소품 무리: 길가 소품마다 같은 종류 1~2개를 바깥쪽으로 붙여 놓음
foreach (var r in props) { string n = r.name; if (!(n.StartsWith("drum_") || n.StartsWith("tires_") || n.StartsWith("pot_") || n.StartsWith("pallet_") || n.StartsWith("cone_"))) continue;
  int bj = 0; float bd = float.MaxValue; for (int j = 0; j < rows; j++) { float dd = (r.bounds.center - cs[j]).sqrMagnitude; if (dd < bd) { bd = dd; bj = j; } }
  float sd = Mathf.Sign(Vector3.Dot(r.bounds.center - cs[bj], ls[bj])); int cnt = rng.Next(1, 3);
  for (int i = 0; i < cnt; i++) { var c = UnityEngine.Object.Instantiate(r.gameObject, gM3.transform); c.name = n + "_grp"; float sz = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
    var fw = Vector3.Cross(ls[bj], Vector3.up).normalized; var p = r.transform.position + ls[bj] * sd * sz * Rn(0.6f, 1.1f) + fw * sz * Rn(-1.1f, 1.1f);
    p.y = r.transform.position.y + (Ground(p.x, p.z) - Ground(r.transform.position.x, r.transform.position.z)); c.transform.position = p; c.transform.Rotate(0, Rn(-40f, 40f), 0, Space.World); Decal("M3", p, Mathf.Clamp(sz * 1.5f + 0.3f, 0.7f, 3f)); nClone++; nDecal++; } }

// ---- 만든 메쉬를 오브젝트로(그룹/재질 키마다 메쉬 1개)
var parentOf = new System.Collections.Generic.Dictionary<string, GameObject> { { "M1", gM1 }, { "M2", gM2 }, { "M3", gM3 }, { "E5", gE5 }, { "E4", gE4 } };
foreach (var kv in BV) { string grp = kv.Key.Substring(0, kv.Key.IndexOf('/')), mk = kv.Key.Substring(kv.Key.IndexOf('/') + 1);
  var mesh = new Mesh { name = "a3_" + kv.Key.Replace("/", "_"), indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(kv.Value); mesh.SetUVs(0, BU[kv.Key]);
  var idx = new int[kv.Value.Count]; for (int i = 0; i < idx.Length; i++) idx[i] = i; mesh.SetTriangles(idx, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
  AssetDatabase.CreateAsset(mesh, D + "Mats/" + mesh.name + ".asset");
  var go = new GameObject(mesh.name); go.transform.SetParent(parentOf[grp].transform, true); go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = matOf[mk]; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
  log.Append(mesh.name + " tris " + idx.Length / 3 + "; "); }
foreach (var kv in decals) { var vs = new System.Collections.Generic.List<Vector3>(); var us = new System.Collections.Generic.List<Vector2>(); var ts = new System.Collections.Generic.List<int>();
  for (int i = 0; i < kv.Value.Count; i += 2) { var p = kv.Value[i]; float h = kv.Value[i + 1].x * 0.5f; int b0 = vs.Count;
    vs.Add(p + new Vector3(-h, 0, -h)); vs.Add(p + new Vector3(-h, 0, h)); vs.Add(p + new Vector3(h, 0, h)); vs.Add(p + new Vector3(h, 0, -h));
    us.Add(new Vector2(0, 0)); us.Add(new Vector2(0, 1)); us.Add(new Vector2(1, 1)); us.Add(new Vector2(1, 0)); ts.AddRange(new[] { b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3 }); }
  var mesh = new Mesh { name = "a3_contact_" + kv.Key }; mesh.SetVertices(vs); mesh.SetUVs(0, us); mesh.SetTriangles(ts, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, D + "Mats/" + mesh.name + ".asset");
  var go = new GameObject(mesh.name); go.transform.SetParent(parentOf[kv.Key].transform, true); go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = decalM; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }

foreach (var c in temp) UnityEngine.Object.DestroyImmediate(c);
// 기본 상태 = 전부 켬
foreach (var m in map.Values) { m.SetFloat("_Ramp", 1); m.SetFloat("_R2", 1); m.SetFloat("_Outline", 1); } foreach (var m in matOf.Values) { m.SetFloat("_Ramp", 1); m.SetFloat("_Outline", 1); }
gE5.SetActive(false); gOff.SetActive(false);
int shadows = 0; foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (l.shadows != LightShadows.None) shadows++;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
return "renderers " + rn + " · 재질 " + map.Count + "+" + matOf.Count + " · 길 행 " + rows + " · 둔덕 " + nMound + " 언덕 " + nHill + " 바위 " + nRock + " · 풀덩이 " + nClump + " 연석 " + nCurb + " · 덤불 " + nBush + " 울타리 기둥 " + nPost + " 나무 줄 " + nTree + " 소품 복제 " + nClone + " 데칼 " + nDecal + " · 땅 못 맞음 " + gMiss + " · 그림자 빛 " + shadows + "\n" + log;
