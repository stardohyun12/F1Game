var sb = new System.Text.StringBuilder();
foreach (var g in new[]{"_PCLook","_PCKeep","_PCMip","_A7Mid"}) sb.Append(g).Append("=").Append(Shader.GetGlobalFloat(g)).Append(" ");
sb.Append("_A7Shade=").Append(Shader.GetGlobalVector("_A7Shade")).Append(" _A7T=").Append(Shader.GetGlobalVector("_A7T")).Append(" _PCDusk=").Append(Shader.GetGlobalVector("_PCDusk")).Append("\n");
foreach (var p in new[]{"Assets/Art/Library/Buildings/room/M_room_room.mat","Assets/Art/Library/Buildings/house_home/M_house_home_house_home.mat"}) {
  var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(p);
  sb.Append(p).Append("\n");
  var s = m.shader;
  for (int i = 0; i < s.GetPropertyCount(); i++) {
    var n = s.GetPropertyName(i); var t = s.GetPropertyType(i);
    if (t == UnityEngine.Rendering.ShaderPropertyType.Float || t == UnityEngine.Rendering.ShaderPropertyType.Range) sb.Append(n).Append("=").Append(m.GetFloat(n)).Append(" ");
    else if (t == UnityEngine.Rendering.ShaderPropertyType.Color) sb.Append(n).Append("=").Append(m.GetColor(n)).Append(" ");
    else if (t == UnityEngine.Rendering.ShaderPropertyType.Vector) sb.Append(n).Append("=").Append(m.GetVector(n)).Append(" ");
  }
  sb.Append("\n");
  var tex = m.GetTexture("_BaseMap"); var ti = UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(tex)) as UnityEditor.TextureImporter;
  sb.Append("import max=").Append(ti.maxTextureSize).Append(" mip=").Append(ti.mipmapEnabled).Append(" comp=").Append(ti.textureCompression).Append(" filter=").Append(ti.filterMode).Append("\n");
}
var cams = UnityEngine.Object.FindObjectsByType<LowResRenderTest>(FindObjectsSortMode.None);
foreach (var c in cams) sb.Append("LRT ").Append(c.name).Append(" h=").Append(c.targetHeight).Append(" smooth=").Append(c.smooth).Append(" en=").Append(c.enabled).Append(" fov=").Append(c.GetComponent<Camera>().fieldOfView).Append("\n");
return sb.ToString();
