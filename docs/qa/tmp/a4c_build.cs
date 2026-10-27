// 과제 1317 — Scene_A4c_Town = Scene_A4b_Town 복사.
//  1 집 6채 메쉬·텍스처를 art/export/scene-a4-houses-c(fbx+png, UV 바뀜)로 교체. 자리·빈 부모 회전 그대로(부모 house_* 아래 인스턴스만 바꿈)
//  2 집 +0.5m, 집 앞 구간 길 벽(Wall_L/R) 꼭대기를 집 바닥+0.1m 로 낮춤 → 낮은 시점에서 1층 문·창이 보이게
//    벽 메쉬는 SA4c 폴더에 복사본(A3·A4·A4b 안 바뀜). MeshCollider 도 같은 메쉬. 벽은 세로 판이라 레이캐스트로 안 잡혀 정점으로 찾는다
const string D = "Assets/Art/SceneA4c/", S = "Assets/Scenes/Scene_A4c_Town.unity"; const float RAISE = 0.5f, ABOVE = 0.1f;
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A4b_Town.unity", S);
AssetDatabase.DeleteAsset("Assets/Art/SceneA4c"); AssetDatabase.CreateFolder("Assets/Art", "SceneA4c"); foreach (var f in new[] { "Mats", "houses", "walls" }) AssetDatabase.CreateFolder("Assets/Art/SceneA4c", f);
string A = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/scene-a4-houses-c/")), P = Application.dataPath + "/Art/SceneA4c/houses/";
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(A + h + e, P + h + e, true);
AssetDatabase.Refresh();
foreach (var h in H) {
  var ti = (TextureImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".png"); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.SaveAndReimport();
  var mi = (ModelImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.SaveAndReimport(); }
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var log = new System.Text.StringBuilder();
var tg = root.GetComponent<SceneA3Toggles>(); var mats = new System.Collections.Generic.List<Material>(tg.materials);
var fronts = new System.Collections.Generic.List<(Transform t, Vector3 half)>();
foreach (Transform t in g) {
  var oldInst = t.GetChild(0); var oldMat = oldInst.GetComponentInChildren<MeshRenderer>().sharedMaterial;
  var m = new Material(oldMat) { name = "SA4c_" + t.name }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(D + "houses/" + t.name + ".png")); AssetDatabase.CreateAsset(m, D + "Mats/" + m.name + ".mat");
  mats.Remove(oldMat); mats.Add(m);
  var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + t.name + ".fbx"), t); inst.transform.localPosition = Vector3.zero;   // fbx 루트 축 회전 유지
  UnityEngine.Object.DestroyImmediate(oldInst.gameObject);
  foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
  foreach (var f in inst.GetComponentsInChildren<MeshFilter>()) { var mc = f.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = f.sharedMesh; }
  t.position += Vector3.up * RAISE;
  // 부모 로컬 경계(회전 전) — 정면 = 로컬 +Z
  var q = t.rotation; t.rotation = Quaternion.identity; var b = inst.GetComponentInChildren<Renderer>().bounds; foreach (var r in inst.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds); var lc = b.center - t.position; var hs = b.extents; t.rotation = q;
  fronts.Add((t, new Vector3(hs.x + 0.5f, 0, lc.z + hs.z)));
  log.Append(t.name + " size " + b.size.ToString("F1") + " floor " + t.position.y.ToString("F2") + "; "); }
tg.materials = mats.ToArray(); tg.Apply();
// ---- 길 벽 낮추기: 집 로컬 좌표에서 |x|≤폭/2+0.5, 정면 ~ 정면+9m 안의 정점 중 집 바닥+0.1 보다 높은 것을 그 높이로
foreach (var wn in new[] { "Wall_L", "Wall_R" }) {
  var w = root.Find("A3_M3/" + wn); if (!w) { log.Append("no " + wn + "; "); continue; }
  var mf = w.GetComponent<MeshFilter>(); var nm = UnityEngine.Object.Instantiate(mf.sharedMesh); nm.name = "a4c_" + wn; var v = nm.vertices; int moved = 0;
  for (int i = 0; i < v.Length; i++) { var wp = w.transform.TransformPoint(v[i]);
    foreach (var fr in fronts) { var lp = fr.t.InverseTransformPoint(wp); if (Mathf.Abs(lp.x) <= fr.half.x && lp.z >= fr.half.z - 1f && lp.z <= fr.half.z + 9f) {
      float top = fr.t.position.y + ABOVE; if (wp.y > top) { wp.y = top; v[i] = w.transform.InverseTransformPoint(wp); moved++; } } } }
  nm.vertices = v; nm.RecalculateBounds(); nm.RecalculateNormals(); AssetDatabase.CreateAsset(nm, D + "walls/" + nm.name + ".asset"); mf.sharedMesh = nm; var mc = w.GetComponent<MeshCollider>(); if (mc) mc.sharedMesh = nm;
  log.Append(wn + " verts lowered " + moved + "/" + v.Length + "; "); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();
