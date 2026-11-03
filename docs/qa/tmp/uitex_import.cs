var names = new[]{"paper","cream","slate","oak","walnut"};
var sb = new System.Text.StringBuilder();
foreach (var n in names) {
  var p = "Assets/Resources/LofiUI/" + n + ".png";
  var ti = UnityEditor.AssetImporter.GetAtPath(p) as UnityEditor.TextureImporter;
  if (ti == null) { sb.Append(n+":missing "); continue; }
  ti.textureType = UnityEditor.TextureImporterType.Default; ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false;
  ti.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; ti.isReadable = true; ti.npotScale = UnityEditor.TextureImporterNPOTScale.None; ti.wrapMode = TextureWrapMode.Repeat;
  ti.SaveAndReimport(); sb.Append(n+":ok ");
}
UnityEditor.AssetDatabase.Refresh();
return sb.ToString();
