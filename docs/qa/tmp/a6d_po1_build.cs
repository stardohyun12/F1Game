// 과제 1916 — a6d_fix1_build 의 A(집)·B(카트) 를 j3·v6e 로 + 소품·나무 po1 png. 돌담(C)·소품판정(D)은 안 돌린다.
const string D = "Assets/Art/SceneA6d/", S = "Assets/Scenes/Scene_A6d_Town.unity";
string root0 = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/"));
foreach (var f in new[] { "houses", "kart", "Tex" }) if (!AssetDatabase.IsValidFolder(D + f)) AssetDatabase.CreateFolder("Assets/Art/SceneA6d", f);
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(root0 + "scene-a4-houses-j3/" + h + e, Application.dataPath + "/Art/SceneA6d/houses/" + h + e, true);
foreach (var n in new[] { "kart_v6e.fbx", "kart_v6e.png" }) System.IO.File.Copy(root0 + "kart-v6e/" + n, Application.dataPath + "/Art/SceneA6d/kart/" + n, true);
AssetDatabase.Refresh();
System.Action<string, TextureWrapMode> Tex = (p, wrap) => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = wrap; ti.SaveAndReimport(); };
var oldKI = (ModelImporter)AssetImporter.GetAtPath("Assets/Art/SceneA6/kart/kart_v6.fbx");
System.Action<string, bool> Fbx = (p, kart) => { var mi = (ModelImporter)AssetImporter.GetAtPath(p); mi.materialImportMode = ModelImporterMaterialImportMode.None; if (!kart) mi.isReadable = true; else if (oldKI != null) { mi.globalScale = oldKI.globalScale; mi.useFileScale = oldKI.useFileScale; mi.bakeAxisConversion = oldKI.bakeAxisConversion; } mi.SaveAndReimport(); };
foreach (var h in H) { Tex(D + "houses/" + h + ".png", TextureWrapMode.Repeat); Fbx(D + "houses/" + h + ".fbx", false); }
Tex(D + "kart/kart_v6e.png", TextureWrapMode.Clamp); Fbx(D + "kart/kart_v6e.fbx", true);
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
var parts = MiniJson(System.IO.File.ReadAllText(root0 + "scene-a4-houses-j3/parts.json"));
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
var oldKM = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA6d_kart_v6d.mat");
var km = new Material(oldKM) { name = "SA6d_kart_v6e" }; km.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "kart/kart_v6e.png"));
AssetDatabase.DeleteAsset(D + "Mats/SA6d_kart_v6e.mat"); AssetDatabase.CreateAsset(km, D + "Mats/SA6d_kart_v6e.mat");
int ixo = mats.IndexOf(oldKM); if (ixo >= 0) mats[ixo] = km; else mats.Add(km);
var kids = new System.Collections.Generic.List<GameObject>(); foreach (Transform c in kv) kids.Add(c.gameObject); foreach (var c in kids) UnityEngine.Object.DestroyImmediate(c);
var kinst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "kart/kart_v6e.fbx"), kv); PrefabUtility.UnpackPrefabInstance(kinst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
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
// ---- 소품·나무 po1 png (scene-a2-kit-po1). 임포트 설정은 지금 그 슬롯에 꽂힌 텍스처에서 그대로 복사(알파·랩 유지)
var kitLog = new System.Text.StringBuilder();
System.Action<string, string[], float> Kit = (nm, slots, sat) => {
  var km0 = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA6d_" + nm + (nm.StartsWith("autumn") ? "_soft" : "") + ".mat"); if (!km0) { kitLog.Append("NO mat " + nm + "\n"); return; }
  string dp = D + "Tex/" + nm + "_po1.png"; System.IO.File.Copy(root0 + "scene-a2-kit-po1/" + nm + ".png", Application.dataPath + "/Art/SceneA6d/Tex/" + nm + "_po1.png", true); AssetDatabase.ImportAsset(dp);
  var oldTex = km0.GetTexture(slots[slots.Length - 1]); var sp = AssetDatabase.GetAssetPath(oldTex); var si = (TextureImporter)AssetImporter.GetAtPath(sp); var di = (TextureImporter)AssetImporter.GetAtPath(dp);
  var ts = new TextureImporterSettings(); si.ReadTextureSettings(ts); di.SetTextureSettings(ts); di.textureCompression = si.textureCompression; di.maxTextureSize = si.maxTextureSize; di.SaveAndReimport();
  var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(dp); kitLog.Append(km0.name + " " + nt.width + "x" + nt.height + " ");
  foreach (var s in slots) { kitLog.Append(s + ":" + (km0.GetTexture(s) ? km0.GetTexture(s).name : "null") + "→" + nt.name + " "); km0.SetTexture(s, nt); }
  if (sat >= 0f) { kitLog.Append("_Sat " + km0.GetFloat("_Sat") + "→" + sat + " "); km0.SetFloat("_Sat", sat); }
  kitLog.Append("filter " + di.filterMode + " mip " + di.mipmapEnabled + " wrap " + di.wrapMode + " alpha " + di.alphaIsTransparency + "\n"); EditorUtility.SetDirty(km0); };
Kit("transformer_01", new[] { "_BaseMap" }, 1.0f); Kit("tires_01", new[] { "_BaseMap" }, 1.0f); Kit("tires_02", new[] { "_BaseMap" }, 1.0f);
foreach (var n in new[] { "01", "02", "03", "05" }) Kit("autumn_tree_" + n, new[] { "_BaseMap", "_BaseMapR2" }, -1f);
log.Append(kitLog);
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