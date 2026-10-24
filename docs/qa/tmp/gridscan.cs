var sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene(); var b=new Bounds(Vector3.zero,Vector3.zero); bool first=true;
var sb=new System.Text.StringBuilder(); sb.Append("scene="+sc.name+" playing="+Application.isPlaying+"\n");
var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); sb.Append("car="+(car!=null?car.transform.position.ToString():"null")+"\n");
foreach(var go in sc.GetRootGameObjects()){ sb.Append(go.name+" ch="+go.transform.childCount+" pos="+go.transform.position+"\n"); }
foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){ if(first){b=c.bounds;first=false;} else b.Encapsulate(c.bounds);}
sb.Append("collider bounds min="+b.min+" max="+b.max+"\n");
var g=GameObject.Find("Ground"); if(g!=null){var cc=g.GetComponent<Collider>(); sb.Append("Ground "+g.transform.position+" scale="+g.transform.localScale+" "+(cc!=null?cc.bounds.ToString():"nocol")+" layer="+g.layer+"\n");}
return sb.ToString();
