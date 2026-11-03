var sb=new System.Text.StringBuilder();
foreach(var p in new[]{"Assets/Scenes/Map_TownV6_Mats/TV6_Stone.mat","Assets/Scenes/Map_TownV6_Mats/TV6_Wall.mat","Assets/Scenes/Map_TownV6_Mats/TV6_DeadEndWall.mat","Assets/Art/SceneA7/Mats/SA7_stonewall.mat","Assets/Scenes/Map_TownV6_Mats/TV6_Terrain.mat"}){
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){sb.AppendLine("no "+p);continue;}
 sb.Append(p+" shader="+m.shader.name+" tex="+(m.mainTexture?UnityEditor.AssetDatabase.GetAssetPath(m.mainTexture):"none"));
 foreach(var n in m.GetTexturePropertyNames()){var t=m.GetTexture(n);if(t)sb.Append(" "+n+"="+t.name);}
 if(m.HasProperty("_Tint"))sb.Append(" tint="+m.GetColor("_Tint"));if(m.HasProperty("_BaseColor"))sb.Append(" base="+m.GetColor("_BaseColor"));
 int users=0;foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var sm in r.sharedMaterials)if(sm==m)users++;
 sb.AppendLine(" sceneUsers="+users);}
var top=GameObject.Find("TownV6_Gray").transform;
foreach(Transform ch in top.Find("Buildings")){var mf=ch.GetComponent<MeshFilter>();if(mf)sb.AppendLine(ch.name+" mesh="+mf.sharedMesh.name+" verts="+mf.sharedMesh.vertexCount+" b="+ch.GetComponent<Renderer>().bounds);}
// sample OSM wall verts near house
var w=top.Find("Buildings/Walls_0");var wm=w.GetComponent<MeshFilter>().sharedMesh;int near=0;var c=new Vector3(185,0,-61);Bounds nb=new Bounds();bool f=false;
foreach(var v in wm.vertices){var q=w.TransformPoint(v);if(new Vector2(q.x-c.x,q.z-c.z).magnitude<68){near++;if(!f){nb=new Bounds(q,Vector3.zero);f=true;}else nb.Encapsulate(q);}}
sb.AppendLine("OSM wall verts in R68="+near+" bounds="+nb);
var slot=GameObject.Find("TV6_Loop/AssetSlots");if(slot)foreach(Transform s in slot.transform){var d=new Vector2(s.position.x-c.x,s.position.z-c.z).magnitude;if(d<75)sb.AppendLine("slot "+s.name+" pos="+s.position.ToString("F2")+" active="+s.gameObject.activeInHierarchy+" d="+d.ToString("F1"));}
return sb.ToString();
