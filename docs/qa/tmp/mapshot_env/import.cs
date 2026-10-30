// mapshot 1830: 사본 에디터 전용 임시 임포트 Assets/_TmpEnv (끝에 삭제). 씬 저장 없음.
var sb = new System.Text.StringBuilder();
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Game_shot/F1/Assets")) return "FAIL: 사본 에디터가 아님 " + Application.dataPath;
if (Application.isPlaying) return "FAIL: play";
const string D = "Assets/_TmpEnv";
string repo = "D:/Github/F1Game/";
System.IO.Directory.CreateDirectory(Application.dataPath + "/_TmpEnv");
var man = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(repo + "art/export/env-first/manifest.json"));
foreach (var b in man["buildings"]) { string n = (string)b["name"]; System.IO.File.Copy(repo + "art/export/env-first/" + n + ".fbx", Application.dataPath + "/_TmpEnv/" + n + ".fbx", true); }
foreach (var s in new[] { "wall", "trim", "roof", "floor" }) System.IO.File.Copy(repo + "art/export/kit-shared-v2/" + s + ".png", Application.dataPath + "/_TmpEnv/" + s + ".png", true);
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
foreach (var s in new[] { "wall", "trim", "roof", "floor" }) {
  string tp = D + "/" + s + ".png"; var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
  ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false;
  ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 2048; ti.SaveAndReimport();
}
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Library/Buildings/house_home/M_house_home_house_home.mat");
foreach (var s in new[] { "wall", "trim", "roof", "floor" }) {
  var m = new Material(tmpl) { name = "Env_" + s }; var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(D + "/" + s + ".png");
  m.SetTexture("_BaseMap", tex); m.SetColor("_Tint", Color.white); m.SetFloat("_Sat", 1f); m.SetFloat("_A7Lit", 1f);
  AssetDatabase.CreateAsset(m, D + "/Env_" + s + ".mat"); sb.Append(s + ": " + m.shader.name + " tex=" + tex.width + "x" + tex.height + "\n");
}
foreach (var b in man["buildings"]) { string fp = D + "/" + (string)b["name"] + ".fbx"; var mi = (ModelImporter)AssetImporter.GetAtPath(fp);
  mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab; mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport();
  var go = AssetDatabase.LoadAssetAtPath<GameObject>(fp); var names = new System.Collections.Generic.HashSet<string>(); int tris = 0;
  foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { foreach (var m in r.sharedMaterials) names.Add(m ? m.name : "null"); var mf = r.GetComponent<MeshFilter>(); if (mf) tris += mf.sharedMesh.triangles.Length / 3; }
  sb.Append(fp + " mats=" + string.Join(",", names) + " tris=" + tris + "\n"); }
return sb.ToString();
