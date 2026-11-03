// artlive 20261031-0205 배경 집 유형 H1~H4 × A/B: art/export/house-types/H?.fbx + H?A/B.png + site → F1Studio Assets/Art/Review/house_types → STUDIO/STUDIO_Slot/house_types
// 비탈 마을 배치: 앞줄(땅·길가) H4 가게(house_home 은 마을 컷에서 끔 — 옆에 줄 세우지 않음) · 1단(1.4m) H2 연립 + H1 단독 · 2단(2.8m) H3 좁은 3층. 집마다 A·B 두 벌 같은 자리, 캡처 때 하나만 켬.
// 재질 = SA7_house_shop.mat 사본(공유 .mat 은 읽기만). 텍스처 = Point·밉 없음·압축 없음.
if (Application.isPlaying) return "FAIL: Play 끄고";
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio 만";
const string dir = "Assets/Art/Review/house_types";
System.IO.Directory.CreateDirectory(dir);
string src = System.IO.Path.GetFullPath(Application.dataPath + "/../../art/export/house-types/");
var SH = new[] { "H1", "H2", "H3", "H4", "site" };
var TX = new[] { "H1A", "H1B", "H2A", "H2B", "H3A", "H3B", "H4A", "H4B", "site" };
foreach (var s in SH) System.IO.File.Copy(src + s + ".fbx", dir + "/" + s + ".fbx", true);
foreach (var s in TX) System.IO.File.Copy(src + s + ".png", dir + "/" + s + ".png", true);
AssetDatabase.Refresh();
foreach (var s in SH) {
  var mi = (ModelImporter)AssetImporter.GetAtPath(dir + "/" + s + ".fbx");
  mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport(); }
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
var mats = new System.Collections.Generic.Dictionary<string, Material>();
foreach (var s in TX) {
  var ti = (TextureImporter)AssetImporter.GetAtPath(dir + "/" + s + ".png");
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 4096; ti.SaveAndReimport();
  var mp = dir + "/M_ht_" + s + ".mat"; AssetDatabase.DeleteAsset(mp);
  var m = new Material(tmpl) { name = "M_ht_" + s }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + s + ".png")); m.SetColor("_Tint", Color.white);
  m.SetFloat("_A7Lit", 1); m.SetFloat("_Sat", 1); if (m.HasProperty("_SN")) m.SetFloat("_SN", 0); if (m.HasProperty("_Blotch")) m.SetFloat("_Blotch", 0);
  AssetDatabase.CreateAsset(m, mp); mats[s] = m; }
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;
var old = slotT.Find("house_types"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var root = new GameObject("house_types"); root.transform.SetParent(slotT, false);
GameObject Put(string fbx, string tex, Transform par, Vector3 pos, bool act) {
  var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/" + fbx + ".fbx"));
  PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); g.name = tex; g.transform.SetParent(par, false); g.transform.localPosition = pos; g.SetActive(act);
  foreach (var r in g.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int j = 0; j < ms.Length; j++) ms[j] = mats[tex]; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
  return g; }
Put("site", "site", root.transform, new Vector3(0, 0, -6f), true);
var POS = new[] { new Vector3(7.6f, 1.4f, -7.2f), new Vector3(-6.6f, 1.4f, -7.4f), new Vector3(1.9f, 2.8f, -17.0f), new Vector3(10.0f, 0f, 1.05f) };
var log = new System.Text.StringBuilder();
for (int i = 0; i < 4; i++) {
  var spot = new GameObject("H" + (i + 1)); spot.transform.SetParent(root.transform, false); spot.transform.localPosition = POS[i];
  Put("H" + (i + 1), "H" + (i + 1) + "A", spot.transform, Vector3.zero, true); Put("H" + (i + 1), "H" + (i + 1) + "B", spot.transform, Vector3.zero, false);
  var b = new Bounds(spot.transform.position, Vector3.zero); foreach (var r in spot.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
  log.Append("H" + (i + 1) + " " + b.min.ToString("F1") + ".." + b.max.ToString("F1") + "; ");
}
// 캠: 마을 눈높이(길 위 1.6m, FOV 50) · 카트(2.8m, FOV 64) · 집마다 단독(앞 왼쪽 = 해 받는 쪽, 눈높이)
var cams = GameObject.Find("STUDIO/STUDIO_Cams").transform; var srcC = cams.Find("Cam_ST_34");
void Cam(string nm, Vector3 pos, Vector3 look, float fov) {
  var o = cams.Find(nm); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject);
  var c = UnityEngine.Object.Instantiate(srcC.gameObject, cams); c.name = nm; c.transform.position = pos; c.transform.LookAt(look); c.GetComponent<Camera>().fieldOfView = fov; c.SetActive(false); }
Cam("Cam_HT_village_eye", new Vector3(-4.0f, 1.6f, 12.5f), new Vector3(3.0f, 8.0f, -12f), 50f);   // 길 건너편에서 비탈 위로 비스듬히(house_home 은 끔)
Cam("Cam_HT_village_kart", new Vector3(-7.0f, 2.8f, 6.0f), new Vector3(4.0f, 4.8f, -11f), 64f);
var DIST = new[] { 12.5f, 16f, 13.5f, 12.5f }; var HH = new[] { 4.0f, 3.8f, 4.8f, 3.6f };
for (int i = 0; i < 4; i++) {
  var d = new Vector2(-.5f, 1f).normalized; var P = POS[i] + new Vector3(0, 0, -3.2f);
  Cam("Cam_HT_H" + (i + 1) + "_solo", new Vector3(P.x + d.x * DIST[i], POS[i].y + 1.6f, P.z + d.y * DIST[i]), new Vector3(P.x, POS[i].y + HH[i], P.z), 50f);
}
root.SetActive(false);  // 저장은 꺼둔 채(다른 세션 스튜디오 화면 그대로) — 캡처 때만 켬
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slotT.gameObject.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(slotT.gameObject.scene);
return "OK " + log;
