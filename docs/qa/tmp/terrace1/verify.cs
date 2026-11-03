// terrace1 검증: (1) 길 메시 정점 아래 지형 v2 vs 지금 (2) 축대가 차길 위로 침범 (3) 범위 안 물체 발밑 지형 변화(뜸/묻힘 후보)
var sb=new System.Text.StringBuilder();var c=new Vector2(185,-61);float R=68;
var v2=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_v2.asset");
var tg=new GameObject("_t1v"){hideFlags=HideFlags.HideAndDontSave};var old=tg.AddComponent<MeshCollider>();old.sharedMesh=v2;
var top=GameObject.Find("TownV6_Gray").transform;var cur=top.Find("TownV6_Terrain").GetComponent<MeshCollider>();
float G(Collider col,float x,float z){RaycastHit h;return col.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)?h.point.y:float.NaN;}
try{
 int n=0,moved=0;float worst=0;string worstAt="";
 var carTris=new System.Collections.Generic.List<Vector2[]>();
 foreach(Transform ch in top.Find("RoadsV2")){if(!ch.gameObject.activeInHierarchy)continue;var mf=ch.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;var tr=mf.sharedMesh.triangles;
  var w=new Vector3[vs.Length];for(int k=0;k<vs.Length;k++)w[k]=ch.TransformPoint(vs[k]);
  foreach(var p in w){if((new Vector2(p.x,p.z)-c).magnitude>R)continue;n++;float a=G(old,p.x,p.z),b=G(cur,p.x,p.z);float d=Mathf.Abs(a-b);if(d>.005f)moved++;if(d>worst){worst=d;worstAt=ch.name+"@"+p.ToString("F1");}}
  if(!ch.name.StartsWith("ROAD_V2p"))for(int t=0;t<tr.Length;t+=3){var A=w[tr[t]];if((new Vector2(A.x,A.z)-c).magnitude>R+5)continue;carTris.Add(new[]{new Vector2(A.x,A.z),new Vector2(w[tr[t+1]].x,w[tr[t+1]].z),new Vector2(w[tr[t+2]].x,w[tr[t+2]].z)});}}
 sb.AppendLine("{\"roadVerts\":"+n+",\"terrainMovedUnderRoad(>5mm)\":"+moved+",\"worst\":"+worst.ToString("F3")+",\"worstAt\":\""+worstAt+"\",");
 bool In(Vector2 p,Vector2[] t){float Cr(Vector2 u,Vector2 v)=>u.x*v.y-u.y*v.x;float d1=Cr(t[1]-t[0],p-t[0]),d2=Cr(t[2]-t[1],p-t[1]),d3=Cr(t[0]-t[2],p-t[2]);return !((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0));}
 var wall=top.Find("TerraceBlock/RetainingWalls");int wv=0,intr=0;
 if(wall){foreach(var v in wall.GetComponent<MeshFilter>().sharedMesh.vertices){var p=wall.TransformPoint(v);wv++;var q=new Vector2(p.x,p.z);foreach(var t in carTris)if(In(q,t)){intr++;break;}}}
 sb.AppendLine("\"wallVerts\":"+wv+",\"wallVertsOnCarRoad\":"+intr+",");
 // 물체 발밑: 활성 렌더러 중 지형·길·축대 아닌 것, 바운드 바닥이 옛 지형 ±0.6 안에 있던 것
 var list=new System.Collections.Generic.List<string>();int checkedN=0;
 foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){
  if(!r.enabled||!r.gameObject.activeInHierarchy)continue;var b=r.bounds;var q=new Vector2(b.center.x,b.center.z);if((q-c).magnitude>R||b.size.x>40||b.size.z>40)continue;
  var nm=r.name;if(nm.StartsWith("ROAD")||nm.Contains("Terrain")||r.transform.IsChildOf(top.Find("TerraceBlock"))||nm.StartsWith("Walls_")||nm.StartsWith("Roofs_"))continue;
  float a=G(old,q.x,q.y),bb=G(cur,q.x,q.y);if(float.IsNaN(a)||Mathf.Abs(b.min.y-a)>.6f)continue;checkedN++;
  float d=bb-a;if(Mathf.Abs(d)>.1f){var p=r.transform;string path=p.name;int k=0;while(p.parent!=null&&k<2){p=p.parent;path=p.name+"/"+path;k++;}list.Add("\""+path+" d="+d.ToString("F2")+(d>0?" 묻힘":" 뜸")+" @("+q.x.ToString("F0")+","+q.y.ToString("F0")+")\"");}}
 sb.AppendLine("\"groundedObjectsChecked\":"+checkedN+",\"groundChanged>0.1\":"+list.Count+",\"list\":["+string.Join(",",list)+"]}");
}finally{UnityEngine.Object.DestroyImmediate(tg);}
return sb.ToString();
