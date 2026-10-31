var m = typeof(OnFootRouteBot).GetMethod("RoadPath", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var args = new object[]{ new Vector3(245.33f,24.66f,-61.68f), new Vector3(93.79f,4.55f,-288.05f), null };
var path = (System.Collections.Generic.List<Vector3>)m.Invoke(null,args);
if(path==null) return "null "+args[2];
string s="n="+path.Count+"\n"; var stuck=new Vector3(68.97f,12.39f,-63.47f);
int bi=0; float bd=1e9f; for(int i=0;i<path.Count;i++){float d=Vector3.Distance(path[i],stuck); if(d<bd){bd=d;bi=i;}}
s+="nearest idx="+bi+" d="+bd+"\n";
for(int i=Mathf.Max(0,bi-8);i<Mathf.Min(path.Count,bi+10);i++) s+=i+": "+path[i].ToString("F1")+"\n";
// raycast ahead from stuck pos toward path[bi+3]
var tgt=path[Mathf.Min(path.Count-1,bi+3)];
s+="ray hits: ";
foreach(var h in Physics.RaycastAll(stuck+Vector3.up*0.6f,(tgt-stuck).normalized,15f)) s+=h.collider.name+"@"+h.distance.ToString("F1")+" ";
return s;
