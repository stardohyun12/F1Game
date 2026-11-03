var sb=new System.Text.StringBuilder();
var c=new Vector3(185,0,-61);float R=68;
var top=GameObject.Find("TownV6_Gray").transform;
foreach(Transform ch in top.Find("Buildings"))sb.AppendLine("Buildings/"+ch.name+" kids="+ch.childCount);
var cnt=new System.Collections.Generic.Dictionary<string,int>();
foreach(var rr in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){
 if(!rr.enabled||!rr.gameObject.activeInHierarchy)continue;var b=rr.bounds;
 float dx=Mathf.Max(0,Mathf.Max(b.min.x-c.x,c.x-b.max.x)),dz=Mathf.Max(0,Mathf.Max(b.min.z-c.z,c.z-b.max.z));if(new Vector2(dx,dz).magnitude>R)continue;
 if(b.size.x>200)continue;
 var p=rr.transform;string path=p.name;int k=0;while(p.parent!=null&&k<3){p=p.parent;path=p.name+"/"+path;k++;}
 string key=path.Length>90?path.Substring(0,90):path;
 if(rr.name.StartsWith("ROAD")){key="ROAD* under "+rr.transform.parent.name;}
 if(!cnt.ContainsKey(key))cnt[key]=0;cnt[key]++;
 if(!rr.name.StartsWith("ROAD")&&cnt[key]==1)sb.AppendLine("R "+key+" mat="+(rr.sharedMaterial?rr.sharedMaterial.name:"null")+" b="+b.min.ToString("F1")+".."+b.max.ToString("F1"));
}
foreach(var kv in cnt)if(kv.Key.StartsWith("ROAD"))sb.AppendLine(kv.Key+" n="+kv.Value);
foreach(var g in UnityEditor.AssetDatabase.FindAssets("t:Material",new[]{"Assets/Scenes/Map_TownV6_Mats","Assets/Art"})){var p=UnityEditor.AssetDatabase.GUIDToAssetPath(g);var l=p.ToLower();if(l.Contains("stone")||l.Contains("wall")||l.Contains("rock")||l.Contains("retain")||l.Contains("cobble"))sb.AppendLine("MAT "+p);}
return sb.ToString();
