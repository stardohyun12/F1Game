// 과제 1525 — Scene_A5_Town = Scene_A4e_Town 복사. 한 그림체(D-175, docs/design/STYLE.md).
//  1 집 6채 → art/export/scene-a4-houses-g (fbx+png). 배치·몸체 상자 콜라이더는 A4e 방식(a4e_build·a4e_fixcol) 그대로 + 길 쪽 큰 소품 자리에 작은 상자(parts.json life x,y)
//  2 소품·자연 → art/export/scene-a2-kit-style (APPLY.md): 이 씬 전용 재질 사본 SA5_* 에 새 png, 게이트 끔(_HasR2·_R1Ok·_LightAmt·_R3Ok=0, _GradAmt·_FootDark·_Blotch=0, _Tint·_WB 흰색, _Sat 1, _Lift 0)
//    자연 tiling: bush·rock 0.6875, mound·clump 11 (APPLY.md 표). 공유 SA4_*.mat 은 안 건드림. 새 사본은 SceneA3Toggles 목록에 안 넣음(게이트가 꺼져 있어 영향 없음)
const string D = "Assets/Art/SceneA5/", S = "Assets/Scenes/Scene_A5_Town.unity";
string root0 = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/"));
AssetDatabase.DeleteAsset("Assets/Art/SceneA5"); AssetDatabase.CreateFolder("Assets/Art", "SceneA5"); foreach (var f in new[] { "Mats", "houses", "kit" }) AssetDatabase.CreateFolder("Assets/Art/SceneA5", f);
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
string[] K = { "bench_01", "bicycle_01", "cone_01", "cone_02", "drum_01", "drum_02", "drum_03", "pot_01", "pot_02", "tires_01", "tires_02", "fence_01", "fence_02", "pole_01" };
var NAT = new System.Collections.Generic.Dictionary<string, (string png, float tile)> { { "SA4_bush", ("bush_style", 0.6875f) }, { "SA4_rock", ("rock_style", 0.6875f) }, { "SA4_mound", ("mound_style", 11f) }, { "SA4_clump", ("mound_style", 11f) } };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(root0 + "scene-a4-houses-g/" + h + e, Application.dataPath + "/Art/SceneA5/houses/" + h + e, true);
foreach (var k in K) System.IO.File.Copy(root0 + "scene-a2-kit-style/" + k + ".png", Application.dataPath + "/Art/SceneA5/kit/" + k + ".png", true);
foreach (var n in new[] { "bush_style", "rock_style", "mound_style" }) System.IO.File.Copy(root0 + "scene-a2-kit-style/" + n + ".png", Application.dataPath + "/Art/SceneA5/kit/" + n + ".png", true);
AssetDatabase.Refresh();
System.Action<string, TextureWrapMode> Tex = (p, wrap) => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = wrap; ti.SaveAndReimport(); };
foreach (var h in H) { Tex(D + "houses/" + h + ".png", TextureWrapMode.Repeat); var mi = (ModelImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.SaveAndReimport(); }
foreach (var k in K) Tex(D + "kit/" + k + ".png", TextureWrapMode.Clamp);
foreach (var n in new[] { "bush_style", "rock_style", "mound_style" }) Tex(D + "kit/" + n + ".png", TextureWrapMode.Repeat);
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A4e_Town.unity", S);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var log = new System.Text.StringBuilder();
var tg = root.GetComponent<SceneA3Toggles>(); var mats = new System.Collections.Generic.List<Material>(tg.materials);
// ---- 1 집
var body = new System.Collections.Generic.Dictionary<string, float[]> {
  { "house_narrow_a", new[] { 5f, 6f, 9f, 0f } }, { "house_narrow_b", new[] { 4f, 5f, 9f, 0f } }, { "house_shop", new[] { 6f, 6f, 6f, 0f } },
  { "house_slope", new[] { 6f, 5f, 7.6f, 1.6f } }, { "house_warehouse", new[] { 8f, 7f, 6f, 0f } }, { "house_cafe", new[] { 6f, 4f, 3f, 0f } } };
var big = new System.Collections.Generic.Dictionary<string, Vector3> {   // 길 쪽 큰 소품 상자(가로·세로·높이 m). 화분·빨래·우편함·번지판·양동이는 작아서 뺌
  { "bike", new Vector3(1f, 1f, 1f) }, { "chair", new Vector3(0.5f, 0.5f, 0.9f) }, { "table", new Vector3(0.9f, 0.9f, 0.75f) }, { "wood_pile", new Vector3(1.5f, 0.8f, 0.8f) },
  { "ladder", new Vector3(0.6f, 0.4f, 2f) }, { "sawhorse", new Vector3(1f, 0.5f, 0.8f) }, { "fish_display", new Vector3(1.4f, 0.7f, 1f) }, { "styro_stack", new Vector3(0.6f, 0.5f, 1f) },
  { "menu_board", new Vector3(0.6f, 0.4f, 1f) }, { "rain_barrel", new Vector3(0.6f, 0.6f, 0.9f) }, { "toolbox", new Vector3(0.6f, 0.4f, 0.5f) } };
var parts = MiniJson(System.IO.File.ReadAllText(root0 + "scene-a4-houses-g/parts.json"));
foreach (Transform t in g) {
  var oldInst = t.GetChild(0); var oldMat = oldInst.GetComponentInChildren<MeshRenderer>().sharedMaterial;
  var m = new Material(oldMat) { name = "SA5_" + t.name }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "houses/" + t.name + ".png")); AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat");
  int ix = mats.IndexOf(oldMat); if (ix >= 0) mats[ix] = m; else mats.Add(m);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + t.name + ".fbx"), t); inst.transform.localPosition = Vector3.zero;
  UnityEngine.Object.DestroyImmediate(oldInst.gameObject);
  foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
  var bd = body[t.name]; float W = bd[0], Dp = bd[1], Hw = bd[2], zb = bd[3]; var mf = inst.GetComponentInChildren<MeshFilter>(); var mb = mf.sharedMesh.bounds;
  var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, t.name == "house_slope" ? mb.max.y - Dp * 0.5f : 0f, (zb + Hw) * 0.5f); bc.size = new Vector3(W, Dp, Hw - zb);
  if (zb > 0f) { var bc2 = mf.gameObject.AddComponent<BoxCollider>(); bc2.center = new Vector3(mb.center.x, mb.center.y, zb * 0.5f); bc2.size = new Vector3(mb.size.x, mb.size.y, zb); }
  int np = 0; foreach (var it in parts[t.name].life) { if (!big.ContainsKey(it.what)) continue; var s = big[it.what]; var pc = mf.gameObject.AddComponent<BoxCollider>(); pc.center = new Vector3(it.x, it.y, zb + s.z * 0.5f); pc.size = s; np++; }
  log.Append(t.name + " props " + np + "; "); }
// ---- 2 소품·자연 재질 사본
var map = new System.Collections.Generic.Dictionary<Material, Material>(); var miss = new System.Collections.Generic.List<string>(K);
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  var ms = r.sharedMaterials; bool ch = false;
  for (int i = 0; i < ms.Length; i++) { var om = ms[i]; if (!om) continue;
    if (!map.ContainsKey(om)) { string png = null; float tile = 1f;
      if (NAT.ContainsKey(om.name)) { png = NAT[om.name].png; tile = NAT[om.name].tile; } else if (om.name.StartsWith("SA4_") && System.Array.IndexOf(K, om.name.Substring(4)) >= 0) png = om.name.Substring(4);
      if (png == null) continue;
      var nm = new Material(om) { name = "SA5_" + om.name.Substring(4) }; nm.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "kit/" + png + ".png")); nm.SetTextureScale("_BaseMap", Vector2.one * tile); nm.SetTextureOffset("_BaseMap", Vector2.zero);
      foreach (var p in new[] { "_HasR2", "_R1Ok", "_LightAmt", "_R3Ok", "_GradAmt", "_FootDark", "_Blotch", "_Lift" }) if (nm.HasProperty(p)) nm.SetFloat(p, 0f);
      foreach (var p in new[] { "_Tint", "_WB" }) if (nm.HasProperty(p)) nm.SetColor(p, Color.white); if (nm.HasProperty("_Sat")) nm.SetFloat("_Sat", 1f);
      AssetDatabase.CreateAsset(nm, D + "Mats/" + nm.name + ".mat"); map[om] = nm; miss.Remove(png); mats.Remove(om); }
    ms[i] = map[om]; ch = true; }
  if (ch) r.sharedMaterials = ms; }
tg.materials = mats.ToArray(); tg.Apply();
log.Append("mats " + map.Count + " notInScene " + string.Join(",", miss));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();

// parts.json 에서 집마다 life[{what,x,y}] 만 뽑는 작은 파서 (JsonUtility 는 dict 를 못 읽음)
System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<(string what, float x, float y)> life, int _)> MiniJson(string js) {
  var res = new System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<(string what, float x, float y)> life, int _)>();
  foreach (var h in H) { var lst = new System.Collections.Generic.List<(string, float, float)>(); int a = js.IndexOf("\"" + h + "\""); int nx = js.Length; foreach (var h2 in H) { int b = js.IndexOf("\"" + h2 + "\""); if (b > a && b < nx) nx = b; }
    int l = js.IndexOf("\"life\"", a); if (l >= 0 && l < nx) { int e = js.IndexOf(']', l); var seg = js.Substring(l, e - l);
      foreach (System.Text.RegularExpressions.Match mm in System.Text.RegularExpressions.Regex.Matches(seg, "\\{\\s*\"what\"\\s*:\\s*\"(\\w+)\"\\s*,\\s*\"x\"\\s*:\\s*(-?[\\d.]+)\\s*,\\s*\"y\"\\s*:\\s*(-?[\\d.]+)"))
        lst.Add((mm.Groups[1].Value, float.Parse(mm.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(mm.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture))); }
    res[h] = (lst, 0); }
  return res; }
