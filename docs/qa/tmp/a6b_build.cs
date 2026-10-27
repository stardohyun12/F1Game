// 과제 1636 — Scene_A6b_Town = Scene_A6_Town 복사. 집 6채 → art/export/scene-a4-houses-i (EDC 기법: 단순 상자·한 색 벽·면 방향 명도만 텍스처에)
//  재질 = URP Unlit(빛·그림자 안 받음), 텍스처 Point·밉맵 끔·무압축. SceneA3Toggles 목록에서 옛 집 재질 뺌(Look 게이트 대상 아님)
//  배치·몸체 상자·축대·연석 = A6 그대로(a6_build 와 같은 식). 소품 상자: parts.json(g = e 와 동일, i 는 이걸 읽어 지음) life 좌표에 두되,
//  상자 안(몸체 상자 밖)에 메시 정점이 8개 미만이면 허공 → collider.enabled = false
const string D = "Assets/Art/SceneA6b/", S = "Assets/Scenes/Scene_A6b_Town.unity";
string root0 = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/"));
AssetDatabase.DeleteAsset("Assets/Art/SceneA6b"); AssetDatabase.CreateFolder("Assets/Art", "SceneA6b"); foreach (var f in new[] { "Mats", "houses" }) AssetDatabase.CreateFolder("Assets/Art/SceneA6b", f);
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(root0 + "scene-a4-houses-i/" + h + e, Application.dataPath + "/Art/SceneA6b/houses/" + h + e, true);
AssetDatabase.Refresh();
foreach (var h in H) {
  var ti = (TextureImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".png"); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
  var mi = (ModelImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.isReadable = true; mi.SaveAndReimport(); }
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A6_Town.unity", S);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var log = new System.Text.StringBuilder();
var tg = root.GetComponent<SceneA3Toggles>(); var mats = new System.Collections.Generic.List<Material>(tg.materials);
var unlit = Shader.Find("Universal Render Pipeline/Unlit");
var body = new System.Collections.Generic.Dictionary<string, float[]> {
  { "house_narrow_a", new[] { 5f, 6f, 9f, 0f } }, { "house_narrow_b", new[] { 4f, 5f, 9f, 0f } }, { "house_shop", new[] { 6f, 6f, 6f, 0f } },
  { "house_slope", new[] { 6f, 5f, 7.6f, 1.6f } }, { "house_warehouse", new[] { 8f, 7f, 6f, 0f } }, { "house_cafe", new[] { 6f, 4f, 3f, 0f } } };
var big = new System.Collections.Generic.Dictionary<string, Vector3> {
  { "bike", new Vector3(1f, 1f, 1f) }, { "chair", new Vector3(0.5f, 0.5f, 0.9f) }, { "table", new Vector3(0.9f, 0.9f, 0.75f) }, { "wood_pile", new Vector3(1.5f, 0.8f, 0.8f) },
  { "ladder", new Vector3(0.6f, 0.4f, 2f) }, { "sawhorse", new Vector3(1f, 0.5f, 0.8f) }, { "fish_display", new Vector3(1.4f, 0.7f, 1f) }, { "styro_stack", new Vector3(0.6f, 0.5f, 1f) },
  { "menu_board", new Vector3(0.6f, 0.4f, 1f) }, { "rain_barrel", new Vector3(0.6f, 0.6f, 0.9f) }, { "toolbox", new Vector3(0.6f, 0.4f, 0.5f) } };
var parts = MiniJson(System.IO.File.ReadAllText(root0 + "scene-a4-houses-g/parts.json"));
foreach (Transform t in g) {
  var oldInst = t.GetChild(0); var oldMat = oldInst.GetComponentInChildren<MeshRenderer>().sharedMaterial;
  var m = new Material(unlit) { name = "SA6b_" + t.name }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "houses/" + t.name + ".png")); m.SetColor("_BaseColor", Color.white); AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat");
  mats.Remove(oldMat);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + t.name + ".fbx"), t); inst.transform.localPosition = Vector3.zero;
  UnityEngine.Object.DestroyImmediate(oldInst.gameObject);
  foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; r.receiveShadows = false; }
  var mfs = inst.GetComponentsInChildren<MeshFilter>(); var mf = mfs[0]; var mb = mf.sharedMesh.bounds;
  var bd = body[t.name]; float W = bd[0], Dp = bd[1], Hw = bd[2], zb = bd[3];
  var bc = mf.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, t.name == "house_slope" ? mb.max.y - Dp * 0.5f : 0f, (zb + Hw) * 0.5f); bc.size = new Vector3(W, Dp, Hw - zb);
  if (zb > 0f) { var bc2 = mf.gameObject.AddComponent<BoxCollider>(); bc2.center = new Vector3(mb.center.x, mb.center.y, zb * 0.5f); bc2.size = new Vector3(mb.size.x, mb.size.y, zb); }
  // 정점(mf 로컬)
  var verts = new System.Collections.Generic.List<Vector3>(); foreach (var f2 in mfs) foreach (var v in f2.sharedMesh.vertices) verts.Add(mf.transform.InverseTransformPoint(f2.transform.TransformPoint(v)));
  var bodyB = new Bounds(bc.center, bc.size + Vector3.one * 0.05f);
  int np = 0, off = 0; var offNames = new System.Collections.Generic.List<string>();
  foreach (var it in parts[t.name].life) { if (!big.ContainsKey(it.what)) continue; var s = big[it.what]; var pc = mf.gameObject.AddComponent<BoxCollider>(); pc.center = new Vector3(it.x, it.y, zb + s.z * 0.5f); pc.size = s; np++;
    var pb = new Bounds(pc.center, s); int nv = 0; foreach (var v in verts) if (pb.Contains(v) && !bodyB.Contains(v)) nv++;
    if (nv < 8) { pc.enabled = false; off++; offNames.Add(it.what + "(" + nv + ")"); } }
  log.Append(t.name + " meshes " + mfs.Length + " verts " + verts.Count + " meshB " + mb.size.ToString("F1") + " props " + np + " off " + off + " [" + string.Join(",", offNames) + "]; "); }
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
