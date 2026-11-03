var sb=new System.Text.StringBuilder();
var gray=GameObject.Find("TownV6_Gray"); foreach(Transform t in gray.transform){ sb.AppendLine("Gray/"+t.name+" ch="+t.childCount+" "+t.position);}
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){ var nm=r.name.ToLower(); if(nm.Contains("sea")||nm.Contains("water")||nm.Contains("ocean")) sb.AppendLine("R "+r.name+" par="+(r.transform.parent?r.transform.parent.name:"")+" b="+r.bounds.center+" sz="+r.bounds.size+" mat="+(r.sharedMaterial?r.sharedMaterial.name+"|"+r.sharedMaterial.shader.name+"|"+UnityEditor.AssetDatabase.GetAssetPath(r.sharedMaterial):""));}
var L=GameObject.Find("Directional Light").GetComponent<Light>(); sb.AppendLine("Sun rot="+L.transform.eulerAngles+" col="+L.color+" int="+L.intensity+" shadows="+L.shadows);
sb.AppendLine("Fog="+RenderSettings.fog+" "+RenderSettings.fogColor+" mode="+RenderSettings.fogMode+" "+RenderSettings.fogStartDistance+"-"+RenderSettings.fogEndDistance+" dens="+RenderSettings.fogDensity);
sb.AppendLine("amb mode="+RenderSettings.ambientMode+" sky="+RenderSettings.ambientSkyColor+" eq="+RenderSettings.ambientEquatorColor+" gr="+RenderSettings.ambientGroundColor+" light="+RenderSettings.ambientLight+" skybox="+(RenderSettings.skybox?RenderSettings.skybox.name:"null")+" sun="+(RenderSettings.sun?RenderSettings.sun.name:"null"));
var cam=Camera.main; foreach(var c in cam.GetComponents<Component>()) sb.AppendLine("Cam comp "+c.GetType().Name);
sb.AppendLine("cam clear="+cam.clearFlags+" bg="+cam.backgroundColor+" fov="+cam.fieldOfView+" far="+cam.farClipPlane);
var pl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>(); sb.AppendLine("PCLook="+(pl?pl.name+" look="+pl.look+" h="+pl.lowResHeight:"none"));
foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)){ var tn=m.GetType().Name; if(tn.Contains("Wind")||tn.Contains("Mood")||tn.Contains("Shade")||tn.Contains("Look")||tn.Contains("LowRes")||tn.Contains("Audio")||tn.Contains("Sky")) sb.AppendLine("MB "+tn+" on "+m.name);}
var a7=GameObject.Find("A7ShadeGlobals"); foreach(var c in a7.GetComponents<Component>()) sb.AppendLine("A7 "+c.GetType().Name);
var of=GameObject.Find("OnFoot_HomeGarage"); foreach(Transform t in of.transform) sb.AppendLine("OF/"+t.name+" "+t.position);
return sb.ToString();
