UnityEditor.AssetDatabase.Refresh();
var sb=new System.Text.StringBuilder();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){
 var b=r.bounds; if(Mathf.Abs(b.center.x+9f)>30f||Mathf.Abs(b.center.z+285f)>22f) continue; if(b.max.y<-0.5f||b.min.y>6f) continue;
 string path=r.name; var t=r.transform.parent; int k=0; while(t!=null&&k<2){ path=t.name+"/"+path; t=t.parent; k++; }
 var cl=r.GetComponent<Collider>();
 sb.AppendLine(path+" c="+b.center.ToString("F1")+" s="+b.size.ToString("F1")+" col="+(cl!=null));
}
return sb.ToString();
