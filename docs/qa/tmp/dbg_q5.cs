var c=new Vector3(93.8f,4f,-288f); string o="";
var all=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
var l=new System.Collections.Generic.List<(float,string)>();
foreach(var r in all){ if(!r.enabled) continue; var b=r.bounds; if(b.size.y<4f) continue; var dv=b.center-c; dv.y=0; float dist=dv.magnitude; if(dist>30f) continue;
 float yaw=Mathf.Atan2(dv.x,dv.z)*Mathf.Rad2Deg; l.Add((dist,r.name+" yaw="+yaw.ToString("F0")+" d="+dist.ToString("F1")+" size="+b.size.ToString("F1"))); }
l.Sort((a,b)=>a.Item1.CompareTo(b.Item1)); int n=0; foreach(var e in l){ o+=e.Item2+"\n"; if(++n>=14) break; }
return o;
