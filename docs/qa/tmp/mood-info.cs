var sb=new System.Text.StringBuilder();
sb.Append("scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" playing="+Application.isPlaying+"\n");
foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if(l.type==LightType.Directional) sb.Append("dir "+l.name+" rot="+l.transform.eulerAngles+" col="+l.color+" int="+l.intensity+" shadows="+l.shadows+" str="+l.shadowStrength+"\n");
sb.Append("ambMode="+RenderSettings.ambientMode+" amb="+RenderSettings.ambientLight+" sky="+RenderSettings.ambientSkyColor+" eq="+RenderSettings.ambientEquatorColor+" gnd="+RenderSettings.ambientGroundColor+" ambInt="+RenderSettings.ambientIntensity+"\n");
sb.Append("fog="+RenderSettings.fog+" mode="+RenderSettings.fogMode+" col="+RenderSettings.fogColor+" dens="+RenderSettings.fogDensity+" start="+RenderSettings.fogStartDistance+" end="+RenderSettings.fogEndDistance+"\n");
sb.Append("skybox="+(RenderSettings.skybox!=null?RenderSettings.skybox.name+"/"+RenderSettings.skybox.shader.name:"null")+"\n");
foreach(var mc in UnityEngine.Object.FindObjectsByType<MoodController>(FindObjectsSortMode.None)){ var so=new UnityEditor.SerializedObject(mc); var p=so.FindProperty("profile").objectReferenceValue; sb.Append("MoodController on "+mc.name+" enabled="+mc.enabled+" profile="+(p!=null?UnityEditor.AssetDatabase.GetAssetPath(p):"null")+" applyOnAwake="+so.FindProperty("applyOnAwake").boolValue+"\n"); }
foreach(var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None)){ sb.Append("Volume "+v.name+" global="+v.isGlobal+" prio="+v.priority+" w="+v.weight+" en="+v.enabled+"/"+v.gameObject.activeInHierarchy+" profile="+(v.sharedProfile!=null?UnityEditor.AssetDatabase.GetAssetPath(v.sharedProfile):"null")+" comps=");
 if(v.sharedProfile!=null) foreach(var c in v.sharedProfile.components) sb.Append(c.GetType().Name+(c.active?"":"(off)")+",");
 sb.Append("\n"); }
foreach(var g in UnityEditor.AssetDatabase.FindAssets("t:MoodProfile")) sb.Append("moodAsset "+UnityEditor.AssetDatabase.GUIDToAssetPath(g)+"\n");
// 가드레일 머티리얼
var mats=new System.Collections.Generic.Dictionary<string,int>();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){ var n=r.gameObject.name.ToLower(); if(n.Contains("guard")||n.Contains("rail")||n.Contains("barrier")) foreach(var m in r.sharedMaterials) if(m!=null){ var k=r.gameObject.name.Split('_')[0]+" -> "+m.name+" ("+UnityEditor.AssetDatabase.GetAssetPath(m)+") col="+(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():"?"); mats[k]=(mats.ContainsKey(k)?mats[k]:0)+1; } }
foreach(var kv in mats) sb.Append("rail "+kv.Key+" x"+kv.Value+"\n");
return sb.ToString();
