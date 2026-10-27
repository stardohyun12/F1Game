// 과제 1704 — Scene_A6c_Town = Scene_A6_Town 복사. 집 6채 → art/export/scene-a4-houses-j (fbx+png). 카트 v6·환경은 A6 그대로.
//  재질 = 정비소 재질 SA4b_garage_baked32 를 그대로 복사(new Material(garage)) + _BaseMap 만 j png. (탐색 a6c_probe: A6 h2 집 재질도 이미 정비소와 모든 값이 같았음 — Look·_Ramp1·_R3Ok1·게이트 0)
//  배치·몸체 상자·축대·연석·소품 상자 = A6 와 같은 식(j parts.json = g/h2 와 동일). 소품 상자는 메시 정점이 안에 8개 미만이면 끔(A6b 식)
const string D = "Assets/Art/SceneA6c/", S = "Assets/Scenes/Scene_A6c_Town.unity";
string root0 = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/"));
AssetDatabase.DeleteAsset("Assets/Art/SceneA6c"); AssetDatabase.CreateFolder("Assets/Art", "SceneA6c"); foreach (var f in new[] { "Mats", "houses" }) AssetDatabase.CreateFolder("Assets/Art/SceneA6c", f);
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(root0 + "scene-a4-houses-j/" + h + e, Application.dataPath + "/Art/SceneA6c/houses/" + h + e, true);
AssetDatabase.Refresh();
System.Action<string, TextureWrapMode> Tex = (p, wrap) => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = wrap; ti.SaveAndReimport(); };
System.Action<string> Fbx = p => { var mi = (ModelImporter)AssetImporter.GetAtPath(p); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.isReadable = true; mi.SaveAndReimport(); };
foreach (var h in H) { Tex(D + "houses/" + h + ".png", TextureWrapMode.Repeat); Fbx(D + "houses/" + h + ".fbx"); }
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A6_Town.unity", S);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var log = new System.Text.StringBuilder();
var tg = root.GetComponent<SceneA3Toggles>(); var mats = new System.Collections.Generic.List<Material>(tg.materials);
// ---- 1 집 (a5_build 와 같음)
var body = new System.Collections.Generic.Dictionary<string, float[]> {
  { "house_narrow_a", new[] { 5f, 6f, 9f, 0f } }, { "house_narrow_b", new[] { 4f, 5f, 9f, 0f } }, { "house_shop", new[] { 6f, 6f, 6f, 0f } },
  { "house_slope", new[] { 6f, 5f, 7.6f, 1.6f } }, { "house_warehouse", new[] { 8f, 7f, 6f, 0f } }, { "house_cafe", new[] { 6f, 4f, 3f, 0f } } };
var big = new System.Collections.Generic.Dictionary<string, Vector3> {   // 길 쪽 큰 소품 상자(가로·세로·높이 m). 화분·빨래·우편함·번지판·양동이는 작아서 뺌
  { "bike", new Vector3(1f, 1f, 1f) }, { "chair", new Vector3(0.5f, 0.5f, 0.9f) }, { "table", new Vector3(0.9f, 0.9f, 0.75f) }, { "wood_pile", new Vector3(1.5f, 0.8f, 0.8f) },
  { "ladder", new Vector3(0.6f, 0.4f, 2f) }, { "sawhorse", new Vector3(1f, 0.5f, 0.8f) }, { "fish_display", new Vector3(1.4f, 0.7f, 1f) }, { "styro_stack", new Vector3(0.6f, 0.5f, 1f) },
  { "menu_board", new Vector3(0.6f, 0.4f, 1f) }, { "rain_barrel", new Vector3(0.6f, 0.6f, 0.9f) }, { "toolbox", new Vector3(0.6f, 0.4f, 0.5f) } };
var garageMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA4b/Mats/SA4b_garage_baked32.mat");
var parts = MiniJson(System.IO.File.ReadAllText(root0 + "scene-a4-houses-j/parts.json"));
foreach (Transform t in g) {
  var oldInst = t.GetChild(0); var oldMat = oldInst.GetComponentInChildren<MeshRenderer>().sharedMaterial;
  var m = new Material(garageMat) { name = "SA6c_" + t.name }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "houses/" + t.name + ".png")); AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat");
  int ix = mats.IndexOf(oldMat); if (ix >= 0) mats[ix] = m; else mats.Add(m);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + t.name + ".fbx"), t); inst.transform.localPosition = Vector3.zero;
  UnityEngine.Object.DestroyImmediate(oldInst.gameObject);
  foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
  var bd = body[t.name]; float W = bd[0], Dp = bd[1], Hw = bd[2], zb = bd[3]; var mf = inst.GetComponentInChildren<MeshFilter>(); var mb = mf.sharedMesh.bounds;
  var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, t.name == "house_slope" ? mb.max.y - Dp * 0.5f : 0f, (zb + Hw) * 0.5f); bc.size = new Vector3(W, Dp, Hw - zb);
  if (zb > 0f) { var bc2 = mf.gameObject.AddComponent<BoxCollider>(); bc2.center = new Vector3(mb.center.x, mb.center.y, zb * 0.5f); bc2.size = new Vector3(mb.size.x, mb.size.y, zb); }
  var verts = new System.Collections.Generic.List<Vector3>(); foreach (var v in mf.sharedMesh.vertices) verts.Add(v); var bodyB = new Bounds(bc.center, bc.size + Vector3.one * 0.05f);
  int np = 0, off = 0; var offN = new System.Collections.Generic.List<string>();
  // 임포트 메시 로컬은 Blender x 가 뒤집혀 있음(a6c_propcheck 실측: 소품 정점이 life x 의 반대편) — A5·A6 는 +x 로 넣어 좌우가 거울이었음
  foreach (var it in parts[t.name].life) { if (!big.ContainsKey(it.what)) continue; var s = big[it.what]; var pc = mf.gameObject.AddComponent<BoxCollider>(); pc.center = new Vector3(-it.x, it.y, zb + s.z * 0.5f); pc.size = s; np++;
    var pb = new Bounds(pc.center, s); int nv = 0; foreach (var v in verts) if (pb.Contains(v) && !bodyB.Contains(v)) nv++; if (nv < 8) { pc.enabled = false; off++; offN.Add(it.what + "(" + nv + ")"); } }
  log.Append(t.name + " verts " + verts.Count + " meshB " + mb.size.ToString("F1") + " props " + np + " off " + off + " [" + string.Join(",", offN) + "]; "); }
tg.materials = mats.ToArray(); tg.Apply();
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
