var sb=new System.Text.StringBuilder();
var mats=new System.Collections.Generic.HashSet<Material>();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 var t=r.transform; bool inTown=false; while(t!=null){ if(t.name=="TownV6_Gray"){inTown=true;break;} t=t.parent; }
 if(!inTown) continue; foreach(var m in r.sharedMaterials) if(m!=null) mats.Add(m); }
foreach(var m in mats){ sb.Append(m.name+" sh="+m.shader.name); int n=m.shader.GetPropertyCount(); for(int i=0;i<n;i++){ if(m.shader.GetPropertyType(i)==UnityEngine.Rendering.ShaderPropertyType.Color){ var nm=m.shader.GetPropertyName(i); sb.Append(" "+nm+"="+m.GetColor(nm).ToString("F2")); } } sb.AppendLine(); }
return sb.ToString();
