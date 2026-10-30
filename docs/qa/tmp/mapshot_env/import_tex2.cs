// mapshot 2020 (v2): 임시 텍스처 5장(env-first-v2/textures)을 하나씩 복사→ImportAsset(동기). Env_*.mat 5개.
var sb = new System.Text.StringBuilder();
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Game_shot/F1/Assets")) return "FAIL: 사본 에디터가 아님";
const string D = "Assets/_TmpEnv";
var slots = new[] { "wall", "trim", "roof", "floor", "retain_stone" };
System.IO.Directory.CreateDirectory(Application.dataPath + "/_TmpEnv");
foreach (var s in slots) {
  System.IO.File.Copy("D:/Github/F1Game/art/export/env-first-v2/textures/" + s + ".png", Application.dataPath + "/_TmpEnv/" + s + ".png", true);
  AssetDatabase.ImportAsset(D + "/" + s + ".png", ImportAssetOptions.ForceSynchronousImport);
  var ti = (TextureImporter)AssetImporter.GetAtPath(D + "/" + s + ".png");
  ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false;
  ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 2048;
  ti.SaveAndReimport();
  var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(D + "/" + s + ".png"); sb.Append(s + " " + (tex ? tex.width + "x" + tex.height : "null") + "\n");
}
var tmpl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Library/Buildings/house_home/M_house_home_house_home.mat");
if (!tmpl) return "FAIL tmpl";
foreach (var s in slots) {
  var m = new Material(tmpl) { name = "Env_" + s }; var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(D + "/" + s + ".png");
  m.SetTexture("_BaseMap", tex); m.SetColor("_Tint", Color.white); m.SetFloat("_Sat", 1f); m.SetFloat("_A7Lit", 1f);
  AssetDatabase.CreateAsset(m, D + "/Env_" + s + ".mat"); sb.Append(s + ": " + m.shader.name + "\n");
}
return sb.ToString();
