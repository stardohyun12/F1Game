var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder();
UnityEngine.GameObject F(string n){ foreach(var r in s.GetRootGameObjects()){ foreach(var t in r.GetComponentsInChildren<UnityEngine.Transform>(true)) if(t.name==n) return t.gameObject; } return null; }
foreach (var n in new[]{"TUT_Garage_PitCrew","TUT_Room","TUT_PackingSpot","TUT_MartOwner","TUT_Mechanic","TUT_CarStart"}) {
  var g = F(n); sb.Append("## "+n+"\n");
  foreach (UnityEngine.Transform c in g.transform) { var mr=c.GetComponent<UnityEngine.Renderer>(); var col=c.GetComponent<UnityEngine.Collider>(); sb.Append("  "+c.name+(c.gameObject.activeSelf?"":" [off]")+" ch="+c.childCount+(mr?" R":"")+(col?" C":"")+" "+string.Join(",", System.Linq.Enumerable.Select(c.GetComponents<UnityEngine.Component>(), k=>k==null?"MISSING":k.GetType().Name))+"\n"); }
}
// references from scripts
foreach (var r in s.GetRootGameObjects()) foreach (var mb in r.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) {
  if (mb==null) continue; var ns = mb.GetType().Namespace; if (ns!=null && (ns.StartsWith("Unity")||ns.StartsWith("TMPro"))) continue;
  var so = new UnityEditor.SerializedObject(mb); var it = so.GetIterator();
  while (it.Next(true)) { if (it.propertyType==UnityEditor.SerializedPropertyType.ObjectReference && it.objectReferenceValue!=null) { var o=it.objectReferenceValue; UnityEngine.Transform tt = o is UnityEngine.Component cc ? cc.transform : (o is UnityEngine.GameObject gg ? gg.transform : null); if (tt==null) continue; var p=tt; string chain=""; while(p!=null){ chain=p.name+"/"+chain; p=p.parent; } if (System.Text.RegularExpressions.Regex.IsMatch(chain,"TUT_|Garage|Q[1-4]_|Tutorial")) sb.Append("REF "+mb.GetType().Name+"@"+mb.name+"."+it.propertyPath+" -> "+chain+"\n"); } }
}
return sb.ToString();
