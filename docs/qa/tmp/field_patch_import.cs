// artlive 20261030-2003 field_patch 1단계(회색 형태): art/export/field-patch/field_patch.fbx → F1Studio Assets/Art/Review/field_asset → STUDIO/STUDIO_Slot/field_patch
// 자리 = house_home 서쪽 마당(x -11.4..-5.4 — 동쪽은 서북 해에 집 그림자가 덮음, z -4.65..0.85 = 스튜디오 길 띠 z 1.5 앞, 입구 +Z). 재질 = SA7_house_shop.mat 사본(텍스처 없음, 회색 틴트). 캠 Cam_FP_front · Cam_FP_kart(카메라 B 높이 2.8·FOV 64) = Cam_ST_34 복제.
// 공유 .mat 은 읽기만. 다시 돌리면 덮어쓴다.
if (Application.isPlaying) return "FAIL: Play 끄고";
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio 만";
const string dir = "Assets/Art/Review/field_asset";
System.IO.Directory.CreateDirectory(dir);
System.IO.File.Copy(System.IO.Path.GetFullPath(Application.dataPath + "/../../art/export/field-patch/field_patch.fbx"), dir + "/field_patch.fbx", true);
AssetDatabase.ImportAsset(dir + "/field_patch.fbx", ImportAssetOptions.ForceUpdate);
var mi = (ModelImporter)AssetImporter.GetAtPath(dir + "/field_patch.fbx");
mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport();
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
var mp = dir + "/M_field_grey.mat"; AssetDatabase.DeleteAsset(mp);
var m = new Material(tmpl) { name = "M_field_grey" }; m.SetTexture("_BaseMap", null); m.SetColor("_Tint", new Color(.55f, .55f, .53f)); m.SetFloat("_A7Lit", 1); m.SetFloat("_Sat", 1); if (m.HasProperty("_SN")) m.SetFloat("_SN", 0); if (m.HasProperty("_Blotch")) m.SetFloat("_Blotch", 0); if (m.HasProperty("_GroundY")) m.SetFloat("_GroundY", 0);
AssetDatabase.CreateAsset(m, mp);
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;
var old = slotT.Find("field_patch"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var root = new GameObject("field_patch"); root.transform.SetParent(slotT, false); root.transform.localPosition = new Vector3(-8.4f, 0f, -1.9f);
var mdl = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/field_patch.fbx"));
PrefabUtility.UnpackPrefabInstance(mdl, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); mdl.transform.SetParent(root.transform, false);
int nr = 0; foreach (var r in mdl.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; nr++; }
var b = new Bounds(root.transform.position, Vector3.zero); foreach (var r in root.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
// 캠
var cams = GameObject.Find("STUDIO/STUDIO_Cams").transform; var src = cams.Find("Cam_ST_34");
string Cam(string nm, Vector3 pos, Vector3 look, float fov) {
  var o = cams.Find(nm); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject);
  var c = UnityEngine.Object.Instantiate(src.gameObject, cams); c.name = nm; c.transform.position = pos; c.transform.LookAt(look); c.GetComponent<Camera>().fieldOfView = fov; c.SetActive(false);
  return nm + " " + pos.ToString("F1") + " fov " + fov + "; "; }
var log = Cam("Cam_FP_front", new Vector3(-7.0f, 5.6f, 6.8f), new Vector3(-7.2f, 0.2f, -1.9f), 50f) + Cam("Cam_FP_kart", new Vector3(-13.0f, 2.8f, 4.0f), new Vector3(-7.4f, 0.5f, -1.8f), 64f) + Cam("Cam_FP_close", new Vector3(-9.6f, 1.5f, 1.7f), new Vector3(-9.0f, 0.25f, -1.8f), 50f);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slotT.gameObject.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(slotT.gameObject.scene);
return "OK renderers " + nr + " bounds " + b.min.ToString("F2") + ".." + b.max.ToString("F2") + " src fov " + src.GetComponent<Camera>().fieldOfView + " | " + log;
