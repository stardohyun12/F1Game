var s=""; foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(x=>x.name=="DetourRoad")){ s+="DetourRoad pos="+go.position.ToString("F1")+" | ";
 foreach(var c in go.GetComponentsInChildren<Collider>()){ var r=c.GetComponent<Renderer>(); s+=c.name+" "+c.GetType().Name+" b.min"+c.bounds.min.ToString("F1")+" max"+c.bounds.max.ToString("F1")+" rend="+(r!=null&&r.enabled)+(r!=null?" rb.min"+r.bounds.min.ToString("F1")+" rmax"+r.bounds.max.ToString("F1"):"")+"; "; } }
return s;
