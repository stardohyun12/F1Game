// 과제 1726 — Scene_A6d_Town = Scene_A6c_Town 복사.
//  0 공유 재질을 안 건드리려고 토글 목록 재질 50개를 전부 Assets/Art/SceneA6d/Mats/SA6d_* 사본으로 바꿔 끼움(토글·노이즈가 사본에만 씀)
//  1 검정·회색이어야 할 물체 8종 → Tex/<n>_mono.png (a6d_mono.py: 어두운 무채 텍셀 = 숯색 R=G=B 44 ±3%)
//  2 카트 v6b: FBX 는 텍스처 경로만 다시 뽑은 것(APPLY.md "기존 UV 그대로") → 메시 일치를 확인하고 재질 _BaseMap 만 v6b png, _Sat 1
//  3 색 보정 S1 켬(밝은 쪽 롤오프)·S2·S3 끔 + A6dGrainToggles((a) 켬 0.04 / (b) 끔 0.03)
const string D = "Assets/Art/SceneA6d/", S = "Assets/Scenes/Scene_A6d_Town.unity";
foreach (var f in new[] { "Mats", "Tex", "kart" }) if (!AssetDatabase.IsValidFolder(D + f)) AssetDatabase.CreateFolder("Assets/Art/SceneA6d", f);
AssetDatabase.Refresh();
System.Action<string, TextureWrapMode> Tex = (p, wrap) => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.wrapMode = wrap; ti.SaveAndReimport(); };
var log = new System.Text.StringBuilder();
// 텍스처 설정은 원본 import 의 wrap 을 따름
string[] MONO = { "streetlight_01", "wire", "tires_01", "tires_02", "bicycle_01", "bench_01", "old_car_01", "transformer_01" };
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A6c_Town.unity", S);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4"); var tg = root.GetComponent<SceneA3Toggles>();
var map = new System.Collections.Generic.Dictionary<Material, Material>(); var list = new System.Collections.Generic.List<Material>();
foreach (var m in tg.materials) {
  if (!m) { list.Add(m); continue; }
  string nm = "SA6d_" + System.Text.RegularExpressions.Regex.Replace(m.name, "^SA[0-9a-z]+_", "");
  var c = new Material(m) { name = nm };
  foreach (var mn in MONO) if (m.name == "SA4_" + mn) {
    var srcT = m.GetTexture("_BaseMap"); var srcI = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(srcT));
    string tp = D + "Tex/" + mn + "_mono.png"; Tex(tp, srcI.wrapMode); c.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp)); log.Append("mono " + mn + " sat " + c.GetFloat("_Sat") + "; "); }
  if (m.name == "SA6_kart_v6") {
    Tex(D + "kart/kart_v6b.png", TextureWrapMode.Clamp); c.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "kart/kart_v6b.png")); c.SetFloat("_Sat", 1f); c.name = "SA6d_kart_v6b"; }
  AssetDatabase.CreateAsset(c, D + "Mats/" + c.name + ".mat"); map[m] = c; list.Add(c); }
int nr = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  var ms = r.sharedMaterials; bool ch = false; for (int i = 0; i < ms.Length; i++) if (ms[i] && map.TryGetValue(ms[i], out var c2)) { ms[i] = c2; ch = true; }
  if (ch) { r.sharedMaterials = ms; nr++; } }
tg.materials = list.ToArray(); tg.S1_highlightRolloff = true; tg.S2_oneLight = false; tg.S3_softBorders = false; tg.Apply();
var gt = root.GetComponent<A6dGrainToggles>() ?? root.AddComponent<A6dGrainToggles>(); gt.grainShader = AssetDatabase.LoadAssetAtPath<Shader>(D + "A6dGrain.shader");
log.Append("mats " + map.Count + " renderers swapped " + nr + "; ");
// 카트 v6b 메시가 v6 와 같은지(이름별 정점 수)
var mi = (ModelImporter)AssetImporter.GetAtPath(D + "kart/kart_v6b.fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.SaveAndReimport();
System.Func<string, string> Sig = p => { var sb = new System.Collections.Generic.List<string>(); foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p)) if (o is Mesh me) sb.Add(me.name + ":" + me.vertexCount); sb.Sort(); return string.Join(",", sb); };
string a = Sig("Assets/Art/SceneA6/kart/kart_v6.fbx"), b = Sig(D + "kart/kart_v6b.fbx");
log.Append("kart mesh same " + (a == b) + " (" + a.Split(',').Length + " meshes)" + (a == b ? "" : " v6 " + a + " | v6b " + b) + "; ");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();
