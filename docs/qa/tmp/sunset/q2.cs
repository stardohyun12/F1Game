var sb=new System.Text.StringBuilder();
var lr=Camera.main.GetComponent<LowResRenderTest>(); sb.AppendLine("lowres h="+lr.targetHeight+" en="+lr.enabled+" int="+lr.integerScale+" smooth="+lr.smooth+" msaa="+lr.msaa);
var sky=RenderSettings.skybox; sb.AppendLine("sky "+UnityEditor.AssetDatabase.GetAssetPath(sky)+" shader="+sky.shader.name);
var sh=sky.shader; for(int i=0;i<sh.GetPropertyCount();i++){ var pn=sh.GetPropertyName(i); var pt=sh.GetPropertyType(i); string v= pt==UnityEngine.Rendering.ShaderPropertyType.Color? sky.GetColor(pn).ToString() : (pt==UnityEngine.Rendering.ShaderPropertyType.Float||pt==UnityEngine.Rendering.ShaderPropertyType.Range? sky.GetFloat(pn).ToString(): pt.ToString()); sb.AppendLine("  "+pn+"="+v);}
var sea=GameObject.Find("TownV6_Gray/Sea").GetComponent<Renderer>().sharedMaterial; var s2=sea.shader; sb.AppendLine("sea shader "+s2.name+" "+UnityEditor.AssetDatabase.GetAssetPath(s2));
for(int i=0;i<s2.GetPropertyCount();i++){ var pn=s2.GetPropertyName(i); var pt=s2.GetPropertyType(i); string v= pt==UnityEngine.Rendering.ShaderPropertyType.Color? sea.GetColor(pn).ToString() : (pt==UnityEngine.Rendering.ShaderPropertyType.Float||pt==UnityEngine.Rendering.ShaderPropertyType.Range? sea.GetFloat(pn).ToString(): pt.ToString()); sb.AppendLine("  "+pn+"="+v);}
var seaMesh=GameObject.Find("TownV6_Gray/Sea").GetComponent<MeshFilter>().sharedMesh; sb.AppendLine("sea mesh verts="+seaMesh.vertexCount+" "+UnityEditor.AssetDatabase.GetAssetPath(seaMesh));
var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain"); foreach(var c in terr.GetComponents<Component>()) sb.AppendLine("terr comp "+c.GetType().Name);
var tr=terr.GetComponent<Renderer>(); if(tr) sb.AppendLine("terr bounds "+tr.bounds.min+" "+tr.bounds.max+" mat="+tr.sharedMaterial.name+" sh="+tr.sharedMaterial.shader.name);
var piers=GameObject.Find("TownV6_Gray/Piers"); int k=0; foreach(Transform t in piers.transform){ if(k++<12){ var r=t.GetComponentInChildren<Renderer>(); sb.AppendLine("pier "+t.name+" "+(r?r.bounds.center+" sz="+r.bounds.size:""));} }
return sb.ToString();
