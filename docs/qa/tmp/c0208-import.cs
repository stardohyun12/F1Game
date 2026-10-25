// 과제 0208: 차 아틀라스를 세계 아틀라스(LofiAtlas 등)와 같은 임포트로 — Point · mip 없음 · 무압축(RGB24). SPEC: 512² Point/Nearest.
var sb = new System.Text.StringBuilder();
foreach (var w in new[] { "A", "C" })
{
  var path = "Assets/Art/BayLoop/Car/" + w + "_Atlas.png";
  var ti = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
  ti.maxTextureSize = 512; ti.sRGBTexture = true; ti.wrapMode = TextureWrapMode.Clamp;
  ti.SaveAndReimport();
  var t = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  sb.Append(w + ": " + t.width + " " + t.format + " " + t.filterMode + " mips=" + t.mipmapCount + "\n");
}
UnityEditor.SessionState.SetString("c.tag", "A_after1");
return sb.ToString();
