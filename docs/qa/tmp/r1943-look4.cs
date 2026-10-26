PitCrewLookMenu.Set(4);
var sb = new System.Text.StringBuilder("look " + PitCrewLookMenu.Current + "\n");
foreach (var g in AssetDatabase.FindAssets("_retro t:Texture2D", new[] { "Assets/Art" })) {
  var p = AssetDatabase.GUIDToAssetPath(g); var ti = AssetImporter.GetAtPath(p) as TextureImporter;
  sb.Append(System.IO.Path.GetFileName(p) + " " + ti.filterMode + " mip=" + ti.mipmapEnabled + " " + ti.textureCompression + "\n"); }
var used = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null).Distinct().ToList();
sb.Append("scene mats w/ tex " + used.Count + " retro " + used.Count(m => AssetDatabase.GetAssetPath(m.GetTexture("_MainTex")).EndsWith("_retro.png")) + "\n");
foreach (var m in used.Where(m => !AssetDatabase.GetAssetPath(m.GetTexture("_MainTex")).EndsWith("_retro.png"))) sb.Append(" nonretro " + m.name + " " + AssetDatabase.GetAssetPath(m.GetTexture("_MainTex")) + "\n");
return sb.ToString();
