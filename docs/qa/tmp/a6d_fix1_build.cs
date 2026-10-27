// 과제 1815 — Scene_A6d_Town 직접 고침(새 씬 없음).
//  A 집 j → j2 (fbx+png, Assets/Art/SceneA6d/houses). 재질은 기존 씬 사본 SA6d_house_* 의 _BaseMap 만 교체. 콜라이더는 a6c_build 와 같은 규칙(몸통·받침·큰 소품 -x)으로 다시.
//  B 카트 v6c → v6d (fbx+png, Assets/Art/SceneA6d/kart). a6_build 와 같은 절차(180°, 바퀴 피벗 Wheel_*, KartWheelVisual 재연결). 재질 = SA6d_kart_v6b 사본 SA6d_kart_v6d.
//  C 결함 9: A4e_Curbs 6개(보이지 않는 1m 상자, 위가 수평이라 경사 아래쪽은 땅 위 1.3m) → 땅을 따라가는 낮은 돌담 0.55m(+ 같은 메시 MeshCollider), 옛 상자는 끔.
//     재질 = 길 벽 SA6d_wall_tile_grey 사본 SA6d_stonewall. 레이어는 옛 연석과 같은 2(Ignore Raycast — 바퀴 레이가 담 위를 땅으로 읽지 않게).
//  D 결함 10: 큰 소품 상자가 길(담 앞·담 끝 밖)에서 닿을 수 있는지 판정해 로그.
const string D = "Assets/Art/SceneA6d/", S = "Assets/Scenes/Scene_A6d_Town.unity";
string root0 = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/"));
foreach (var f in new[] { "houses", "kart", "Walls" }) if (!AssetDatabase.IsValidFolder(D + f)) AssetDatabase.CreateFolder("Assets/Art/SceneA6d", f);
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(root0 + "scene-a4-houses-j2/" + h + e, Application.dataPath + "/Art/SceneA6d/houses/" + h + e, true);
foreach (var n in new[] { "kart_v6d.fbx", "kart_v6d.png" }) System.IO.File.Copy(root0 + "kart-v6d/" + n, Application.dataPath + "/Art/SceneA6d/kart/" + n, true);
AssetDatabase.Refresh();
System.Action<string, TextureWrapMode> Tex = (p, wrap) => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = wrap; ti.SaveAndReimport(); };
var oldKI = (ModelImporter)AssetImporter.GetAtPath("Assets/Art/SceneA6/kart/kart_v6.fbx");
System.Action<string, bool> Fbx = (p, kart) => { var mi = (ModelImporter)AssetImporter.GetAtPath(p); mi.materialImportMode = ModelImporterMaterialImportMode.None; if (!kart) mi.isReadable = true; else if (oldKI != null) { mi.globalScale = oldKI.globalScale; mi.useFileScale = oldKI.useFileScale; mi.bakeAxisConversion = oldKI.bakeAxisConversion; } mi.SaveAndReimport(); };
foreach (var h in H) { Tex(D + "houses/" + h + ".png", TextureWrapMode.Repeat); Fbx(D + "houses/" + h + ".fbx", false); }
Tex(D + "kart/kart_v6d.png", TextureWrapMode.Clamp); Fbx(D + "kart/kart_v6d.fbx", true);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var log = new System.Text.StringBuilder();
var tg = root.GetComponent<SceneA3Toggles>(); var mats = new System.Collections.Generic.List<Material>(tg.materials);
// ---- A 집
var body = new System.Collections.Generic.Dictionary<string, float[]> {
  { "house_narrow_a", new[] { 5f, 6f, 9f, 0f } }, { "house_narrow_b", new[] { 4f, 5f, 9f, 0f } }, { "house_shop", new[] { 6f, 6f, 6f, 0f } },
  { "house_slope", new[] { 6f, 5f, 7.6f, 1.6f } }, { "house_warehouse", new[] { 8f, 7f, 6f, 0f } }, { "house_cafe", new[] { 6f, 4f, 3f, 0f } } };
var big = new System.Collections.Generic.Dictionary<string, Vector3> {
  { "bike", new Vector3(1f, 1f, 1f) }, { "chair", new Vector3(0.5f, 0.5f, 0.9f) }, { "table", new Vector3(0.9f, 0.9f, 0.75f) }, { "wood_pile", new Vector3(1.5f, 0.8f, 0.8f) },
  { "ladder", new Vector3(0.6f, 0.4f, 2f) }, { "sawhorse", new Vector3(1f, 0.5f, 0.8f) }, { "fish_display", new Vector3(1.4f, 0.7f, 1f) }, { "styro_stack", new Vector3(0.6f, 0.5f, 1f) },
  { "menu_board", new Vector3(0.6f, 0.4f, 1f) }, { "rain_barrel", new Vector3(0.6f, 0.6f, 0.9f) }, { "toolbox", new Vector3(0.6f, 0.4f, 0.5f) } };
var parts = MiniJson(System.IO.File.ReadAllText(root0 + "scene-a4-houses-j2/parts.json"));
var propBoxes = new System.Collections.Generic.List<(string house, string what, BoxCollider bc)>();
foreach (Transform t in g) {
  var oldInst = t.GetChild(0);
  var m = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA6d_" + t.name + ".mat"); m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "houses/" + t.name + ".png")); EditorUtility.SetDirty(m);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + t.name + ".fbx"), t); inst.transform.localPosition = oldInst.localPosition; inst.transform.localRotation = oldInst.localRotation; inst.transform.localScale = oldInst.localScale;
  UnityEngine.Object.DestroyImmediate(oldInst.gameObject);
  foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
  var bd = body[t.name]; float W = bd[0], Dp = bd[1], Hw = bd[2], zb = bd[3]; var mf = inst.GetComponentInChildren<MeshFilter>(); var mb = mf.sharedMesh.bounds;
  var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, t.name == "house_slope" ? mb.max.y - Dp * 0.5f : 0f, (zb + Hw) * 0.5f); bc.size = new Vector3(W, Dp, Hw - zb);
  if (zb > 0f) { var bc2 = mf.gameObject.AddComponent<BoxCollider>(); bc2.center = new Vector3(mb.center.x, mb.center.y, zb * 0.5f); bc2.size = new Vector3(mb.size.x, mb.size.y, zb); }
  var verts = mf.sharedMesh.vertices; var bodyB = new Bounds(bc.center, bc.size + Vector3.one * 0.05f);
  int np = 0, off = 0; var offN = new System.Collections.Generic.List<string>();
  foreach (var it in parts[t.name].life) { if (!big.ContainsKey(it.what)) continue; var s = big[it.what]; var pc = mf.gameObject.AddComponent<BoxCollider>(); pc.center = new Vector3(-it.x, it.y, zb + s.z * 0.5f); pc.size = s; np++;
    var pb = new Bounds(pc.center, s); int nv = 0; foreach (var v in verts) if (pb.Contains(v) && !bodyB.Contains(v)) nv++; if (nv < 8) { pc.enabled = false; off++; offN.Add(it.what + "(" + nv + ")"); } else propBoxes.Add((t.name, it.what, pc)); }
  log.Append(t.name + " verts " + verts.Length + " meshB " + mb.size.ToString("F1") + " props " + np + " off " + off + " [" + string.Join(",", offN) + "]\n"); }
// ---- B 카트
var car = GameObject.Find("Race").transform; var kv = car.Find("A6_KartVisual"); float sc0 = car.lossyScale.x; var kw = kv.GetComponent<KartWheelVisual>();
var oldW = new System.Collections.Generic.Dictionary<string, Vector3>(); foreach (Transform c in kv) if (c.name.StartsWith("Wheel_")) oldW[c.name] = car.InverseTransformPoint(c.position) * sc0;
var oldKM = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA6d_kart_v6b.mat");
var km = new Material(oldKM) { name = "SA6d_kart_v6d" }; km.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "kart/kart_v6d.png"));
AssetDatabase.DeleteAsset(D + "Mats/SA6d_kart_v6d.mat"); AssetDatabase.CreateAsset(km, D + "Mats/SA6d_kart_v6d.mat");
int ixo = mats.IndexOf(oldKM); if (ixo >= 0) mats[ixo] = km; else mats.Add(km);
var kids = new System.Collections.Generic.List<GameObject>(); foreach (Transform c in kv) kids.Add(c.gameObject); foreach (var c in kids) UnityEngine.Object.DestroyImmediate(c);
var kinst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "kart/kart_v6d.fbx"), kv); PrefabUtility.UnpackPrefabInstance(kinst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
kinst.transform.localPosition = Vector3.zero; kinst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
foreach (var r in kinst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = km; r.sharedMaterials = ms; }
var kbody = new GameObject("KartBody").transform; kbody.SetParent(kv, false);
string[] WN = { "FL", "FR", "RL", "RR" }; var wl = new Transform[4];
for (int i = 0; i < 4; i++) {
  Transform tire = null, hub = null; foreach (var tr in kinst.GetComponentsInChildren<Transform>(true)) { if (tr.name == "k_tire_" + WN[i]) tire = tr; if (tr.name == "k_hub_" + WN[i]) hub = tr; }
  if (!tire) { log.Append("NO k_tire_" + WN[i] + "; "); continue; }
  var tb = tire.GetComponent<Renderer>().bounds; var pv = new GameObject("Wheel_" + WN[i]).transform; pv.SetParent(kv, false); pv.position = tb.center; pv.rotation = car.rotation;
  tire.SetParent(pv, true); if (hub) hub.SetParent(pv, true); wl[i] = pv;
  var nw = car.InverseTransformPoint(tb.center) * sc0; log.Append(pv.name + " " + nw.ToString("F3") + " 전 " + (oldW.ContainsKey(pv.name) ? oldW[pv.name].ToString("F3") : "?") + (hub ? "" : " nohub") + "; "); }
var rest = new System.Collections.Generic.List<Transform>(); foreach (Transform c in kv) if (c != kbody && System.Array.IndexOf(wl, c) < 0) rest.Add(c);
foreach (var t in rest) t.SetParent(kbody, true);
var kso = new SerializedObject(kw); kso.FindProperty("body").objectReferenceValue = kbody; for (int i = 0; i < 4; i++) kso.FindProperty("wheels").GetArrayElementAtIndex(i).objectReferenceValue = wl[i]; kso.ApplyModifiedProperties();
int nk = 0, ntri = 0; bool hasBox = false; foreach (var mf in kv.GetComponentsInChildren<MeshFilter>(true)) { nk++; ntri += mf.sharedMesh.triangles.Length / 3; if (mf.name.StartsWith("delivery_box")) hasBox = true; }
bool f0 = false; var vb = new Bounds(); foreach (var r in kv.GetComponentsInChildren<MeshRenderer>(true)) foreach (var q in new[] { r.bounds.min, r.bounds.max }) { var lp = car.InverseTransformPoint(q) * sc0; if (!f0) { vb = new Bounds(lp, Vector3.zero); f0 = true; } else vb.Encapsulate(lp); }
log.Append("\nkart meshes " + nk + " tris " + ntri + " delivery_box " + hasBox + " raceLocal_m min " + vb.min.ToString("F3") + " max " + vb.max.ToString("F3") + "\n");
// ---- C 돌담
var wallMat0 = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA6d_wall_tile_grey.mat");
var sm = new Material(wallMat0) { name = "SA6d_stonewall" }; AssetDatabase.DeleteAsset(D + "Mats/SA6d_stonewall.mat"); AssetDatabase.CreateAsset(sm, D + "Mats/SA6d_stonewall.mat"); if (!mats.Contains(sm)) mats.Add(sm);
// 길 벽 메시의 UV 밀도(UV/m) — 같은 결 크기로
var wmesh = GameObject.Find("Wall_L").GetComponent<MeshFilter>().sharedMesh; var wtf = GameObject.Find("Wall_L").transform; var wv = wmesh.vertices; var wu = wmesh.uv; var wt = wmesh.triangles; double sa = 0, su = 0;
for (int i = 0; i < wt.Length; i += 3) { var a = wtf.TransformPoint(wv[wt[i]]); var b = wtf.TransformPoint(wv[wt[i + 1]]); var c = wtf.TransformPoint(wv[wt[i + 2]]); sa += Vector3.Cross(b - a, c - a).magnitude; Vector3 ua = wu[wt[i]], ub = wu[wt[i + 1]], uc = wu[wt[i + 2]]; su += Vector3.Cross(ub - ua, uc - ua).magnitude; }
float uvPerM = (float)System.Math.Sqrt(su / System.Math.Max(1e-6, sa)); log.Append("wall uv/m " + uvPerM.ToString("F3") + "\n");
var wroot = root.Find("A6d_StoneWalls"); if (wroot) UnityEngine.Object.DestroyImmediate(wroot.gameObject); wroot = new GameObject("A6d_StoneWalls").transform; wroot.SetParent(root, false);
const float H0 = 0.55f, zF = 0.15f, zB = -0.27f, step = 0.5f;
var rnd = new System.Random(1815);
System.Func<Vector3, float> Ground = wp => { float best = float.NaN; foreach (var hh in Physics.RaycastAll(wp + Vector3.up * 6f, Vector3.down, 20f)) { var nm = hh.collider.name; if (nm == "Road" || nm == "DryVerge" || nm.Contains("Grass") || nm.Contains("Ground") || nm.Contains("Terrain")) if (float.IsNaN(best) || hh.point.y > best) best = hh.point.y; } return best; };
var curbs = new System.Collections.Generic.List<(Transform t, float L)>();
foreach (Transform cb in root.Find("A4e_Curbs")) {
  var cbc = cb.GetComponent<BoxCollider>(); float L = cbc.size.x; curbs.Add((cb, L)); int N = Mathf.Max(2, Mathf.CeilToInt(L / step)); float dx = L / N;
  var go = new GameObject("StoneWall_" + cb.name.Substring(5)); go.transform.SetParent(wroot, false); go.transform.SetPositionAndRotation(cb.position, cb.rotation); go.layer = cb.gameObject.layer;
  var top = new float[N + 1]; var bot = new float[N + 1]; float last = 0f; int miss = 0; float minG = 99, maxG = -99;
  for (int i = 0; i <= N; i++) { float x = -L * 0.5f + i * dx; float gF = Ground(cb.TransformPoint(new Vector3(x, 0, zF))), gB = Ground(cb.TransformPoint(new Vector3(x, 0, zB)));
    float gy; if (float.IsNaN(gF) && float.IsNaN(gB)) { gy = last; miss++; } else gy = float.IsNaN(gF) ? gB : float.IsNaN(gB) ? gF : Mathf.Max(gF, gB); last = gy;
    float lyHi = cb.InverseTransformPoint(new Vector3(0, gy, 0)).y + cb.position.y * 0f; float ly = gy - cb.position.y; minG = Mathf.Min(minG, ly); maxG = Mathf.Max(maxG, ly);
    top[i] = ly + H0 + (float)(rnd.NextDouble() * 2 - 1) * 0.03f; bot[i] = (float.IsNaN(gF) || float.IsNaN(gB) ? gy : Mathf.Min(gF, gB)) - cb.position.y - 0.25f; }
  var V = new System.Collections.Generic.List<Vector3>(); var U = new System.Collections.Generic.List<Vector2>(); var T = new System.Collections.Generic.List<int>();
  System.Action<Vector3, Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, Vector2> Quad = (a, b, c, d, ua, ub, uc, ud) => { int o = V.Count; V.Add(a); V.Add(b); V.Add(c); V.Add(d); U.Add(ua); U.Add(ub); U.Add(uc); U.Add(ud); T.Add(o); T.Add(o + 1); T.Add(o + 2); T.Add(o); T.Add(o + 2); T.Add(o + 3); };
  for (int i = 0; i < N; i++) { float x0 = -L * 0.5f + i * dx, x1 = x0 + dx; float u0 = x0 * uvPerM, u1 = x1 * uvPerM;
    // 앞면(+z, 길 쪽) · 뒷면 · 윗면. 시계방향 = 바깥에서 볼 때
    Quad(new Vector3(x1, bot[i + 1], zF), new Vector3(x1, top[i + 1], zF), new Vector3(x0, top[i], zF), new Vector3(x0, bot[i], zF), new Vector2(u1, bot[i + 1] * uvPerM), new Vector2(u1, top[i + 1] * uvPerM), new Vector2(u0, top[i] * uvPerM), new Vector2(u0, bot[i] * uvPerM));
    Quad(new Vector3(x0, bot[i], zB), new Vector3(x0, top[i], zB), new Vector3(x1, top[i + 1], zB), new Vector3(x1, bot[i + 1], zB), new Vector2(u0, bot[i] * uvPerM), new Vector2(u0, top[i] * uvPerM), new Vector2(u1, top[i + 1] * uvPerM), new Vector2(u1, bot[i + 1] * uvPerM));
    Quad(new Vector3(x0, top[i], zB), new Vector3(x0, top[i], zF), new Vector3(x1, top[i + 1], zF), new Vector3(x1, top[i + 1], zB), new Vector2(u0, zB * uvPerM), new Vector2(u0, zF * uvPerM), new Vector2(u1, zF * uvPerM), new Vector2(u1, zB * uvPerM)); }
  float xa = -L * 0.5f, xb = L * 0.5f;   // 양 끝 마구리
  Quad(new Vector3(xa, bot[0], zF), new Vector3(xa, top[0], zF), new Vector3(xa, top[0], zB), new Vector3(xa, bot[0], zB), new Vector2(zF * uvPerM, bot[0] * uvPerM), new Vector2(zF * uvPerM, top[0] * uvPerM), new Vector2(zB * uvPerM, top[0] * uvPerM), new Vector2(zB * uvPerM, bot[0] * uvPerM));
  Quad(new Vector3(xb, bot[N], zB), new Vector3(xb, top[N], zB), new Vector3(xb, top[N], zF), new Vector3(xb, bot[N], zF), new Vector2(zB * uvPerM, bot[N] * uvPerM), new Vector2(zB * uvPerM, top[N] * uvPerM), new Vector2(zF * uvPerM, top[N] * uvPerM), new Vector2(zF * uvPerM, bot[N] * uvPerM));
  var mesh = new Mesh { name = go.name }; mesh.SetVertices(V); mesh.SetUVs(0, U); mesh.SetTriangles(T, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
  string mp = D + "Walls/" + go.name + ".asset"; AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
  go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = sm; go.AddComponent<MeshCollider>().sharedMesh = mesh;
  cbc.enabled = false;
  log.Append(go.name + " L " + L.ToString("F1") + " 땅(로컬y) " + minG.ToString("F2") + "~" + maxG.ToString("F2") + " 옛상자 위 " + (cbc.center.y + 0.5f).ToString("F2") + " → 옛 땅위높이 " + (cbc.center.y + 0.5f - maxG).ToString("F2") + "~" + (cbc.center.y + 0.5f - minG).ToString("F2") + "m, 레이 놓침 " + miss + "\n"); }
// ---- D 소품 상자: 길에서 닿나 (담 앞쪽 z>zB 이거나 담 끝 밖)
foreach (var p in propBoxes) {
  var b = p.bc; var hsz = b.size * 0.5f; var house = p.house; Transform cb = null; float L = 0; foreach (var c in curbs) if (c.t.name == "Curb_" + house) { cb = c.t; L = c.L; }
  float zmax = -99, xmin = 99, xmax = -99;
  foreach (var sx in new[] { -1f, 1f }) foreach (var sy in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) { var lp = cb.InverseTransformPoint(b.transform.TransformPoint(b.center + Vector3.Scale(hsz, new Vector3(sx, sy, sz)))); zmax = Mathf.Max(zmax, lp.z); xmin = Mathf.Min(xmin, lp.x); xmax = Mathf.Max(xmax, lp.x); }
  bool reach = zmax > zB || xmax < -L * 0.5f || xmin > L * 0.5f;
  log.Append("prop " + house + "/" + p.what + " 담기준 z<=" + zmax.ToString("F2") + " x " + xmin.ToString("F1") + "~" + xmax.ToString("F1") + " (담 ±" + (L * 0.5f).ToString("F1") + ") " + (reach ? "길에서 닿음" : "담 뒤") + "\n"); }
tg.materials = mats.ToArray(); tg.Apply();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();

System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<(string what, float x, float y)> life, int _)> MiniJson(string js) {
  var res = new System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<(string what, float x, float y)> life, int _)>();
  foreach (var h in H) { var lst = new System.Collections.Generic.List<(string, float, float)>(); int a = js.IndexOf("\"" + h + "\""); int nx = js.Length; foreach (var h2 in H) { int b = js.IndexOf("\"" + h2 + "\""); if (b > a && b < nx) nx = b; }
    int l = js.IndexOf("\"life\"", a); if (l >= 0 && l < nx) { int e = js.IndexOf(']', l); var seg = js.Substring(l, e - l);
      foreach (System.Text.RegularExpressions.Match mm in System.Text.RegularExpressions.Regex.Matches(seg, "\\{\\s*\"what\"\\s*:\\s*\"(\\w+)\"\\s*,\\s*\"x\"\\s*:\\s*(-?[\\d.]+)\\s*,\\s*\"y\"\\s*:\\s*(-?[\\d.]+)"))
        lst.Add((mm.Groups[1].Value, float.Parse(mm.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(mm.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture))); }
    res[h] = (lst, 0); }
  return res; }
