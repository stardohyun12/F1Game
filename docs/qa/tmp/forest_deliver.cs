// artlive 20261031-2009 숲 나무 F1·F2 납품: art/export/forest/ → F1/Assets/Art/Library/Trees/forest_F<n>/ (F1Studio 와 같은 폴더·정션)
// forest_F<n>.fbx(LOD0) + forest_F<n>_lod1.fbx → 재질 2 (SA7_house_shop 사본, 공유 .mat 은 읽기만) → 프리팹 forest_F<n> (LODGroup: LOD0 0.12 / LOD1 0.02 / 컬)
if (Application.isPlaying) return "FAIL: Play 끄고";
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio 만";
string src = System.IO.Path.GetFullPath(Application.dataPath + "/../../art/export/forest/");
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
var log = new System.Text.StringBuilder();
foreach (var n in new[] { "F1", "F2" }) {
  string dir = "Assets/Art/Library/Trees/forest_" + n; System.IO.Directory.CreateDirectory(dir);
  foreach (var nm in new[] { "forest_" + n, "forest_" + n + "_lod1" }) {
    System.IO.File.Copy(src + nm + ".fbx", dir + "/" + nm + ".fbx", true); System.IO.File.Copy(src + nm + ".png", dir + "/" + nm + ".png", true); }
  AssetDatabase.Refresh();
  var mats = new Material[2]; int mi_ = 0;
  foreach (var nm in new[] { "forest_" + n, "forest_" + n + "_lod1" }) {
    var mi = (ModelImporter)AssetImporter.GetAtPath(dir + "/" + nm + ".fbx");
    mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport();
    var ti = (TextureImporter)AssetImporter.GetAtPath(dir + "/" + nm + ".png");
    ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
    ti.alphaIsTransparency = true; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 2048; ti.SaveAndReimport();
    var mp = dir + "/M_" + nm + ".mat"; AssetDatabase.DeleteAsset(mp);
    var m = new Material(tmpl) { name = "M_" + nm }; m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + nm + ".png")); m.SetColor("_Tint", Color.white);
    m.SetFloat("_A7Lit", 1); m.SetFloat("_Sat", 1); m.SetFloat("_Cutoff", 0.5f); if (m.HasProperty("_SN")) m.SetFloat("_SN", 0); if (m.HasProperty("_Blotch")) m.SetFloat("_Blotch", 0);
    AssetDatabase.CreateAsset(m, mp); mats[mi_++] = m; }
  AssetDatabase.SaveAssets();
  var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/forest_" + n + ".fbx")); PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
  root.name = "forest_" + n; root.transform.position = Vector3.zero; root.transform.rotation = Quaternion.identity; root.transform.localScale = Vector3.one;
  var r0 = root.GetComponentsInChildren<Renderer>(true);
  foreach (var r in r0) { var ms = r.sharedMaterials; for (int j = 0; j < ms.Length; j++) ms[j] = mats[0]; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
  var lod = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/forest_" + n + "_lod1.fbx")); PrefabUtility.UnpackPrefabInstance(lod, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
  lod.name = "LOD1"; lod.transform.SetParent(root.transform, false);
  var r1 = lod.GetComponentsInChildren<Renderer>(true);
  foreach (var r in r1) { var ms = r.sharedMaterials; for (int j = 0; j < ms.Length; j++) ms[j] = mats[1]; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
  var lg = root.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(0.12f, r0), new LOD(0.02f, r1) }); lg.RecalculateBounds();
  string pp = dir + "/forest_" + n + ".prefab"; AssetDatabase.DeleteAsset(pp);
  var pf = PrefabUtility.SaveAsPrefabAsset(root, pp); UnityEngine.Object.DestroyImmediate(root);
  int t0 = 0, t1 = 0; foreach (var mf in pf.GetComponentsInChildren<MeshFilter>(true)) { int tr = mf.sharedMesh.triangles.Length / 3; if (mf.transform.IsChildOf(pf.transform.Find("LOD1"))) t1 += tr; else t0 += tr; }
  var names = new System.Collections.Generic.List<string>(); foreach (Transform ch in pf.transform) names.Add(ch.name);
  log.Append(n + " LOD0 " + t0 + "tri · LOD1 " + t1 + "tri · 자식 " + string.Join(",", names) + "; ");
}
AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
return "OK " + log;
