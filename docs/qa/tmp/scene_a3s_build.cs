// 과제 1141 — Scene_A3s_Town = A3 복사. 배치·메쉬·렌더링·카메라 그대로, 색 보정만(S1·S2·S3 + 시험 T1·T2) + 추가 2: 아트 2 건물 텍스처 교체(토글).
//  재질 SA3_* → SA3s_* (같은 셰이더 PitCrew/SceneA3/Look, 새 값 _S1/_S2/_S3/_T1/_T2). 건물: art/export/scene-a2-bldg-light → Assets/Art/SceneA3s/bldg
//  건물 교체 토글 = A3s_BldgOld(원래) / A3s_BldgNew(복제 + 새 메쉬·텍스처) 켜고 끔. 값 = 가설.
const string D = "Assets/Art/SceneA3s/", S = "Assets/Scenes/Scene_A3s_Town.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A3_Town.unity", S);
AssetDatabase.DeleteAsset("Assets/Art/SceneA3s"); AssetDatabase.CreateFolder("Assets/Art", "SceneA3s"); AssetDatabase.CreateFolder("Assets/Art/SceneA3s", "Mats"); AssetDatabase.CreateFolder("Assets/Art/SceneA3s", "bldg");
string src = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/scene-a2-bldg-light/")), dstDir = System.IO.Path.Combine(Application.dataPath, "Art/SceneA3s/bldg/");
var log = new System.Text.StringBuilder(); bool haveKit = System.IO.Directory.Exists(src);
if (haveKit) foreach (var f in System.IO.Directory.GetFiles(src)) if (f.EndsWith(".fbx") || f.EndsWith(".png")) System.IO.File.Copy(f, dstDir + System.IO.Path.GetFileName(f), true);
AssetDatabase.Refresh();
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA3"); root.name = "SceneA3s";
var map = new System.Collections.Generic.Dictionary<Material, Material>();
System.Func<Material, Material> Conv = m0 => {
  if (m0 == null || !m0.name.StartsWith("SA3_")) return m0; if (map.TryGetValue(m0, out var mm)) return mm;
  var m = new Material(m0) { name = "SA3s_" + m0.name.Substring(4) }; AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); map[m0] = m; return m; };
int rn = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{ var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Conv(ms[i]); r.sharedMaterials = ms; rn++; }

// ---- 건물 교체
var gOld = new GameObject("A3s_BldgOld"); var gNew = new GameObject("A3s_BldgNew"); gOld.transform.SetParent(root.transform, true); gNew.transform.SetParent(root.transform, true);
int swapped = 0;
if (haveKit) {
  System.Func<string, Texture2D> Tex = p => { var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
    ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(p); };
  var fbxObjs = new System.Collections.Generic.Dictionary<string, Transform>(); var fbxTex = new System.Collections.Generic.Dictionary<string, string>();
  foreach (var fb in new[] { "far_town", "background_building_01", "background_building_02", "background_building_03", "background_building_04" }) {
    var pre = AssetDatabase.LoadAssetAtPath<GameObject>(D + "bldg/" + fb + ".fbx"); if (!pre) { log.Append("fbx 없음 " + fb + "; "); continue; }
    foreach (var t in pre.GetComponentsInChildren<Transform>(true)) if (t.GetComponent<MeshFilter>()) { fbxObjs[t.name] = t; fbxTex[t.name] = fb == "far_town" ? (t.name.StartsWith("far_terrace") ? "far_terrace_baked32" : "far_town_baked32") : fb; } }
  var newMat = new System.Collections.Generic.Dictionary<string, Material>();
  var targets = new System.Collections.Generic.List<Renderer>();
  foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) if (fbxObjs.ContainsKey(r.name)) targets.Add(r);
  foreach (var r in targets) {
    var ft = fbxObjs[r.name]; var nm = ft.GetComponent<MeshFilter>().sharedMesh; var om = r.GetComponent<MeshFilter>().sharedMesh;
    string tk = fbxTex[r.name];
    if (!newMat.ContainsKey(tk)) { var m = new Material(r.sharedMaterials[0]) { name = "SA3s_bldg_" + tk }; m.SetTexture("_BaseMap", Tex(D + "bldg/" + tk + ".png")); m.SetColor("_Tint", Color.white); m.SetFloat("_Sat", 1); m.SetColor("_WB", Color.white); m.SetFloat("_Lift", 0);
      m.SetFloat("_HasR2", 0); AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat"); newMat[tk] = m; }
    var cp = UnityEngine.Object.Instantiate(r.gameObject, gNew.transform); cp.name = r.name; cp.transform.position = r.transform.position; cp.transform.rotation = r.transform.rotation;
    cp.transform.localScale = Vector3.Scale(r.transform.lossyScale, new Vector3(1 / gNew.transform.lossyScale.x, 1 / gNew.transform.lossyScale.y, 1 / gNew.transform.lossyScale.z));
    cp.GetComponent<MeshFilter>().sharedMesh = nm; var mats = new Material[nm.subMeshCount]; for (int i = 0; i < mats.Length; i++) mats[i] = newMat[tk]; cp.GetComponent<MeshRenderer>().sharedMaterials = mats;
    if (swapped < 4 || r.name.StartsWith("background")) log.Append(r.name + " old " + om.bounds.size.ToString("F2") + " new " + nm.bounds.size.ToString("F2") + " sub " + nm.subMeshCount + "; ");
    r.transform.SetParent(gOld.transform, true); swapped++; }
}
gOld.SetActive(false);
// 기본 = S1·S2·S3 켬, T1·T2 끔
foreach (var m in AssetDatabase.FindAssets("t:Material", new[] { D + "Mats" })) { var mt = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(m)); if (!mt.HasProperty("_S1")) continue;
  mt.SetFloat("_S1", 1); mt.SetFloat("_S2", 1); mt.SetFloat("_S3", 1); mt.SetFloat("_T1", 0); mt.SetFloat("_T2", 0); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
return "renderers " + rn + " · 재질 " + map.Count + " · 키트 " + haveKit + " · 건물 교체 " + swapped + "\n" + log;
