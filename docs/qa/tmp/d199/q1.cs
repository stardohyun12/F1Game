var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder(); sb.Append("active=" + s.path + " dirty=" + s.isDirty + "\n");
System.Action<UnityEngine.Transform,int> walk=null;
walk=(t,d)=>{ var n=t.name; bool hit = System.Text.RegularExpressions.Regex.IsMatch(n,"TUT_|Garage|ArtSlot|^Q[1-4]|Room|Depot|PostOffice|Cutscene|PitCrew|Workshop|Tutorial");
 bool pf = UnityEditor.PrefabUtility.IsPartOfPrefabInstance(t);
 if(hit){ var comps=string.Join(",", System.Linq.Enumerable.Select(t.GetComponents<UnityEngine.Component>(), c=> c==null?"MISSING":c.GetType().Name)); sb.Append(new string(' ',d*2)+n+(t.gameObject.activeSelf?"":" [off]")+" pos="+t.position.ToString("F0")+" ch="+t.childCount+" {"+comps+"}"+(pf?" PREFAB:"+UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t):"")+"\n"); }
 if(d<3 || (hit && !pf)) foreach(UnityEngine.Transform c in t) walk(c,d+1); };
foreach(var r in s.GetRootGameObjects()) walk(r.transform,0);
return sb.ToString();
