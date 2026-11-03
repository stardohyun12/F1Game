var sb=new System.Text.StringBuilder();
var c=new Vector3(185,0,-61);float R=68;
var top=GameObject.Find("TownV6_Gray").transform;
var terr=top.Find("TownV6_Terrain");var mf=terr.GetComponent<MeshFilter>();
sb.AppendLine("terrain mesh="+mf.sharedMesh.name+" path="+UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh)+" verts="+mf.sharedMesh.vertexCount+" pos="+terr.position+" rot="+terr.rotation.eulerAngles+" scale="+terr.lossyScale+" mat="+terr.GetComponent<MeshRenderer>().sharedMaterial.name);
var v=mf.sharedMesh.vertices;int n=0;float ymin=999,ymax=-999;
for(int i=0;i<v.Length;i++){var w=terr.TransformPoint(v[i]);var d=new Vector2(w.x-c.x,w.z-c.z).magnitude;if(d<R){n++;ymin=Mathf.Min(ymin,w.y);ymax=Mathf.Max(ymax,w.y);}}
sb.AppendLine("verts in R68="+n+" y "+ymin+".."+ymax);
var t=mf.sharedMesh.triangles;var el=new System.Collections.Generic.List<float>();
for(int i=0;i<t.Length;i+=3){var a=terr.TransformPoint(v[t[i]]);if(new Vector2(a.x-c.x,a.z-c.z).magnitude>R)continue;var b=terr.TransformPoint(v[t[i+1]]);el.Add(new Vector2(a.x-b.x,a.z-b.z).magnitude);}
el.Sort();if(el.Count>0)sb.AppendLine("edge len p10="+el[el.Count/10]+" p50="+el[el.Count/2]+" p90="+el[el.Count*9/10]);
foreach(Transform ch in top)sb.AppendLine("top child: "+ch.name+" kids="+ch.childCount+" active="+ch.gameObject.activeSelf);
var scn=top.gameObject.scene;
foreach(var r in scn.GetRootGameObjects()){sb.AppendLine("ROOT "+r.name+" kids="+r.transform.childCount);}
foreach(var r in scn.GetRootGameObjects())foreach(var tr in r.GetComponentsInChildren<Transform>(false)){
 if(tr.name.StartsWith("ROAD")||tr.parent==null)continue;
 bool pref=UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(tr.gameObject);
 bool grp=tr.parent.name.EndsWith("Buildings");
 if(!pref&&!grp)continue;
 var d=new Vector2(tr.position.x-c.x,tr.position.z-c.z).magnitude;if(d>R)continue;
 Bounds bb=new Bounds(tr.position,Vector3.zero);bool any=false;foreach(var rr in tr.GetComponentsInChildren<Renderer>(false)){if(!any){bb=rr.bounds;any=true;}else bb.Encapsulate(rr.bounds);}
 sb.AppendLine((pref?"PREF ":"GRP ")+tr.parent.name+"/"+tr.name+" pos="+tr.position.ToString("F2")+" yaw="+tr.eulerAngles.y.ToString("F0")+" d="+d.ToString("F1")+" bmin="+bb.min.ToString("F1")+" bmax="+bb.max.ToString("F1")+(pref?" src="+UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(tr.gameObject)):""));
}
return sb.ToString();
