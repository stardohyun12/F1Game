var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var sv = UnityEditor.SceneView.lastActiveSceneView; string cam = sv!=null ? sv.camera.transform.position.ToString("F2")+" rot "+sv.camera.transform.eulerAngles.ToString("F1")+" fov "+sv.camera.fieldOfView.ToString("F1")+" piv "+sv.pivot.ToString("F1")+" size "+sv.size.ToString("F1") : "nosv";
var top = UnityEngine.GameObject.Find("TownV6_Gray"); string t = "";
if (top!=null) { var terr = top.transform.Find("TownV6_Terrain"); var mf = terr.GetComponent<UnityEngine.MeshFilter>(); var m = mf.sharedMesh; t = m.name+" v"+m.vertexCount+" tri"+(m.triangles.Length/3)+" b "+terr.GetComponent<UnityEngine.MeshRenderer>().bounds.ToString("F0");
 var b = top.transform.Find("Buildings"); t += " Buildings active="+(b!=null && b.gameObject.activeSelf)+" ch="+(b!=null?b.childCount:0);
 var bb = new UnityEngine.Bounds(); bool f=false; foreach (UnityEngine.Transform ch in top.transform.Find("RoadsV2")) { var r = ch.GetComponent<UnityEngine.Renderer>(); if (r==null||!ch.gameObject.activeInHierarchy) continue; if(!f){bb=r.bounds;f=true;} else bb.Encapsulate(r.bounds);} t += " roads b "+bb.ToString("F0");
 if (b!=null) { var ob = new UnityEngine.Bounds(); f=false; foreach (var r in b.GetComponentsInChildren<UnityEngine.Renderer>(true)) { if(!f){ob=r.bounds;f=true;} else ob.Encapsulate(r.bounds);} t += " osm b "+ob.ToString("F0"); foreach (UnityEngine.Transform ch in b) t += " ["+ch.name+" col="+(ch.GetComponent<UnityEngine.Collider>()!=null)+"]"; }
}
return s+" | "+cam+" | "+t;
