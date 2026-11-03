// artlive 20261031-1926 짙은 숲 후보 F1~F4: art/export/forest-cands/{sugi,laurel}.fbx + F<n>.png → F1Studio Assets/Art/Review/forest → STUDIO/STUDIO_Slot/forest
// 비탈 = 검토용 메시 FOREST_slope (마당 뒤 z -15 → -50, 높이 0 → ~17m, ~27°), 재질 = ST_Ground 의 재질 그대로 참조(수정 없음).
// 후보마다 GROVE_F<n>(침엽 24그루 / 활엽 20그루, 같은 자리표) + SINGLE_F<n>(마당 동쪽 평지). 캡처 때 하나만 켬.
// 재질 = SA7_house_shop.mat 사본(공유 .mat 은 읽기만) + F<n>.png, _Cutoff .5. 텍스처 = Point·밉 없음·압축 없음.
if (Application.isPlaying) return "FAIL: Play 끄고";
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio 만";
const string dir = "Assets/Art/Review/forest";
System.IO.Directory.CreateDirectory(dir);
string src = System.IO.Path.GetFullPath(Application.dataPath + "/../../art/export/forest-cands/");
var SP = new[] { "sugi", "laurel" };
foreach (var s in SP) System.IO.File.Copy(src + s + ".fbx", dir + "/" + s + ".fbx", true);
for (int n = 1; n <= 4; n++) System.IO.File.Copy(src + "F" + n + ".png", dir + "/F" + n + ".png", true);
AssetDatabase.Refresh();
foreach (var s in SP) {
  var mi = (ModelImporter)AssetImporter.GetAtPath(dir + "/" + s + ".fbx");
  mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport(); }
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
var mats = new Material[5];
for (int n = 1; n <= 4; n++) {
  var ti = (TextureImporter)AssetImporter.GetAtPath(dir + "/F" + n + ".png");
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.alphaIsTransparency = true; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 2048; ti.SaveAndReimport();
  var mp = dir + "/M_forest_F" + n + ".mat"; AssetDatabase.DeleteAsset(mp);
  var m = new Material(tmpl) { name = "M_forest_F" + n }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/F" + n + ".png")); m.SetColor("_Tint", Color.white);
  m.SetFloat("_A7Lit", 1); m.SetFloat("_Sat", 1); m.SetFloat("_Cutoff", 0.5f); if (m.HasProperty("_SN")) m.SetFloat("_SN", 0); if (m.HasProperty("_Blotch")) m.SetFloat("_Blotch", 0);
  AssetDatabase.CreateAsset(m, mp); mats[n] = m; }
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;
var old = slotT.Find("forest"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var root = new GameObject("forest"); root.transform.SetParent(slotT, false);
// 비탈 높이 = 마당 끝(z -15)에서 뒤로 0.5/m + 옆으로 굽이(능선)
// s06 에 옆 굽이를 빼 봤으나 검은 띠 그대로(원인 아님) → s07 굽이 되살림
System.Func<float, float, float> H = (x, z) => z > -15f ? 0f : (-15f - z) * .5f + 1.4f * Mathf.Sin(x * .19f + .6f) * Mathf.Clamp01((-15f - z) / 8f);
// s04: x ≥ ~9 쪽이 재질을 바꿔도 검게 나와 비탈을 x 9 에서 끊음(원인 미상, 스튜디오 배경만의 문제)
{ int nx = 25, nz = 18; float x0 = -34, x1 = 16, z0 = -14.5f, z1 = -52; var v = new Vector3[nx * nz]; var tri = new System.Collections.Generic.List<int>();
  for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) { float x = Mathf.Lerp(x0, x1, i / (nx - 1f)), z = Mathf.Lerp(z0, z1, j / (nz - 1f)); v[j * nx + i] = new Vector3(x, H(x, z) - .02f, z); }
  for (int j = 0; j < nz - 1; j++) for (int i = 0; i < nx - 1; i++) { int a = j * nx + i; tri.AddRange(new[] { a, a + 1, a + nx, a + 1, a + nx + 1, a + nx }); }
  // s07: 검은 띠 원인 = 땅 텍스처(yard_ground_soft.png)가 아틀라스 — 왼쪽 아래 u 0~.62·v 0~.84 만 흙, 나머지는 짙은 남색. UV 를 흙 칸 안으로
  var uv = new Vector2[v.Length]; for (int k = 0; k < v.Length; k++) uv[k] = new Vector2(.02f + .58f * (v[k].x - x0) / (x1 - x0), .02f + .8f * (v[k].z - z0) / (z1 - z0));
  var me = new Mesh { name = "FOREST_slope" }; me.vertices = v; me.uv = uv; me.triangles = tri.ToArray(); me.RecalculateNormals(); me.RecalculateBounds();
  if (Vector3.Dot(me.normals[nx + 1], Vector3.up) < 0) { var t2 = me.triangles; for (int k = 0; k < t2.Length; k += 3) { int q = t2[k]; t2[k] = t2[k + 1]; t2[k + 1] = q; } me.triangles = t2; me.RecalculateNormals(); }
  AssetDatabase.DeleteAsset(dir + "/FOREST_slope.asset"); AssetDatabase.CreateAsset(me, dir + "/FOREST_slope.asset");
  var go = new GameObject("FOREST_slope"); go.transform.SetParent(root.transform, false); go.AddComponent<MeshFilter>().sharedMesh = me;
  // s02: ST_ground 재질 그대로는 비탈 +x 쪽이 검게 나옴(그림자 받기 꺼도 같음) → 검토 전용 M_forest_slope(SA7_house_shop 사본 + 땅 텍스처만 빌림)
  var gm = GameObject.Find("STUDIO/ST_Ground").GetComponent<Renderer>().sharedMaterial; AssetDatabase.DeleteAsset(dir + "/M_forest_slope.mat");
  var sm = new Material(tmpl) { name = "M_forest_slope" }; sm.SetTexture("_BaseMap", gm.GetTexture("_BaseMap")); sm.SetColor("_Tint", Color.white); sm.SetFloat("_A7Lit", 1); sm.SetFloat("_Cutoff", 0f);
  AssetDatabase.CreateAsset(sm, dir + "/M_forest_slope.mat");
  var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = sm; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }   // s05: 비탈이 제 몸에 큰 그림자 띠(+x 가장자리 ~10m)를 떨궈 → 땅은 그림자 드리우기 끔
var log = new System.Text.StringBuilder();
// 자리표: 침엽 6열×4줄 = 24 (간격 5.0, 줄마다 반 칸 엇갈림) / 활엽 5열×4줄 = 20 (간격 6.4)
System.Collections.Generic.List<Vector4> Spots(int cols, int rows, float dx, float dz, int seed, float smin, float smax) {
  var rnd = new System.Random(seed); var L = new System.Collections.Generic.List<Vector4>();
  float xc = -4f; for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) {
    float x = xc + (c - (cols - 1) / 2f + (r % 2) * .5f) * dx + (float)(rnd.NextDouble() - .5) * dx * .4f;
    float z = -18.5f - r * dz + (float)(rnd.NextDouble() - .5) * dz * .4f;
    L.Add(new Vector4(x, z, (float)rnd.NextDouble() * 360f, smin + (float)rnd.NextDouble() * (smax - smin))); }
  return L; }
// s02: 빽빽하게 — 침엽 5×5 = 25 (간격 3.8 × 4.4), 활엽 5×4 = 20 (5.6 × 5.4). s01(5.0 × 5.6 / 6.4 × 6.2)은 사이로 땅이 보였다
var SPOTS = new[] { Spots(5, 5, 3.8f, 4.4f, 7, .8f, 1.2f), Spots(5, 4, 5.6f, 5.4f, 9, .85f, 1.15f) };
for (int n = 1; n <= 4; n++) {
  int si = (n - 1) / 2; var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/" + SP[si] + ".fbx");
  var grove = new GameObject("GROVE_F" + n); grove.transform.SetParent(root.transform, false); grove.SetActive(n == 1);
  int k = 0; foreach (var p in SPOTS[si]) {
    var t = (GameObject)PrefabUtility.InstantiatePrefab(fbx); PrefabUtility.UnpackPrefabInstance(t, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
    t.name = "F" + n + "_" + (++k).ToString("00"); t.transform.SetParent(grove.transform, false);
    t.transform.localPosition = new Vector3(p.x, H(p.x, p.y) - .1f, p.y); t.transform.localRotation = Quaternion.Euler(0, p.z, 0); t.transform.localScale = Vector3.one * p.w;
    foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int j = 0; j < ms.Length; j++) ms[j] = mats[n]; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; } }
  var one = (GameObject)PrefabUtility.InstantiatePrefab(fbx); PrefabUtility.UnpackPrefabInstance(one, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
  one.name = "SINGLE_F" + n; one.transform.SetParent(root.transform, false); one.transform.localPosition = new Vector3(10f, 0, -8f); one.transform.localRotation = Quaternion.Euler(0, 30, 0); one.SetActive(n == 1);
  foreach (var r in one.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int j = 0; j < ms.Length; j++) ms[j] = mats[n]; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
  log.Append("F" + n + " " + k + "그루; ");
}
// 캠: 멀리(마당 앞 ~50m, 집 너머 비탈) · 카트(2.8m, FOV 64 = Cam_ST_34, 길에서 비탈 밑) · 단독(눈높이 1.6m, FOV 50)
var cams = GameObject.Find("STUDIO/STUDIO_Cams").transform; var srcC = cams.Find("Cam_ST_34");
void Cam(string nm, Vector3 pos, Vector3 look, float fov) {
  var o = cams.Find(nm); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject);
  var c = UnityEngine.Object.Instantiate(srcC.gameObject, cams); c.name = nm; c.transform.position = pos; c.transform.LookAt(look); c.GetComponent<Camera>().fieldOfView = fov; c.SetActive(false); }
Cam("Cam_FO_far", new Vector3(9f, 12f, 28f), new Vector3(-4f, 6.5f, -28f), 50f);   // s03: 집이 무리를 가리던 것 → 캠을 집 지붕 위로(12m) 올려 집 너머 비탈
Cam("Cam_FO_kart", new Vector3(9f, 2.8f, -4f), new Vector3(-4f, 6.5f, -24f), 64f);
Cam("Cam_FO_single", new Vector3(14f, 1.6f, 7f), new Vector3(10f, 4.6f, -8f), 50f);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slotT.gameObject.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(slotT.gameObject.scene);
return "OK " + log;
