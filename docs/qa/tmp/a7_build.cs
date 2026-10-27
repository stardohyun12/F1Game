// 과제 2050 — Scene_A7_Light = A6d 사본 + albedo 텍스처 + PitCrew/SceneA7/Lit 재질 사본(SA7_*) + 사진 키트 + A7LightToggles
const string D6 = "Assets/Art/SceneA6d/", D = "Assets/Art/SceneA7/", S6 = "Assets/Scenes/Scene_A6d_Town.unity", S = "Assets/Scenes/Scene_A7_Light.unity";
string ex = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/")); string adp = Application.dataPath + "/Art/SceneA7/";
var log = new System.Text.StringBuilder();
foreach (var f in new[] { "Mats", "houses", "Tex", "photokit" }) if (!AssetDatabase.IsValidFolder(D + f)) AssetDatabase.CreateFolder("Assets/Art/SceneA7", f);
var sh = Shader.Find("PitCrew/SceneA7/Lit"); if (!sh) return "no shader";
// ---- 파일 복사
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(ex + "scene-a4-houses-alb/" + h + e, adp + "houses/" + h + e, true);
foreach (var p in System.IO.Directory.GetFiles(ex + "scene-albedo-kit/", "*.png")) System.IO.File.Copy(p, adp + "Tex/" + System.IO.Path.GetFileName(p), true);
string[] PK = { "vending_G1", "vending_G2", "postbox_G1", "ac_G3" };
foreach (var n in PK) System.IO.File.Copy(ex + "scene-a6-photo-kit/" + n + ".fbx", adp + "photokit/" + n + ".fbx", true);
AssetDatabase.Refresh();
System.Action<string, TextureWrapMode> Tex = (p, wrap) => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = wrap; ti.alphaIsTransparency = p.Contains("autumn_tree"); ti.SaveAndReimport(); };
foreach (var h in H) { Tex(D + "houses/" + h + ".png", TextureWrapMode.Repeat); var mi = (ModelImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.isReadable = true; mi.SaveAndReimport(); }
foreach (var p in System.IO.Directory.GetFiles(adp + "Tex/", "*.png")) { var n = System.IO.Path.GetFileNameWithoutExtension(p); bool tile = n.StartsWith("stone_") || n.StartsWith("tin_"); Tex(D + "Tex/" + n + ".png", tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp); }
foreach (var n in PK) { var mi = (ModelImporter)AssetImporter.GetAtPath(D + "photokit/" + n + ".fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.SaveAndReimport(); }
System.Func<string, Texture2D> T = n => AssetDatabase.LoadAssetAtPath<Texture2D>(D + "Tex/" + n + ".png");
// ---- 재질 사본 SA6d_* → SA7_*
var map = new System.Collections.Generic.Dictionary<Material, Material>();
foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { D6 + "Mats" })) {
  var src = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)); string nn = "SA7_" + src.name.Substring(5); string dp = D + "Mats/" + nn + ".mat";
  AssetDatabase.DeleteAsset(dp); var m = new Material(src) { name = nn };
  if (src.shader.name == "PitCrew/SceneA3/Look") {
    int rq = src.renderQueue; m.shader = sh; m.renderQueue = rq;
    bool baked = nn.Contains("far_") || nn.Contains("sea_") || nn.Contains("background");   // 먼 배경·바다 = 빛 구운 판 그대로
    m.SetFloat("_A7Lit", baked ? 0f : 1f); if (!baked) { m.SetFloat("_R1Ok", 0f); m.SetFloat("_LightAmt", 0f); m.SetFloat("_Lift", 0f); }
  }
  AssetDatabase.CreateAsset(m, dp); map[src] = m; }
log.Append("mats " + map.Count + "\n");
Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA7_" + n + ".mat");
// ---- 텍스처 교체 (APPLY.md 표)
System.Action<string, Texture2D> Kit = (mn, tx) => { var m = M(mn); if (!m || !tx) { log.Append("NO " + mn + "/" + (tx ? tx.name : "tex") + "; "); return; }
  m.SetTexture("_BaseMap", tx); if (m.GetFloat("_HasR2") > 0.5f) m.SetTexture("_BaseMapR2", tx); m.SetFloat("_Sat", 1f); EditorUtility.SetDirty(m); log.Append(mn + "←" + tx.name + "; "); };
foreach (var n in new[] { "drum_01", "drum_02", "drum_03", "tires_01", "tires_02", "transformer_01", "pot_01", "pot_02", "bench_01", "bicycle_01", "cone_01", "cone_02", "streetlight_01" }) Kit(n, T(n));
foreach (var n in new[] { "01", "02", "03", "05" }) Kit("autumn_tree_" + n + "_soft", T("autumn_tree_" + n));
Kit("garage_baked32", T("garage_tin_F2_atlas")); Kit("kart_v6e", T("kart_v6e"));
foreach (var n in new[] { "fence_01", "fence_02", "pole_01" }) if (M(n)) Kit(n, T(n)); else log.Append("(씬에 재질 없음 " + n + ") ");
var sw = M("stonewall"); log.Append("\nstonewall old " + (sw.GetTexture("_BaseMap") ? sw.GetTexture("_BaseMap").name + " " + sw.GetTexture("_BaseMap").width + "x" + sw.GetTexture("_BaseMap").height : "null") + " ST " + sw.GetTextureScale("_BaseMap") + "; ");
Kit("stonewall", T("stone_H1"));
foreach (var kv in map) { var bt = kv.Value.HasProperty("_BaseMap") ? kv.Value.GetTexture("_BaseMap") : null; if (bt && bt.name.Contains("dirt")) { kv.Value.SetTexture("_BaseMap", T("dirt_baked32")); log.Append(kv.Value.name + "←dirt_baked32; "); } }
// 사진 키트 재질
var pkm = new System.Collections.Generic.Dictionary<string, Material>();
foreach (var n in PK) { var m = new Material(M("drum_01")) { name = "SA7_" + n }; m.SetTexture("_BaseMap", T(n)); m.SetFloat("_HasR2", 0f); m.SetFloat("_Sat", 1f); AssetDatabase.DeleteAsset(D + "Mats/SA7_" + n + ".mat"); AssetDatabase.CreateAsset(m, D + "Mats/SA7_" + n + ".mat"); pkm[n] = m; }
AssetDatabase.SaveAssets();
// ---- 씬
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S6); AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset(S6, S);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
int nr = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var ms = r.sharedMaterials; bool ch = false; for (int i = 0; i < ms.Length; i++) if (ms[i] && map.ContainsKey(ms[i])) { ms[i] = map[ms[i]]; ch = true; } if (ch) { r.sharedMaterials = ms; nr++; } }
var root = GameObject.Find("SceneA4").transform; var tg = root.GetComponent<SceneA3Toggles>();
var tl = new System.Collections.Generic.List<Material>(); foreach (var m in tg.materials) tl.Add(m && map.ContainsKey(m) ? map[m] : m); foreach (var m in pkm.Values) tl.Add(m); tg.materials = tl.ToArray();
log.Append("\nrenderers remapped " + nr + "\n");
// ---- 집: albedo fbx 로 다시 놓기(po1 빌드와 같은 규칙)
var g0 = root.Find("A4b_Houses");
var body = new System.Collections.Generic.Dictionary<string, float[]> {
  { "house_narrow_a", new[] { 5f, 6f, 9f, 0f } }, { "house_narrow_b", new[] { 4f, 5f, 9f, 0f } }, { "house_shop", new[] { 6f, 6f, 6f, 0f } },
  { "house_slope", new[] { 6f, 5f, 7.6f, 1.6f } }, { "house_warehouse", new[] { 8f, 7f, 6f, 0f } }, { "house_cafe", new[] { 6f, 4f, 3f, 0f } } };
var big = new System.Collections.Generic.Dictionary<string, Vector3> {
  { "bike", new Vector3(1f, 1f, 1f) }, { "chair", new Vector3(0.5f, 0.5f, 0.9f) }, { "table", new Vector3(0.9f, 0.9f, 0.75f) }, { "wood_pile", new Vector3(1.5f, 0.8f, 0.8f) },
  { "ladder", new Vector3(0.6f, 0.4f, 2f) }, { "sawhorse", new Vector3(1f, 0.5f, 0.8f) }, { "fish_display", new Vector3(1.4f, 0.7f, 1f) }, { "styro_stack", new Vector3(0.6f, 0.5f, 1f) },
  { "menu_board", new Vector3(0.6f, 0.4f, 1f) }, { "rain_barrel", new Vector3(0.6f, 0.6f, 0.9f) }, { "toolbox", new Vector3(0.6f, 0.4f, 0.5f) } };
var parts = MiniJson(System.IO.File.ReadAllText(ex + "scene-a4-houses-alb/parts.json"));
foreach (Transform t in g0) {
  var oldInst = t.GetChild(0); var m = M(t.name); m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "houses/" + t.name + ".png")); EditorUtility.SetDirty(m);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + t.name + ".fbx"), t); inst.transform.localPosition = oldInst.localPosition; inst.transform.localRotation = oldInst.localRotation; inst.transform.localScale = oldInst.localScale;
  UnityEngine.Object.DestroyImmediate(oldInst.gameObject);
  foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
  var bd = body[t.name]; float W = bd[0], Dp = bd[1], Hw = bd[2], zb = bd[3]; var mf = inst.GetComponentInChildren<MeshFilter>(); var mb = mf.sharedMesh.bounds;
  var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, t.name == "house_slope" ? mb.max.y - Dp * 0.5f : 0f, (zb + Hw) * 0.5f); bc.size = new Vector3(W, Dp, Hw - zb);
  if (zb > 0f) { var bc2 = mf.gameObject.AddComponent<BoxCollider>(); bc2.center = new Vector3(mb.center.x, mb.center.y, zb * 0.5f); bc2.size = new Vector3(mb.size.x, mb.size.y, zb); }
  var verts = mf.sharedMesh.vertices; var bodyB = new Bounds(bc.center, bc.size + Vector3.one * 0.05f); int np = 0, off = 0;
  foreach (var it in parts[t.name].life) { if (!big.ContainsKey(it.what)) continue; var s = big[it.what]; var pc = mf.gameObject.AddComponent<BoxCollider>(); pc.center = new Vector3(-it.x, it.y, zb + s.z * 0.5f); pc.size = s; np++;
    var pb = new Bounds(pc.center, s); int nv = 0; foreach (var v in verts) if (pb.Contains(v) && !bodyB.Contains(v)) nv++; if (nv < 8) { pc.enabled = false; off++; } }
  log.Append(t.name + " verts " + verts.Length + " props " + np + " off " + off + "; "); }
// ---- 사진 키트 배치: 집 앞 무리(가게 앞 자판기 2 + 우체통) / 정비소 옆 벽 밑(실외기 + 자판기 흰)
var pkRoot = new GameObject("A7_PhotoKit").transform; pkRoot.SetParent(root, false);
System.Action<string, Vector3, Vector3> Put = (n, pos, face) => { var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "photokit/" + n + ".fbx"), pkRoot);
  if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out var hit, 20f)) pos.y = hit.point.y; go.transform.position = pos; face.y = 0; go.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
  foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = pkm[n]; r.sharedMaterials = ms; }
  log.Append("\nput " + n + " " + pos.ToString("F1")); };
var cbS = root.Find("A4e_Curbs/Curb_house_shop"); var hS = g0.Find("house_shop");
Vector3 toStreet = (cbS.position - hS.position); toStreet.y = 0; toStreet.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, toStreet);
Vector3 front = Vector3.Lerp(hS.position, cbS.position, 0.6f);
Put("vending_G2", front + side * 2.2f, toStreet); Put("vending_G1", front + side * 3.5f, toStreet); Put("postbox_G1", front - side * 2.6f, toStreet);
var gb = new Bounds(); bool f0 = false; foreach (var r in root.Find("garage_v5_combo").GetComponentsInChildren<Renderer>()) { if (!f0) { gb = r.bounds; f0 = true; } else gb.Encapsulate(r.bounds); }
Vector3 gToH = g0.position - gb.center; gToH.y = 0; Vector3 gs = Mathf.Abs(gToH.x) > Mathf.Abs(gToH.z) ? new Vector3(0, 0, Mathf.Sign(gToH.z)) : new Vector3(Mathf.Sign(gToH.x), 0, 0);   // 집 쪽이 아닌 옆벽
Vector3 wallP = gb.center + Vector3.Scale(gs, gb.extents) + gs * 0.45f;
Put("ac_G3", wallP, gs); Put("vending_G1", wallP + Vector3.Cross(Vector3.up, gs) * 1.6f, gs);
log.Append("\ngarage bounds " + gb.center.ToString("F1") + " ext " + gb.extents.ToString("F1") + " side " + gs);
// ---- 빛 토글
var lt = root.GetComponent<A7LightToggles>(); if (!lt) lt = root.gameObject.AddComponent<A7LightToggles>(); lt.Apply();
RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
tg.Apply();
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
