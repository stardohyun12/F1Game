var sb=new System.Text.StringBuilder();
var seen=new System.Collections.Generic.Dictionary<string,int>();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 foreach(var m in r.sharedMaterials){ if(m==null) continue;
  Color c=Color.black; bool has=false; if(m.HasProperty("_BaseColor")){ c=m.GetColor("_BaseColor"); has=true; } else if(m.HasProperty("_Color")){ c=m.GetColor("_Color"); has=true; }
  if(has&&c.r>0.8f&&c.g>0.25f&&c.g<0.6f&&c.b<0.25f){
   var b=r.bounds; string k=m.name+" | "+r.name; string path=r.name; var t=r.transform.parent; int n=0; while(t!=null&&n<3){ path=t.name+"/"+path; t=t.parent; n++; }
   string key=m.name+" "+path; if(!seen.ContainsKey(key)){ seen[key]=1; sb.AppendLine(key+" act="+r.gameObject.activeInHierarchy+" en="+r.enabled+" c="+b.center.ToString("F0")+" s="+b.size.ToString("F0")); }
  } } }
return sb.ToString();
