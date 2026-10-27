// 과제 1058 — Scene_A2e_Town = A2d 복사. 배치·카메라 그대로. 렌더링은 ⑥(720 Nearest · 그림자 끔 · unlit=빛 구움 · 주변광=안개색 · 거리 안개) 유지,
// 질감·색은 ①: 텍스처를 ① 팔레트 32색 양자화본(GenE/, a2e_tex.py)으로, 색 보정은 약하게(팔레트가 이미 채도 있음), 그늘은 청보라 쪽으로 들어올림. 값 = 가설.
const string D = "Assets/Art/SceneA2/", S = "Assets/Scenes/Scene_A2e_Town.unity";
float SAT = 1.15f, LIFT = 0.18f; Color WB = new Color(1.03f, 1.0f, 0.95f), LIFTC = new Color(0.52f, 0.54f, 0.78f);
Color FOG = new Color(0.78f, 0.88f, 0.96f), HOR = new Color(0.86f, 0.92f, 0.96f);
float FS = 45, FE = 240, FM = 0.35f, FFS = 30, FFE = 120, FFM = 0.45f;
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A2d_Town.unity", S);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
System.IO.Directory.CreateDirectory(D + "MatsE"); AssetDatabase.Refresh();
var log = new System.Text.StringBuilder(); int texN = 0;
var map = new System.Collections.Generic.Dictionary<Material, Material>();
System.Func<Material, Material> Conv = src => {
  if (src == null || !src.name.StartsWith("SA2d_")) return src; if (map.TryGetValue(src, out var mm)) return mm;
  var m = new Material(src) { name = "SA2e_" + src.name.Substring(5) }; string n = m.name;
  var t = src.GetTexture("_BaseMap"); string tp = t ? AssetDatabase.GetAssetPath(t) : "";
  if (tp.StartsWith(D) && !n.Contains("sky")) {
    string ep = D + "GenE/" + tp.Substring(D.Length);
    if (System.IO.File.Exists(ep)) {
      var ti = (TextureImporter)AssetImporter.GetAtPath(ep); var si = (TextureImporter)AssetImporter.GetAtPath(tp);
      if (ti != null) { ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192; ti.alphaIsTransparency = true; ti.wrapMode = si != null ? si.wrapMode : TextureWrapMode.Repeat; ti.SaveAndReimport(); }
      m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ep)); texN++; } }
  if (n.Contains("sky")) m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "Gen/sky_a2e.png"));
  else {
    m.SetFloat("_Sat", SAT); m.SetColor("_WB", WB); m.SetFloat("_Lift", LIFT); m.SetColor("_LiftTint", LIFTC); m.SetColor("_FogColor", FOG); m.SetColor("_Tint", Color.white);
    if (n.EndsWith("_farfog")) { m.SetFloat("_FogStart", FFS); m.SetFloat("_FogEnd", FFE); m.SetFloat("_FogMax", FFM); }
    else if (src.GetFloat("_FogMax") > .01f) { m.SetFloat("_FogStart", FS); m.SetFloat("_FogEnd", FE); m.SetFloat("_FogMax", n.Contains("sea") ? .2f : FM); }
    if (n.Contains("asphalt")) m.SetColor("_Tint", new Color(1.14f, 1.06f, 0.90f));
  }
  AssetDatabase.CreateAsset(m, D + "MatsE/" + n + ".mat"); map[src] = m; return m; };
{ var p = D + "Gen/sky_a2e.png"; AssetDatabase.ImportAsset(p); var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
int rn = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{ var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Conv(ms[i]); r.sharedMaterials = ms; rn++; }
var rootGo = GameObject.Find("SceneA2d"); if (rootGo) rootGo.name = "SceneA2e";
RenderSettings.fog = false; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = FOG;   // 주변광 = 안개색(⑥). 이 재질은 unlit(_LightAmt 0)이라 화면엔 영향 없음
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.backgroundColor = HOR;
int shadows = 0; foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (l.shadows != LightShadows.None) shadows++;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
return $"renderers {rn} · 재질 {map.Count} · ① 팔레트 텍스처 {texN} · 그림자 켠 빛 {shadows} · SAT {SAT} LIFT {LIFT}";
