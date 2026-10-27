// 과제 1302 — Scene_A4b_Town = Scene_A4_Town 복사.
//  1 정비소: v5b(실내 다시 구운) png 로 교체. fbx 는 A4 것과 md5 같음(1f25dacd) → 메쉬는 그대로, 재질만 새 텍스처(SA4b_garage_baked32)
//  2 집 6채(art/export/scene-a4-houses-b): 중경 길 양쪽, 정면(로컬 +Z 가설)이 길을 보게. 바닥 = 발밑 지형 최저점. MeshCollider. 근경 background_building_03_22 끔
//  3 집 근접·정비소 실내 카메라(Cam_B_Game 복제 — LowRes 같음)
const string D = "Assets/Art/SceneA4b/", S = "Assets/Scenes/Scene_A4b_Town.unity";
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A4_Town.unity", S);
AssetDatabase.DeleteAsset("Assets/Art/SceneA4b"); AssetDatabase.CreateFolder("Assets/Art", "SceneA4b"); foreach (var f in new[] { "Mats", "garage", "houses" }) AssetDatabase.CreateFolder("Assets/Art/SceneA4b", f);
string A = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/")), P = Application.dataPath + "/Art/SceneA4b/";
foreach (var f in new[] { "garage_v5_combo.fbx", "garage_baked32.png" }) System.IO.File.Copy(A + "scene-a-v5b-garage/" + f, P + "garage/" + f, true);
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(A + "scene-a4-houses-b/" + h + e, P + "houses/" + h + e, true);
AssetDatabase.Refresh();
System.Action<string> Px = p => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); if (ti == null) return;
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.SaveAndReimport(); };
foreach (var h in H) { var mi = (ModelImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.SaveAndReimport(); }
var gmi = (ModelImporter)AssetImporter.GetAtPath(D + "garage/garage_v5_combo.fbx"); gmi.materialImportMode = ModelImporterMaterialImportMode.None; gmi.SaveAndReimport();
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var log = new System.Text.StringBuilder(); var newMats = new System.Collections.Generic.List<Material>();
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA4/Mats/SA4_bldg_background_building_03.mat");
System.Func<string, string, Material> Mk = (nm, tex) => { Px(tex); var m = new Material(tmpl) { name = nm }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
  m.SetColor("_Tint", Color.white); m.SetFloat("_Sat", 1); m.SetColor("_WB", Color.white); m.SetFloat("_Lift", 0); m.SetFloat("_HasR2", 0); m.SetFloat("_LightAmt", 0); m.SetFloat("_R1Ok", 0);
  AssetDatabase.CreateAsset(m, D + "Mats/" + nm + ".mat"); newMats.Add(m); return m; };
// ---- 1 정비소: 같은 메쉬, 새 텍스처
var gm = Mk("SA4b_garage_baked32", D + "garage/garage_baked32.png"); int gn = 0;
foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) { var ms = r.sharedMaterials; bool ch = false; for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name == "SA4_garage_baked32") { ms[i] = gm; ch = true; } if (ch) { r.sharedMaterials = ms; gn++; } }
log.Append("garage renderers " + gn + "; ");
// ---- 2 집
var near = root.Find("A4_BldgNew/background_building_03_22"); if (near) near.gameObject.SetActive(false);
var g = new GameObject("A4b_Houses"); g.transform.SetParent(root, false);
System.Func<float, Vector3> RoadAt = z => { float xs = 0, ys = 0, mn = 999, mx = -999; int n = 0;
  for (float x = -25; x <= 45; x += 0.25f) foreach (var hh in Physics.RaycastAll(new Vector3(x, 40, z), Vector3.down, 100)) if (hh.collider.name == "Road") { xs += x; ys += hh.point.y; n++; mn = Mathf.Min(mn, x); mx = Mathf.Max(mx, x); }
  return new Vector3(xs / n, ys / n, (mx - mn) * 0.5f); };   // z 자리에 반폭
// 자리: (이름, 길 z, 쪽 +1=왼쪽(+x)/-1=오른쪽(-x), 길 가장자리에서 띄움 m). 차는 -Z 로 달림 → 왼쪽 = +x
var slots = new (string h, float z, int side, float gap)[] {
  // 왼쪽 줄 1차(-23/-33/-46): 심판이 "큰 집 벽 위에 주황 나무가 떠 보임" → 나무 뒤를 피해 4m 뒤로
  ("house_cafe", -21f, -1, 2.0f), ("house_narrow_a", -27f, 1, 2.0f), ("house_slope", -31f, -1, 2.0f),
  ("house_shop", -37f, 1, 2.0f), ("house_narrow_b", -42f, -1, 2.0f), ("house_warehouse", -49f, 1, 2.5f) };
foreach (var s in slots) {
  var pre = AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + s.h + ".fbx"); var go = new GameObject(s.h); go.transform.SetParent(g.transform, false);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(pre, go.transform); inst.transform.localPosition = Vector3.zero;   // fbx 루트 회전(-90 X = 축 변환)은 그대로 둔다 → 부모로 돌림
  var m = Mk("SA4b_" + s.h, D + "houses/" + s.h + ".png"); foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
  var c = RoadAt(s.z); var c2 = RoadAt(s.z - 2); var tan = new Vector3(c2.x - c.x, 0, -2).normalized;   // 달리는 방향
  var left = Vector3.Cross(tan, Vector3.up) * -1; if (left.x < 0) left = -left; var outw = left * s.side;    // 길에서 바깥쪽
  go.transform.rotation = Quaternion.LookRotation(-outw, Vector3.up);   // 로컬 +Z(정면 가설) → 길 쪽
  var mf = go.GetComponentInChildren<MeshFilter>(); var lb = new Bounds(); bool fst = true; foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (fst) { lb = r.bounds; fst = false; } else lb.Encapsulate(r.bounds); }
  // 깊이(정면 축 길이) = 회전 전 로컬 z 크기
  float depth = 0; { var q = go.transform.rotation; go.transform.rotation = Quaternion.identity; var b0 = go.GetComponentInChildren<Renderer>().bounds; foreach (var r in go.GetComponentsInChildren<Renderer>()) b0.Encapsulate(r.bounds); depth = b0.size.z; go.transform.rotation = q; }
  var pos = new Vector3(c.x, 0, s.z) + outw * (c.z + s.gap + depth * 0.5f);
  go.transform.position = pos;
  // 바닥 = 정면 쪽(길 쪽) 세 점·가운데의 최저점(가설: 길 높이에 문이 맞게. 뒤쪽이 비탈 아래면 뜰 수 있음 → 캡처로 확인)
  float gy = 999; foreach (var o in new[] { Vector3.zero, go.transform.forward * depth * 0.4f, go.transform.forward * depth * 0.4f + go.transform.right * 2, go.transform.forward * depth * 0.4f - go.transform.right * 2 })
    foreach (var hh in Physics.RaycastAll(pos + o + Vector3.up * 40, Vector3.down, 100)) if (hh.collider.name == "Terrain" || hh.collider.name == "Road" || hh.collider.name == "DryVerge") gy = Mathf.Min(gy, hh.point.y);
  if (gy > 900) gy = c.y; pos.y = gy - 0.1f; go.transform.position = pos;
  foreach (var f in go.GetComponentsInChildren<MeshFilter>()) { var mc = f.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = f.sharedMesh; }
  log.Append(s.h + " pos " + pos.ToString("F1") + " rotY " + go.transform.eulerAngles.y.ToString("F0") + " depth " + depth.ToString("F1") + " road hw " + c.z.ToString("F1") + "; "); }
// ---- 3 카메라
Camera tc = null; foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c.name == "Cam_B_Game") tc = c;
System.Action<string, Vector3, Vector3> Cam = (nm, p, look) => { var o = UnityEngine.Object.Instantiate(tc.gameObject, tc.transform.parent); o.name = nm; o.SetActive(false); o.transform.position = p; o.transform.rotation = Quaternion.LookRotation(look - p); };
Cam("Cam_GarageIn", new Vector3(1, 1.7f, 11f), new Vector3(1, 1.4f, 20f));
var shop = g.transform.Find("house_shop"); Cam("Cam_House", shop.position + shop.forward * 9f + Vector3.up * 3.4f + shop.right * 2f, shop.position + Vector3.up * 3f);   // 1.8m 는 길 벽에 가림
// ---- 4 토글 목록에 새 재질
var tg = root.GetComponent<SceneA3Toggles>(); var all = new System.Collections.Generic.List<Material>(tg.materials); foreach (var m in newMats) all.Add(m); tg.materials = all.ToArray(); tg.Apply();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();
