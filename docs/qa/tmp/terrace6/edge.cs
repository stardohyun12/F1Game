// terrace6: 큰길(165_1) 북쪽 가장자리 z, 보도(Walkways) 북쪽 끝 z, 땅 높이 — x 205..245 1 m 마다. AssetSlots 중 근처 것 bounds.
var roads=new System.Collections.Generic.List<Collider>();foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform)if(ch.gameObject.activeInHierarchy&&!ch.name.StartsWith("ROAD_V2p")){var c=ch.GetComponent<Collider>();if(c)roads.Add(c);}
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways").GetComponent<Collider>();var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<Collider>();
bool Hit(Collider c,float x,float z,out float y){RaycastHit h;y=0;if(c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)){y=h.point.y;return true;}return false;}
bool Road(float x,float z,out float y){y=0;foreach(var c in roads)if(Hit(c,x,z,out y))return true;return false;}
var sb=new System.Text.StringBuilder();
for(float x=205;x<=245;x+=1){float zr=float.NaN,yr=0;for(float z=-50;z>=-90;z-=.1f){if(Road(x,z,out float y)){zr=z;yr=y;break;}}
 float zw0=float.NaN,zw1=float.NaN,yw=0;for(float z=-50;z>=-90;z-=.1f){if(Hit(wk,x,z,out float y)){if(float.IsNaN(zw0)){zw0=z;yw=y;}zw1=z;}else if(!float.IsNaN(zw0)&&z<zw0-4)break;}
 Hit(terr,x,zr+1.5f,out float t15);Hit(terr,x,zr+3f,out float t3);
 sb.Append($"x{x:F0} roadN z{zr:F1} y{yr:F2} | walk z{zw0:F1}..{zw1:F1} y{yw:F2} | terr+1.5 {t15:F2} +3 {t3:F2}\n");}
foreach(Transform s in GameObject.Find("TV6_Loop/AssetSlots").transform){var b=new Bounds(s.position,Vector3.zero);bool f=false;foreach(var r in s.GetComponentsInChildren<Renderer>()){if(!f){b=r.bounds;f=true;}else b.Encapsulate(r.bounds);}if(b.max.x<200||b.min.x>250||b.max.z<-85||b.min.z>-40)continue;sb.Append("slot "+s.name+" act="+s.gameObject.activeInHierarchy+" pos "+s.position.ToString("F1")+" "+b.min.ToString("F1")+" "+b.max.ToString("F1")+"\n");}
return sb.ToString();
