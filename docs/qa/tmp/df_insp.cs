var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb=new System.Text.StringBuilder(); sb.Append(s.path+" dirty="+s.isDirty+"\n");
foreach(var c in UnityEngine.Object.FindObjectsByType<ArcadeCarController>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 var rb=c.GetComponent<Rigidbody>(); sb.Append(c.name+" act="+c.gameObject.activeInHierarchy+" mass="+rb.mass+" pos="+c.transform.position+" scale="+c.transform.lossyScale+"\n");
 foreach(Transform ch in c.transform) sb.Append("  child "+ch.name+" act="+ch.gameObject.activeSelf+"\n");
 foreach(var m in c.GetComponents<MonoBehaviour>()) sb.Append("  comp "+m.GetType().Name+" en="+m.enabled+"\n");
 var bc=c.GetComponent<BoxCollider>(); if(bc!=null) sb.Append("  box c="+bc.center+" s="+bc.size+"\n");
 var so=new UnityEditor.SerializedObject(c); var it=so.GetIterator(); it.NextVisible(true);
 while(it.NextVisible(false)){ if(it.propertyType==UnityEditor.SerializedPropertyType.Float) sb.Append("   "+it.name+"="+it.floatValue+"\n"); else if(it.propertyType==UnityEditor.SerializedPropertyType.Enum||it.propertyType==UnityEditor.SerializedPropertyType.Boolean) sb.Append("   "+it.name+"="+(it.propertyType==UnityEditor.SerializedPropertyType.Enum?it.enumNames[it.enumValueIndex]:it.boolValue.ToString())+"\n"); }
}
foreach(var f in UnityEngine.Object.FindObjectsByType<FollowCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){ var so=new UnityEditor.SerializedObject(f); sb.Append("cam "+f.name+" preset="+(f.Preset?f.Preset.name:"null")+" posSmooth="+so.FindProperty("positionSmoothTime").floatValue+" yawSmooth="+so.FindProperty("yawSmoothTime").floatValue+" heightSmooth="+so.FindProperty("heightSmoothTime").floatValue+"\n"); }
foreach(var l in UnityEngine.Object.FindObjectsByType<CarVisualLean>(FindObjectsInactive.Include,FindObjectsSortMode.None)) sb.Append("lean on "+l.name+"\n");
foreach(var e in UnityEngine.Object.FindObjectsByType<EngineAudio>(FindObjectsInactive.Include,FindObjectsSortMode.None)) sb.Append("engine on "+e.name+" clip="+(e.GetComponent<AudioSource>().clip?e.GetComponent<AudioSource>().clip.name:"null")+"\n");
return sb.ToString();
