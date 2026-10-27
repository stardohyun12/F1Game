// 과제 1046 — Scene_A2d_Town = A2c 복사. 배치·카메라 그대로, 색·빛·안개만: 재질을 SA2d_ 복사본으로 바꿔 안개 하늘빛·옅게, 채도·화이트밸런스·그늘 들어올림, 길·담 따뜻하게, 하늘 새 텍스처(구름).
// 값은 아래 상수만 고쳐 다시 돌린다(가설 값). 다시 돌리면 A2c 에서 새로 복사한다.
const string D = "Assets/Art/SceneA2/", S = "Assets/Scenes/Scene_A2d_Town.unity";
float SAT = 2.6f, LIFT = 0.16f; Color WB = new Color(1.04f, 0.97f, 0.86f), LIFTC = new Color(0.46f, 0.58f, 0.62f);
Color FOG = new Color(0.80f, 0.87f, 0.93f), HOR = new Color(0.95f, 0.886f, 0.73f);
float FS = 40, FE = 220, FM = 0.40f, FFS = 30, FFE = 120, FFM = 0.55f;   // 일반 · 원경 마을(farfog)
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A2c_Town.unity", S);
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
System.IO.Directory.CreateDirectory(D + "MatsD"); var log = new System.Text.StringBuilder();
{ var p = D + "Gen/sky_a2d.png"; AssetDatabase.ImportAsset(p); var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapMode = TextureWrapMode.Clamp; ti.wrapModeU = TextureWrapMode.Repeat; ti.SaveAndReimport(); }
var map = new System.Collections.Generic.Dictionary<Material, Material>();
System.Func<Material, Material> Conv = src => {
  if (src == null || !src.name.StartsWith("SA2c_")) return src; if (map.TryGetValue(src, out var mm)) return mm;
  var m = new Material(src) { name = "SA2d_" + src.name.Substring(5) }; string n = m.name;
  bool sky = n.Contains("sky_grad"), nofog = src.GetFloat("_FogMax") < .01f, far = n.EndsWith("_farfog");
  if (sky) { m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "Gen/sky_a2d.png")); m.SetFloat("_Sat", 1.0f); }
  else {
    m.SetFloat("_Sat", SAT); m.SetColor("_WB", WB); m.SetFloat("_Lift", LIFT); m.SetColor("_LiftTint", LIFTC);
    m.SetColor("_FogColor", FOG);
    if (far) { m.SetFloat("_FogStart", FFS); m.SetFloat("_FogEnd", FFE); m.SetFloat("_FogMax", FFM); }
    else if (!nofog) { m.SetFloat("_FogStart", FS); m.SetFloat("_FogEnd", FE); m.SetFloat("_FogMax", n.Contains("sea") ? .22f : FM); }
    if (n.Contains("asphalt")) { m.SetColor("_Tint", new Color(1.17f, 1.03f, 0.86f)); m.SetFloat("_Sat", 1.0f); }          // 청회색 → 따뜻한 회색 아스팔트
    if (n.Contains("wall_tile")) { m.SetColor("_Tint", new Color(1.14f, 1.08f, 1.0f)); m.SetFloat("_Sat", 0.6f); }        // 바랜 석판
  }
  AssetDatabase.CreateAsset(m, D + "MatsD/" + n + ".mat"); map[src] = m; return m; };
int rn = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{ var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Conv(ms[i]); r.sharedMaterials = ms; rn++; }
// 하늘 반구: u = 방위(구름이 돌아가게), v = 고도
var skyGo = GameObject.Find("Sky"); if (skyGo != null) {
  int nx = 33, ny = 12; var v = new Vector3[nx * ny]; var uv = new Vector2[nx * ny]; var t = new int[(nx - 1) * (ny - 1) * 6]; int k = 0;
  for (int j = 0; j < ny; j++) for (int i = 0; i < nx; i++) { float a = i / 32f * Mathf.PI * 2, e = Mathf.Lerp(-0.25f, Mathf.PI / 2, j / 11f);
    var b = new Vector3(Mathf.Cos(a) * Mathf.Cos(e) * 1500, Mathf.Sin(a) * Mathf.Cos(e) * 1500, Mathf.Sin(e) * 1500); v[j * nx + i] = new Vector3(-b.x, b.z, -b.y); uv[j * nx + i] = new Vector2(i / 32f * 2f, Mathf.Clamp01(e / (Mathf.PI / 2))); }
  for (int j = 0; j < ny - 1; j++) for (int i = 0; i < nx - 1; i++) { int a = j * nx + i; t[k++] = a; t[k++] = a + nx; t[k++] = a + 1; t[k++] = a + 1; t[k++] = a + nx; t[k++] = a + nx + 1; }
  var sm = new Mesh { name = "d_sky", vertices = v, uv = uv, triangles = t }; sm.RecalculateNormals(); sm.RecalculateBounds();
  AssetDatabase.DeleteAsset(D + "MatsD/d_sky.asset"); AssetDatabase.CreateAsset(sm, D + "MatsD/d_sky.asset"); skyGo.GetComponent<MeshFilter>().sharedMesh = sm; }
var rootGo = GameObject.Find("SceneA2c"); if (rootGo) rootGo.name = "SceneA2d";
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.backgroundColor = HOR;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
return $"renderers {rn} · 재질 {map.Count} · sky {(skyGo != null)} · SAT {SAT} LIFT {LIFT} fog {FS}-{FE}x{FM}";
