// TV6_Stone / TV6_Terrain 셰이더·속성 확인(텃밭 단색 재질 사본용)
var sb=new System.Text.StringBuilder();
foreach(var p in new[]{"Assets/Scenes/Map_TownV6_Mats/TV6_Stone.mat","Assets/Scenes/Map_TownV6_Mats/TV6_Terrain.mat","Assets/Scenes/Map_TownV6_Mats/TV6_RetainCap.mat"}){
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){sb.AppendLine(p+" none");continue;}
 var s=m.shader;sb.Append(p+" shader="+s.name+" :");
 for(int i=0;i<s.GetPropertyCount();i++){var n=s.GetPropertyName(i);var t=s.GetPropertyType(i);sb.Append(" "+n+"("+t+")=");if(t==UnityEngine.Rendering.ShaderPropertyType.Color)sb.Append(m.GetColor(n).ToString("F2"));else if(t==UnityEngine.Rendering.ShaderPropertyType.Texture)sb.Append(m.GetTexture(n)?m.GetTexture(n).name:"null");else if(t==UnityEngine.Rendering.ShaderPropertyType.Float||t==UnityEngine.Rendering.ShaderPropertyType.Range)sb.Append(m.GetFloat(n).ToString("F2"));}
 sb.AppendLine();}
return sb.ToString();
