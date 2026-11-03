// artlive 20261031-0142 나무 후보 T1~T8: art/export/tree-cands/{종}.fbx + T<n>.png → F1Studio Assets/Art/Review/trees → STUDIO/STUDIO_Slot/trees
// 자리 = house_home·field_patch 서쪽·뒤 마당(집 그림자 밖, 해 = 서북). 종마다 A·B 두 벌을 같은 자리에 두고 캡처 때 하나만 켬.
// 재질 = SA7_house_shop.mat 사본(공유 .mat 은 읽기만) + T<n>.png, _Cutoff .5(잎 카드 알파). 텍스처 = Point·밉 없음·압축 없음.
if (Application.isPlaying) return "FAIL: Play 끄고";
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio 만";
const string dir = "Assets/Art/Review/trees";
System.IO.Directory.CreateDirectory(dir);
string src = System.IO.Path.GetFullPath(Application.dataPath + "/../../art/export/tree-cands/");
var SP = new[] { "pine", "celtis", "camellia", "cypress" };
var POS = new[] { new Vector3(-13.4f, 0, -4.2f), new Vector3(-7.6f, 0, -9.6f), new Vector3(-12.6f, 0, 0.4f), new Vector3(-3.4f, 0, -6.2f) };
var YAW = new[] { 180f, 20f, 0f, 0f };   // 곰솔: FBX 에서 기울기가 -x(마당 밖) → 180° 돌려 밭 쪽(+x)으로
foreach (var s in SP) { System.IO.File.Copy(src + s + ".fbx", dir + "/" + s + ".fbx", true); }
for (int n = 1; n <= 8; n++) System.IO.File.Copy(src + "T" + n + ".png", dir + "/T" + n + ".png", true);
AssetDatabase.Refresh();
foreach (var s in SP) {
  var mi = (ModelImporter)AssetImporter.GetAtPath(dir + "/" + s + ".fbx");
  mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport(); }
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
var mats = new Material[9];
for (int n = 1; n <= 8; n++) {
  var ti = (TextureImporter)AssetImporter.GetAtPath(dir + "/T" + n + ".png");
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.alphaIsTransparency = true; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 2048; ti.SaveAndReimport();
  var mp = dir + "/M_tree_T" + n + ".mat"; AssetDatabase.DeleteAsset(mp);
  var m = new Material(tmpl) { name = "M_tree_T" + n }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/T" + n + ".png")); m.SetColor("_Tint", Color.white);
  m.SetFloat("_A7Lit", 1); m.SetFloat("_Sat", 1); m.SetFloat("_Cutoff", 0.5f); if (m.HasProperty("_SN")) m.SetFloat("_SN", 0); if (m.HasProperty("_Blotch")) m.SetFloat("_Blotch", 0);
  AssetDatabase.CreateAsset(m, mp); mats[n] = m; }
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;
var old = slotT.Find("trees"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var root = new GameObject("trees"); root.transform.SetParent(slotT, false);
var log = new System.Text.StringBuilder();
for (int i = 0; i < 4; i++) {
  var spot = new GameObject("TREE_" + SP[i]); spot.transform.SetParent(root.transform, false); spot.transform.localPosition = POS[i]; spot.transform.localRotation = Quaternion.Euler(0, YAW[i], 0);
  for (int k = 0; k < 2; k++) {
    int n = i * 2 + k + 1;
    var mdl = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/" + SP[i] + ".fbx"));
    PrefabUtility.UnpackPrefabInstance(mdl, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); mdl.name = "T" + n; mdl.transform.SetParent(spot.transform, false); mdl.SetActive(k == 0);
    foreach (var r in mdl.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int j = 0; j < ms.Length; j++) ms[j] = mats[n]; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
  }
  var b = new Bounds(spot.transform.position, Vector3.zero); foreach (var r in spot.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
  log.Append(SP[i] + " " + b.min.ToString("F1") + ".." + b.max.ToString("F1") + "; ");
}
// 캠: 종마다 눈높이(1.6m, FOV 50) · 카트(2.8m, FOV 64 = Cam_ST_34). + 전체 1장
var cams = GameObject.Find("STUDIO/STUDIO_Cams").transform; var srcC = cams.Find("Cam_ST_34");
void Cam(string nm, Vector3 pos, Vector3 look, float fov) {
  var o = cams.Find(nm); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject);
  var c = UnityEngine.Object.Instantiate(srcC.gameObject, cams); c.name = nm; c.transform.position = pos; c.transform.LookAt(look); c.GetComponent<Camera>().fieldOfView = fov; c.SetActive(false); }
var D = new[] { new Vector2(.45f, 1f), new Vector2(.25f, 1f), new Vector2(.5f, 1f), new Vector2(-.8f, 1f) };
var DIST = new[] { 10f, 12.5f, 4.4f, 13f }; var LY = new[] { 2.8f, 3.4f, .8f, 3.8f };
for (int i = 0; i < 4; i++) {
  var d = D[i].normalized; var P = POS[i];
  Cam("Cam_TR_" + SP[i] + "_eye", new Vector3(P.x + d.x * DIST[i], 1.6f, P.z + d.y * DIST[i]), new Vector3(P.x, LY[i], P.z), 50f);
  float kd = DIST[i] * .85f; Cam("Cam_TR_" + SP[i] + "_kart", new Vector3(P.x + d.x * kd, 2.8f, P.z + d.y * kd), new Vector3(P.x, LY[i] * .85f, P.z), 64f);
}
Cam("Cam_TR_all", new Vector3(3.5f, 4.2f, 12.5f), new Vector3(-7.5f, 2.6f, -4.5f), 60f);
foreach (Transform ch in slotT) ch.gameObject.SetActive(ch.name == "house_home" || ch.name == "field_patch" || ch.name == "trees");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slotT.gameObject.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(slotT.gameObject.scene);
return "OK " + log;
