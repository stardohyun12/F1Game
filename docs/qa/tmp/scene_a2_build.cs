// 과제 0546 — Scene_A2_Town 만들기(구성안 docs/design/SCENE-A2.md + layout.json). 다시 돌리면 씬을 새로 만든다.
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
const string D = "Assets/Art/SceneA2/"; System.IO.Directory.CreateDirectory(D + "Mats"); System.IO.Directory.CreateDirectory(D + "Gen");
var look = Shader.Find("PitCrew/SceneA/Look"); var log = new System.Text.StringBuilder();
Color fogC = new Color(.86f, .85f, .80f);           // 구성안: 안개색 = 하늘 지평선색
var mats = new System.Collections.Generic.Dictionary<string, Material>();
System.Func<string, float, Material> Mat = (png, fogMax) => {
  string key = png + fogMax; if (mats.TryGetValue(key, out var mm)) return mm;
  var m = new Material(look) { name = "SA2_" + System.IO.Path.GetFileNameWithoutExtension(png) + (fogMax < .01f ? "_nofog" : "") };
  m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + png)); m.SetFloat("_LightAmt", 0); m.SetFloat("_Cutoff", 0.5f);
  m.SetColor("_FogColor", fogC); m.SetFloat("_FogStart", 12); m.SetFloat("_FogEnd", 140); m.SetFloat("_FogMax", fogMax);
  AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); mats[key] = m; return m; };
System.Func<float, float, float, Vector3> U = (x, y, z) => new Vector3(-x, z, -y);   // Blender → Unity
var root = new GameObject("SceneA2").transform;
System.Func<string, Transform, GameObject> Put = (path, parent) => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + path), parent);
System.Action<GameObject, string> Paint = (go, png) => { foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
  string p = png ?? (r.name.StartsWith("far_terrace") ? "far/far_terrace_baked32.png" : null); var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Mat(p ?? png, .65f); r.sharedMaterials = ms; } };

// ── 땅 높이: layout 물건 높이(z)를 역거리 가중으로 이은 면(가설 — 구성안에 땅 모양 파일이 없어서) ──
var L = new System.Collections.Generic.List<Vector3>();
foreach (var x in new float[] { -12, -6, 0, 6, 12 }) foreach (var y in new float[] { -20, -12, -6, 0, 4.5f }) L.Add(new Vector3(x, y, 0));
var json = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../art/_review/2026-10-27-scene-a2/layout.json"));
var items = new System.Collections.Generic.List<(string name, string layer, float x, float y, float z, float rot, float tilt)>();
foreach (System.Text.RegularExpressions.Match mt in System.Text.RegularExpressions.Regex.Matches(json, "\"name\":\\s*\"([^\"]+)\",\\s*\"layer\":\\s*\"(\\w+)\".*?\"pos\":\\s*\\[([^\\]]+)\\],\\s*\"rot_deg\":\\s*([-\\d.]+).*?\"tilt_deg\":\\s*([-\\d.]+)", System.Text.RegularExpressions.RegexOptions.Singleline))
{ var p = mt.Groups[3].Value.Split(','); items.Add((mt.Groups[1].Value, mt.Groups[2].Value, float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), float.Parse(mt.Groups[4].Value), float.Parse(mt.Groups[5].Value))); }
foreach (var it in items) if (it.layer != "near" && !it.name.StartsWith("hill") && it.name != "sea") L.Add(new Vector3(it.x, it.y, it.z));
L.Add(new Vector3(16, 78, -8.6f)); L.Add(new Vector3(-60, 60, -4)); L.Add(new Vector3(-60, 140, -8)); L.Add(new Vector3(90, 60, -12)); L.Add(new Vector3(90, 150, -14));
System.Func<float, float, float> H = (x, y) => { if (y < 4.5f && Mathf.Abs(x) < 10.5f) return -0.02f; float sw = 0, sz = 0; foreach (var q in L) { float d2 = (q.x - x) * (q.x - x) + (q.y - y) * (q.y - y) + 1f; float w = 1f / (d2 * d2); sw += w; sz += w * q.z; } return sz / sw; };
log.Append($"items {items.Count} anchors {L.Count} H(0,20)={H(0, 20):F2} H(20,80)={H(20, 80):F2}\n");

// 격자 메시 도우미: (i,j) → Blender 좌표, uv
System.Func<string, int, int, System.Func<int, int, Vector3>, System.Func<int, int, Vector3, Vector2>, Mesh> Grid = (nm, nx, ny, f, uvf) => {
  var v = new Vector3[nx * ny]; var uv = new Vector2[nx * ny]; var t = new int[(nx - 1) * (ny - 1) * 6]; int k = 0;
  for (int j = 0; j < ny; j++) for (int i = 0; i < nx; i++) { var b = f(i, j); v[j * nx + i] = U(b.x, b.y, b.z); uv[j * nx + i] = uvf(i, j, b); }
  for (int j = 0; j < ny - 1; j++) for (int i = 0; i < nx - 1; i++) { int a = j * nx + i; t[k++] = a; t[k++] = a + nx; t[k++] = a + 1; t[k++] = a + 1; t[k++] = a + nx; t[k++] = a + nx + 1; }
  var m = new Mesh { name = nm, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = v, uv = uv, triangles = t }; m.RecalculateNormals(); m.RecalculateBounds();
  AssetDatabase.CreateAsset(m, D + "Gen/" + nm + ".asset"); return m; };
System.Action<string, Mesh, Material> MeshGO = (nm, m, mat) => { var g = new GameObject(nm); g.transform.SetParent(root, false); g.AddComponent<MeshFilter>().sharedMesh = m; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; };

foreach (var tp in new[] { D + "Gen/grass_tile.png", D + "Gen/road_tile.png" }) { AssetDatabase.ImportAsset(tp); var ti = (TextureImporter)AssetImporter.GetAtPath(tp); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport(); }
// 땅(중경~원경): x −80..90, y −30..150, 2 m 격자. 텍스처 = V5 땅 사진(32 texel/m 그대로)
MeshGO("Terrain", Grid("a2_terrain", 86, 91, (i, j) => { float x = -80 + i * 2, y = -30 + j * 2; return new Vector3(x, y, H(x, y) - 0.03f); }, (i, j, b) => new Vector2(b.x / 32f, b.y / 29.6f)), Mat("Gen/grass_tile.png", .65f));
// 길: 중심선 따라 폭 4 m, 흙 텍스처, 땅 + 4 cm
var cl = new Vector2[] { new(0, -12), new(0, 0), new(0, 8), new(1.5f, 16), new(5, 24), new(10, 32), new(16, 40), new(22, 52), new(26, 70), new(27, 95), new(24, 125) };
var pts = new System.Collections.Generic.List<Vector2>(); for (int s = 0; s < cl.Length - 1; s++) for (int q = 0; q < 8; q++) pts.Add(Vector2.Lerp(cl[s], cl[s + 1], q / 8f)); pts.Add(cl[cl.Length - 1]); pts.RemoveAll(q => q.y < 3f); var cum = new float[pts.Count]; for (int q = 1; q < pts.Count; q++) cum[q] = cum[q - 1] + (pts[q] - pts[q - 1]).magnitude;
MeshGO("Road", Grid("a2_road", 5, pts.Count, (i, j) => { var a = pts[Mathf.Max(0, j - 1)]; var b = pts[Mathf.Min(pts.Count - 1, j + 1)]; var d = (b - a).normalized; var n = new Vector2(d.y, -d.x); var p = pts[j] + n * (i - 2) * 1.0f; return new Vector3(p.x, p.y, (p.y < 4.5f ? 0.0f : H(p.x, p.y)) + 0.04f); }, (i, j, b) => new Vector2(i / 4f * 0.98f, cum[j] / 7.8f)), Mat("Gen/road_tile.png", .65f));
// 언덕 3개(돔), 땅 텍스처
foreach (var it in items) if (it.name.StartsWith("hill"))
{
  var sz = it.name == "hill_L" ? new Vector3(150, 105, 38) : it.name == "hill_R" ? new Vector3(170, 119, 45) : new Vector3(120, 84, 22);
  var c = it; MeshGO(it.name, Grid("a2_" + it.name, 25, 9, (i, j) => { float a = i / 24f * Mathf.PI * 2, r = j / 8f; return new Vector3(c.x + Mathf.Cos(a) * r * sz.x / 2, c.y + Mathf.Sin(a) * r * sz.y / 2, c.z - 2 + sz.z * Mathf.Pow(1 - r * r, 0.8f)); }, (i, j, b) => new Vector2(b.x / 32f, b.y / 29.6f)), Mat("Gen/grass_tile.png", .65f));
}
// 하늘 그라데이션: 2×128 텍스처(지평선 = 안개색 → 꼭대기 옅은 하늘색) 반구, 안개 없음
var gradPath = D + "Gen/sky_grad.png";
{ var tx = new Texture2D(2, 128, TextureFormat.RGB24, false); Color top = new Color(.55f, .70f, .84f); for (int y = 0; y < 128; y++) { var c = Color.Lerp(fogC, top, Mathf.Pow(y / 127f, 0.6f)); tx.SetPixel(0, y, c); tx.SetPixel(1, y, c); }
  System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(Application.dataPath + "/../" + gradPath), tx.EncodeToPNG()); AssetDatabase.ImportAsset(gradPath);
  var ti = (TextureImporter)AssetImporter.GetAtPath(gradPath); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
MeshGO("Sky", Grid("a2_sky", 33, 12, (i, j) => { float a = i / 32f * Mathf.PI * 2, e = Mathf.Lerp(-0.25f, Mathf.PI / 2, j / 11f); return new Vector3(Mathf.Cos(a) * Mathf.Cos(e) * 1500, Mathf.Sin(a) * Mathf.Cos(e) * 1500, Mathf.Sin(e) * 1500); }, (i, j, b) => new Vector2(0.5f, Mathf.Clamp01(b.z / 1500f))), Mat("Gen/sky_grad.png", 0));

// ── 큰 덩어리 ──
var yard = Put("far/yard_ground.fbx", root); yard.transform.localPosition = U(0, 0, 0.02f); Paint(yard, "far/yard_ground_baked32.png");
var far = Put("far/far_town.fbx", root); far.transform.localPosition = U(16, 78, -8.6f); Paint(far, "far/far_town_baked32.png");
// 정비소 V5: Scene A 에선 카트가 정비소 원점 앞 8.3 m(+z). 180° 돌려 카트 뒤(+z)에, 앞면이 Blender y −8 에 오게(구성안 y −8~−16)
var gar = Put("v5/garage_v5_combo.fbx", root); gar.transform.localRotation = Quaternion.Euler(0, 180, 0); gar.transform.localPosition = new Vector3(0.7f, 0, 11.5f); Paint(gar, "v5/garage_baked32.png");
var sea = Put("v5/sea_v5_combo.fbx", root); sea.transform.localScale = new Vector3(1.125f, 1, 1.25f); sea.transform.localPosition = U(0, 500, -50.48f) - new Vector3(0, -1.2f, -311f * 1.25f); Paint(sea, "v5/sea_baked32.png");
var kart = Put("kart/kart_a2.fbx", root); kart.name = "Kart"; Paint(kart, "kart/kart_a2_baked32.png");

// ── 근·중경 물건: layout 이름 → 키트 ──
var map = new System.Collections.Generic.Dictionary<string, string> {
  { "tire_stack_L1", "tires_01" }, { "tire_stack_L2", "tires_02" }, { "drum_red", "drum_01" }, { "drum_teal", "drum_02" }, { "pot_R1", "pot_01" }, { "pot_R2", "pot_02" }, { "pot_L", "pot_01" },
  { "pallet_crates", "pallet_01" }, { "fence_L", "fence_02" }, { "fence_R", "fence_02" }, { "sign_garage", "bench_01" },
  { "street_lamp", "streetlight_01" }, { "bicycle", "bicycle_01" }, { "old_car", "old_car_01" },
  { "house_L1", "background_building_01" }, { "house_R1", "background_building_02" }, { "shed_L2", "background_building_03" },
  { "tree_L", "autumn_tree_01" }, { "tree_R", "autumn_tree_02" }, { "tree_L2", "autumn_tree_03" } };
// 키트에 있으나 layout 에 없는 것(가설 자리): 나무 2·건물 1·드럼·콘·팔레트·변압기
items.Add(("x_tree4", "mid", 18, 36, H(18, 36), 0, 0)); map["x_tree4"] = "autumn_tree_04";
items.Add(("x_tree5", "mid", -16, 50, H(-16, 50), 0, 0)); map["x_tree5"] = "autumn_tree_05";
items.Add(("x_bld4", "mid", -14, 44, H(-14, 44), 15, 0)); map["x_bld4"] = "background_building_04";
items.Add(("x_drum3", "near", 4.3f, 0.6f, 0, 0, 0)); map["x_drum3"] = "drum_03";
items.Add(("x_cone1", "near", 1.9f, 4.9f, 0, 0, 0)); map["x_cone1"] = "cone_01";
items.Add(("x_cone2", "near", -1.8f, 5.3f, 0, 30, 0)); map["x_cone2"] = "cone_02";
items.Add(("x_pallet2", "near", -3.6f, -1.2f, 0, -12, 0)); map["x_pallet2"] = "pallet_02";
items.Add(("x_transformer", "mid", -2.9f, 22.3f, H(-2.9f, 22.3f), 0, 0)); map["x_transformer"] = "transformer_01";
int placed = 0; var miss = new System.Collections.Generic.List<string>();
foreach (var it in items)
{
  if (!map.TryGetValue(it.name, out var kit)) { if (it.layer != "far") miss.Add(it.name); continue; }
  var g = Put("kit/" + kit + ".fbx", root); g.name = it.name + "_" + kit;
  g.transform.localPosition = U(it.x, it.y, it.z); g.transform.localRotation = Quaternion.Euler(0, -it.rot, it.tilt) * g.transform.localRotation;
  // 키트 건물(높이 5.3~12 m)·나무(12.5~15 m)는 구성안 크기보다 크다 → 건물은 벽 높이 × 1.6(지붕 포함 2층 집), 나무는 구성안 키로 줄임(가설)
  float hz = 0; var mt2 = System.Text.RegularExpressions.Regex.Match(json, "\"name\":\\s*\"" + it.name + "\".*?\"size_m\":\\s*\\[[^,]+,[^,]+,\\s*([\\d.]+)\\]", System.Text.RegularExpressions.RegexOptions.Singleline); if (mt2.Success) hz = float.Parse(mt2.Groups[1].Value);
  var bs = g.GetComponentInChildren<Renderer>().bounds.size.y;
  if (kit.StartsWith("background_building")) g.transform.localScale *= (hz > 0 ? hz * 1.6f : 6f) / bs;
  if (kit.StartsWith("autumn_tree")) g.transform.localScale *= (hz > 0 ? hz : 9.5f) / bs;
  Paint(g, "kit/" + kit + ".png"); placed++;
}
// 전봇대: pole_01 은 전봇대 여러 개 + 전선이 한 줄(35 m)로 든 모델 → 길 왼쪽에 한 줄만, 길 방향(17°)·내리막(−7°)을 따라 (가설)
{ var g = Put("kit/pole_01.fbx", root); g.name = "poles_row_pole_01"; g.transform.localPosition = U(-3.6f, 20, H(-3.6f, 20) + 0.2f); g.transform.localRotation = Quaternion.Euler(-7f, 17f, 0) * g.transform.localRotation; Paint(g, "kit/pole_01.png"); placed++; }
log.Append($"placed {placed} · 빠짐(새 데칼 등) {string.Join(",", miss)}\n");

// ── 카메라: B(게임·판정) 뒤 5.6 높이 2.8 FOV 64 → 앞 6 m 높이 1.0 / D 뒤 3.9 높이 2.0 → 앞 5 m 높이 0.3 / 소개(카트 앞 오른쪽에서 카트+정비소) ──
RenderSettings.fog = false; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white;
var camDefs = new (string n, Vector3 pos, Vector3 at, float fov)[] {
  ("Cam_B_Game", U(0, -5.6f, 2.8f), U(0, 6, 1.0f), 64), ("Cam_D", U(0, -3.9f, 2.0f), U(0, 5, 0.3f), 64), ("Cam_Intro", U(5.5f, 7.5f, 4.5f), U(-0.5f, -5f, 1.2f), 55) };
for (int c = 0; c < camDefs.Length; c++)
{
  var cg = new GameObject(camDefs[c].n); var cam = cg.AddComponent<Camera>(); if (c == 0) cg.tag = "MainCamera";
  cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fogC; cam.nearClipPlane = 0.1f; cam.farClipPlane = 2000; cam.fieldOfView = camDefs[c].fov;
  cg.transform.position = camDefs[c].pos; cg.transform.LookAt(camDefs[c].at);
  var lr = cg.AddComponent<LowResRenderTest>(); lr.targetHeight = 720; lr.integerScale = false; lr.smooth = false;
  cg.SetActive(c == 0); log.Append($"{cg.name} pos={cg.transform.position:F2} rot={cg.transform.eulerAngles:F1}\n");
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_A2_Town.unity");
AssetDatabase.SaveAssets();
return log + "mats " + mats.Count;
