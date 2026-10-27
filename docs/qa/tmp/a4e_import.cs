// 과제 1429 1단계: art/export/scene-a4-houses-e (fbx+png) → Assets/Art/SceneA4e/houses, 임포트 설정(Point·무압축·밉맵 끔, 머티리얼 임포트 없음), fbx 자식 이름 나열
const string D = "Assets/Art/SceneA4e/";
AssetDatabase.DeleteAsset("Assets/Art/SceneA4e"); AssetDatabase.CreateFolder("Assets/Art", "SceneA4e"); foreach (var f in new[] { "Mats", "houses" }) AssetDatabase.CreateFolder("Assets/Art/SceneA4e", f);
string A = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/export/scene-a4-houses-e/")), P = Application.dataPath + "/Art/SceneA4e/houses/";
string[] H = { "house_narrow_a", "house_narrow_b", "house_shop", "house_slope", "house_warehouse", "house_cafe" };
foreach (var h in H) foreach (var e in new[] { ".fbx", ".png" }) System.IO.File.Copy(A + h + e, P + h + e, true);
AssetDatabase.Refresh();
var sb = new System.Text.StringBuilder();
foreach (var h in H) {
  var ti = (TextureImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".png"); ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 8192; ti.SaveAndReimport();
  var mi = (ModelImporter)AssetImporter.GetAtPath(D + "houses/" + h + ".fbx"); mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.SaveAndReimport();
  var go = AssetDatabase.LoadAssetAtPath<GameObject>(D + "houses/" + h + ".fbx"); sb.Append(h + ": ");
  foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) { var b = mf.sharedMesh.bounds; sb.Append(mf.name + "[" + mf.sharedMesh.vertexCount + "v sub" + mf.sharedMesh.subMeshCount + " " + b.size.ToString("F1") + "] "); }
  sb.Append("\n"); }
return sb.ToString();
