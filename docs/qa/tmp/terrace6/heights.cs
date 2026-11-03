// terrace6: 높이 격자(지형+보도+길+차고 콜라이더 최고점) x 195..245 / z -45..-82, 2.5 m 간격. 칸 표시: R=차길 W=보도 G=차고 메시 T=지형
var cols=new System.Collections.Generic.List<(Collider c,char k)>();
cols.Add((GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>(),'T'));
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)cols.Add((wk.GetComponent<MeshCollider>(),'W'));
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){if(!ch.gameObject.activeInHierarchy)continue;var c=ch.GetComponent<Collider>();if(c)cols.Add((c,ch.name.StartsWith("ROAD_V2p")?'P':'R'));}
foreach(var c in GameObject.Find("D198_Buildings/D198_garage").GetComponentsInChildren<Collider>())cols.Add((c,'G'));
var sb=new System.Text.StringBuilder();sb.Append("garageColliders="+GameObject.Find("D198_Buildings/D198_garage").GetComponentsInChildren<Collider>().Length+"\n      ");
for(float x=195;x<=247.6f;x+=2.5f)sb.Append(x.ToString("F0").PadLeft(7));sb.Append("\n");
for(float z=-45;z>=-82.1f;z-=2.5f){sb.Append(z.ToString("F1").PadLeft(6));
 for(float x=195;x<=247.6f;x+=2.5f){float y=float.NegativeInfinity;char k='?';foreach(var (c,kk) in cols){RaycastHit h;if(c&&c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)&&h.point.y>y+.01f){y=h.point.y;k=kk;}}sb.Append((y.ToString("F1")+k).PadLeft(7));}
 sb.Append("\n");}
// 차고 메시 바닥(렌더러 정점 중 최저 +0.3 m 안) 범위
var gm=GameObject.Find("D198_Buildings/D198_garage");float mn=1e9f;var pts=new System.Collections.Generic.List<Vector3>();
foreach(var mf in gm.GetComponentsInChildren<MeshFilter>()){if(!mf.sharedMesh)continue;foreach(var v in mf.sharedMesh.vertices){var w=mf.transform.TransformPoint(v);pts.Add(w);mn=Mathf.Min(mn,w.y);}}
sb.Append("garage minY "+mn.ToString("F2")+" verts "+pts.Count+"\n");
foreach(Transform c in gm.transform.GetChild(0)){var b=new Bounds(c.position,Vector3.zero);bool f=false;foreach(var r in c.GetComponentsInChildren<Renderer>()){if(!f){b=r.bounds;f=true;}else b.Encapsulate(r.bounds);}sb.Append("  "+c.name+" "+b.min.ToString("F1")+" "+b.max.ToString("F1")+"\n");}
return sb.ToString();
