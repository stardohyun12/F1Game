// 과제 1232 — Scene_A4_Town = Scene_A3_Town(운전판) 복사. 재질 SA3_→SA4_(Assets/Art/SceneA4/Mats).
//  1 차 외형 = 카트 kart_a2(A3 의 정지 카트와 같은 모델) → Race 아래 A4_KartVisual, CarVisual_A 끔. 물리·콜라이더·카메라 그대로
//  2 건물 = art/export/scene-a2-bldg-light fbx+png 로 메쉬·텍스처 교체(이름 슬롯 유지), 원경은 안개 줄임(텍스처에 지평선색 구움)
//  3 하늘 R2 끔(계단 구름), 덤불·둔덕 채도↓·반짝임↓   4 S1~S3 켬, T1·T2 끔(토글)
const string D = "Assets/Art/SceneA4/", S = "Assets/Scenes/Scene_A4_Town.unity";
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A3_Town.unity", S);
AssetDatabase.DeleteAsset("Assets/Art/SceneA4"); AssetDatabase.CreateFolder("Assets/Art", "SceneA4"); AssetDatabase.CreateFolder("Assets/Art/SceneA4", "Mats"); AssetDatabase.CreateFolder("Assets/Art/SceneA4", "bldg");
string src = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/scene-a2-bldg-light/")), dstDir = System.IO.Path.Combine(Application.dataPath, "Art/SceneA4/bldg/");
var log = new System.Text.StringBuilder();
foreach (var f in System.IO.Directory.GetFiles(src)) if (f.EndsWith(".fbx") || f.EndsWith(".png")) System.IO.File.Copy(f, dstDir + System.IO.Path.GetFileName(f), true);
AssetDatabase.Refresh();
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA3"); root.name = "SceneA4";
System.Action<string> Px = p => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); if (ti == null) return; if (ti.filterMode != FilterMode.Point || ti.mipmapEnabled || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.npotScale != TextureImporterNPOTScale.None) {
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.SaveAndReimport(); } };
var map = new System.Collections.Generic.Dictionary<Material, Material>();
System.Func<Material, Material> Conv = m0 => {
  if (m0 == null || !m0.name.StartsWith("SA3_")) return m0; if (map.TryGetValue(m0, out var mm)) return mm;
  var m = new Material(m0) { name = "SA4_" + m0.name.Substring(4) }; AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); map[m0] = m; return m; };
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Conv(ms[i]); r.sharedMaterials = ms; }
// ---- 1 차 외형
var car = GameObject.Find("Race"); var kart = root.transform.Find("Kart"); var oldVis = car.transform.Find("CarVisual_A");
System.Func<Transform, Bounds> BB = t => { var b = new Bounds(); bool f = false; foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { if (r is TrailRenderer || r is ParticleSystemRenderer) continue; if (!f) { b = r.bounds; f = true; } else b.Encapsulate(r.bounds); } return b; };
var ob = BB(oldVis); var kv = UnityEngine.Object.Instantiate(kart.gameObject); kv.name = "A4_KartVisual"; kv.SetActive(true);
var kb0 = BB(kv.transform); float sc0 = ob.size.x / kb0.size.x;   // 폭 맞춤(가설: 옛 차 외형 폭 1.96m). 1차 길이 맞춤 ×2.02 는 소품 대비 탱크처럼 커서 버림
kv.transform.localScale *= sc0; var kb = BB(kv.transform);
kv.transform.position += new Vector3(ob.center.x - kb.center.x, ob.min.y - kb.min.y, ob.center.z - kb.center.z);
kv.transform.SetParent(car.transform, true); oldVis.gameObject.SetActive(false);
var kmat = kv.GetComponentInChildren<Renderer>().sharedMaterial; kmat.SetFloat("_LightAmt", 0); kmat.SetFloat("_R1Ok", 0);   // 구운 텍스처 = 무광(빛 두 번 안 걸림)
var kTex = kmat.GetTexture("_BaseMap"); if (kTex) Px(AssetDatabase.GetAssetPath(kTex));
log.Append("kart ×" + sc0.ToString("F2") + " old " + ob.size.ToString("F2") + " new " + BB(kv.transform).size.ToString("F2") + " tex " + (kTex ? AssetDatabase.GetAssetPath(kTex) + " " + kTex.filterMode : "none") + "; ");
// ---- 2 건물
var gOld = new GameObject("A4_BldgOld"); var gNew = new GameObject("A4_BldgNew"); gOld.transform.SetParent(root.transform, true); gNew.transform.SetParent(root.transform, true);
var fbxObjs = new System.Collections.Generic.Dictionary<string, Transform>(); var fbxTex = new System.Collections.Generic.Dictionary<string, string>();
foreach (var fb in new[] { "far_town", "background_building_01", "background_building_02", "background_building_03", "background_building_04" }) {
  var pre = AssetDatabase.LoadAssetAtPath<GameObject>(D + "bldg/" + fb + ".fbx"); if (!pre) { log.Append("fbx 없음 " + fb + "; "); continue; }
  foreach (var t in pre.GetComponentsInChildren<Transform>(true)) if (t.GetComponent<MeshFilter>()) { fbxObjs[t.name] = t; fbxTex[t.name] = fb == "far_town" ? (t.name.StartsWith("far_terrace") ? "far_terrace_baked32" : "far_town_baked32") : fb; } }
var newMat = new System.Collections.Generic.Dictionary<string, Material>(); var targets = new System.Collections.Generic.List<Renderer>();
foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) { var key = r.name; if (!fbxObjs.ContainsKey(key)) { var k2 = System.Text.RegularExpressions.Regex.Replace(key, "_\\d+$", ""); if (fbxObjs.ContainsKey(k2)) key = k2; else continue; } targets.Add(r); }
int swapped = 0;
foreach (var r in targets) {
  var key = fbxObjs.ContainsKey(r.name) ? r.name : System.Text.RegularExpressions.Regex.Replace(r.name, "_\\d+$", "");
  var nm = fbxObjs[key].GetComponent<MeshFilter>().sharedMesh; string tk = fbxTex[key]; bool far = tk.StartsWith("far");
  if (!newMat.ContainsKey(tk)) { var p = D + "bldg/" + tk + ".png"; Px(p); var m = new Material(r.sharedMaterials[0]) { name = "SA4_bldg_" + tk }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p));
    m.SetColor("_Tint", Color.white); m.SetFloat("_Sat", 1); m.SetColor("_WB", Color.white); m.SetFloat("_Lift", 0); m.SetFloat("_HasR2", 0); m.SetFloat("_LightAmt", 0); m.SetFloat("_R1Ok", 0);   // 빛 구운 텍스처 → 무광
    if (far) { m.SetFloat("_FogMax", 0.12f); var hp = m.GetVector("_HazeP"); hp.z = 0; m.SetVector("_HazeP", hp); }   // 원경: 지평선색 이미 구움 → 안개·공기층 이중 금지
    AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); newMat[tk] = m; }
  var cp = UnityEngine.Object.Instantiate(r.gameObject, gNew.transform); cp.name = r.name; cp.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
  cp.transform.localScale = Vector3.Scale(r.transform.lossyScale, new Vector3(1 / gNew.transform.lossyScale.x, 1 / gNew.transform.lossyScale.y, 1 / gNew.transform.lossyScale.z));
  cp.GetComponent<MeshFilter>().sharedMesh = nm; var mats = new Material[nm.subMeshCount]; for (int i = 0; i < mats.Length; i++) mats[i] = newMat[tk]; cp.GetComponent<MeshRenderer>().sharedMaterials = mats;
  var mc = cp.GetComponent<MeshCollider>(); if (mc) mc.sharedMesh = nm;
  // 단위가 다른 원본(옛 메쉬 cm 등): 월드 경계 크기·바닥·중심을 옛 것에 맞춘다
  var wb0 = r.bounds; var wb1 = cp.GetComponent<MeshRenderer>().bounds; float big0 = Mathf.Max(wb0.size.x, Mathf.Max(wb0.size.y, wb0.size.z)), big1 = Mathf.Max(wb1.size.x, Mathf.Max(wb1.size.y, wb1.size.z));
  if (Mathf.Abs(big1 / big0 - 1) > 0.05f) { cp.transform.localScale *= big0 / big1; wb1 = cp.GetComponent<MeshRenderer>().bounds; cp.transform.position += new Vector3(wb0.center.x - wb1.center.x, wb0.min.y - wb1.min.y, wb0.center.z - wb1.center.z);
    log.Append(r.name + " 단위 맞춤 world old " + wb0.size.ToString("F1") + " new " + cp.GetComponent<MeshRenderer>().bounds.size.ToString("F1") + "; "); }
  if (swapped < 3 || !far) log.Append(r.name + " old " + r.GetComponent<MeshFilter>().sharedMesh.bounds.size.ToString("F1") + " new " + nm.bounds.size.ToString("F1") + "; ");
  r.transform.SetParent(gOld.transform, true); swapped++; }
gOld.SetActive(false);
// ---- 3 하늘·덤불·둔덕
Material sky = null; foreach (var m in map.Values) {
  if (m.name.StartsWith("SA4_sky")) { m.SetFloat("_R2", 0); sky = m; }
  if (m.name == "SA4_bush" || m.name == "SA4_clump" || m.name == "SA4_mound") { m.SetFloat("_Sat", 0.7f); m.SetFloat("_Sun", 0.9f); m.SetFloat("_Steps", 3); }   // 가설: 채도 ×0.7, 해 쪽 하이라이트 -10%, 부드러운 명암 대신 3단
}
// ---- 4 토글
var tg = root.GetComponent<SceneA3Toggles>(); var list = new System.Collections.Generic.List<Material>();
foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { D + "Mats" })) { var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)); if (m != sky) list.Add(m); }
tg.materials = list.ToArray(); tg.S1_highlightRolloff = tg.S2_oneLight = tg.S3_softBorders = true; tg.T1_sunEdge = tg.T2_shadeAmbient = false; tg.Apply();
if (sky) { sky.SetFloat("_S1", 1); sky.SetFloat("_S2", 1); sky.SetFloat("_S3", 1); }   // 하늘도 같은 한 빛(R2 만 제외)
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return "mats " + map.Count + " · bldg " + swapped + " · toggles mats " + list.Count + "\n" + log;
