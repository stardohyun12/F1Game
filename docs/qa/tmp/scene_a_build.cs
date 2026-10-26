// 과제 0429 — Scene_A_Garage 만들기. 다시 돌리면 씬을 새로 만든다(재질은 덮어씀).
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
const string D = "Assets/Art/SceneA/"; System.IO.Directory.CreateDirectory(D + "Mats");
var look = Shader.Find("PitCrew/SceneA/Look"); var lit = Shader.Find("Universal Render Pipeline/Lit");
var log = new System.Text.StringBuilder();
// 판별 값 (가설 — 보고서에 적음). [안개색, 주변광 배율, 해 밝기, 빛 양, 계단, 안개 시작·끝·최대, 그라데이션, 발밑, 얼룩]
Color fogV1 = new Color(0.93f, 0.88f, 0.78f), fogV2 = new Color(0.80f, 0.84f, 0.82f);
var P = new System.Collections.Generic.Dictionary<int, float[]> {
  { 1, new float[] { 0, .70f, 1.08f, 1f, 0, 6, 45, .60f, 0, 0, 0 } },
  { 2, new float[] { 1, .72f, 1.04f, .35f, 3, 12, 70, .55f, 0, 0, 0 } },
  { 3, new float[] { 0, .70f, 1.08f, 1f, 0, 6, 45, .60f, .15f, .35f, .18f } } };
Vector4 sunDir = new Vector4(-0.5f, 0.75f, 0.45f, 0);
var cache = new System.Collections.Generic.Dictionary<string, Material>();
System.Func<int, Material, Material> Conv = (v, src) => {
  string key = v + "_" + src.name; if (cache.TryGetValue(key, out var mm)) return mm;
  var p = P[v]; var m = new Material(look) { name = "SA_V" + v + "_" + src.name };
  m.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
  Color fog = p[0] > 0 ? fogV2 : fogV1;
  m.SetColor("_FogColor", fog); m.SetColor("_Ambient", fog * p[1]); m.SetFloat("_Sun", p[2]); m.SetFloat("_LightAmt", p[3]); m.SetFloat("_Steps", p[4]);
  m.SetFloat("_FogStart", p[5]); m.SetFloat("_FogEnd", p[6]); m.SetFloat("_FogMax", p[7]); m.SetVector("_SunDir", sunDir);
  m.SetFloat("_GradAmt", p[8]); m.SetFloat("_FootDark", p[9]); m.SetFloat("_Blotch", p[10]);
  AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); cache[key] = m; return m; };
System.Func<Material, Material> Flat = src => {   // V0 땅: v5 에는 env 가 없다 → V1 땅 모양에 텍스처 평균색 한 색(v5 식 단색 면)
  string key = "flat_" + src.name; if (cache.TryGetValue(key, out var mm)) return mm;
  var t = src.GetTexture("_BaseMap"); var c = Color.gray;
  if (t != null) { var tx = new Texture2D(2, 2); tx.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(t))); var px = tx.GetPixels(); c = Color.black; foreach (var q in px) c += q; c /= px.Length; c.a = 1; }
  var m = new Material(lit) { name = "SA_V0_flat_" + src.name }; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0);
  AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); cache[key] = m; return m; };
var sw = new GameObject("SceneA").AddComponent<SceneAVariant>();
Vector3 kartPos = new Vector3(-0.7f, 0, 8.3f); var kartRot = Quaternion.Euler(0, 10, 0); // Blender (0.7,-8.3) −10° → Unity (−x, z, −y): 정비소 열린 앞 = +z
for (int v = 0; v < 4; v++)
{
  var root = new GameObject("V" + v); root.transform.SetParent(sw.transform, false); sw.roots[v] = root;
  string sv = v == 0 ? "V0" : v == 2 ? "V2" : "V1", sf = v == 0 ? "v5" : v == 2 ? "v2" : "v1";
  foreach (var part in new[] { "garage", "garage_props", "env", "kart" })
  {
    string path = part == "env" && v == 0 ? D + "V1/env_v1.fbx" : D + sv + "/" + part + "_" + sf + ".fbx";
    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
    if (part == "kart") { go.transform.localPosition = kartPos; go.transform.localRotation = kartRot; }
    foreach (var r in go.GetComponentsInChildren<Renderer>())
    {
      r.shadowCastingMode = v == 0 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = v == 0;
      if (v == 0 && part != "env") continue;
      var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) if (ms[i] != null) ms[i] = v == 0 ? Flat(ms[i]) : Conv(v, ms[i]); r.sharedMaterials = ms;
    }
  }
}
// 해 (V0 만)
var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.intensity = 1.2f;
sun.transform.rotation = Quaternion.LookRotation(-(Vector3)sunDir); sw.sun = sun;
RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f, .65f, .66f); RenderSettings.fog = false;
sw.sky = new[] { new Color(208 / 255f, 218 / 255f, 208 / 255f), fogV1, fogV2, fogV1 };
// 카메라 2개: 게임 컷(카트 뒤 높이 2.8 m·거리 5.6 m·FOV 64), 소개 컷(정비소 앞 3/4)
Vector3 fwd = kartRot * Vector3.back;   // 카메라는 정비소 반대편(카트 뒤)에서 정비소 쪽(−z)을 본다 (가설)
for (int c = 0; c < 2; c++)
{
  var cg = new GameObject(c == 0 ? "Cam_Game" : "Cam_Intro"); var cam = cg.AddComponent<Camera>(); cg.tag = "MainCamera";
  cam.clearFlags = CameraClearFlags.SolidColor; cam.nearClipPlane = 0.1f; cam.farClipPlane = 600;
  if (c == 0) { cam.fieldOfView = 64; cg.transform.position = kartPos - fwd * 5.6f + Vector3.up * 2.8f; cg.transform.LookAt(kartPos + fwd * 6f + Vector3.up * 1.0f); }
  else { cam.fieldOfView = 50; cg.transform.position = new Vector3(-9.5f, 3.4f, 18.5f); cg.transform.LookAt(new Vector3(0.5f, 2.0f, 2f)); }
  var lr = cg.AddComponent<LowResRenderTest>(); lr.targetHeight = 720; lr.integerScale = false; lr.smooth = false;
  sw.cams[c] = cam; log.Append($"{cg.name} pos={cg.transform.position:F2} rot={cg.transform.eulerAngles:F1}\n");
}
sw.variant = 0; sw.cut = 0; sw.Apply();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_A_Garage.unity");
AssetDatabase.SaveAssets();
return log + "mats " + cache.Count;
