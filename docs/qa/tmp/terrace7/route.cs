// terrace7: 진입로 후보 노선 — 원호(중심 CX,CZ, 반지름 R, 방위 B0 → -90 = 서쪽) 를 0.5 m 간격으로 걸으며
//   중심·좌우 2.25 m 의 차길(큰길/마을길) 높이·지형 높이, ridge_up_c 중심선까지 거리. 큰길 북쪽 가장자리(x 236..252) 도 같이.
float CX=235.5f,CZ=-57.5f,R=11f,B0=20f;
var roads=new System.Collections.Generic.List<(Collider c,string n)>();
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){if(!ch.gameObject.activeInHierarchy)continue;var c=ch.GetComponent<Collider>();if(c&&!ch.name.StartsWith("ROAD_V2p"))roads.Add((c,ch.name.Replace("ROAD_V2","")));}
var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
string Hit(float x,float z){string s="";foreach(var (c,n) in roads){RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))s+=n.Substring(0,Mathf.Min(14,n.Length))+"@"+h.point.y.ToString("F2")+" ";}RaycastHit t;terr.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out t,1000);return "T"+t.point.y.ToString("F2")+" "+s;}
RoadSurface rc=null;foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform)if(ch.name=="ROAD_V2e_ridge_up_c_1")rc=ch.GetComponent<RoadSurface>();
float DC(Vector2 p){float b=1e9f;for(int i=0;i<rc.centerline.Length-1;i++){var a=new Vector2(rc.centerline[i].x,rc.centerline[i].z);var e=new Vector2(rc.centerline[i+1].x,rc.centerline[i+1].z);var ab=e-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude));b=Mathf.Min(b,(a+ab*t-p).magnitude);}return b;}
var sb=new System.Text.StringBuilder();
sb.Append("main north edge (x: first z going north where no main road):\n");
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){}
for(float x=236;x<=252;x+=2){float zEdge=float.NaN;for(float z=-72;z<=-45;z+=.1f){bool onMain=false;foreach(var (c,n) in roads){if(!(n.Contains("165_1")||n=="j_245"))continue;RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000))onMain=true;}if(!onMain&&z>-70){zEdge=z;break;}}sb.Append("  x"+x+" z"+zEdge.ToString("F1")+"\n");}
float L=R*Mathf.Deg2Rad*(B0+90);sb.Append("arc len "+L.ToString("F1")+"\n");
for(float s=-4;s<=L+3;s+=1f){float b=(B0-s/R*Mathf.Rad2Deg);Vector2 p,dir;
 if(s<0){var p0=new Vector2(CX+R*Mathf.Cos(B0*Mathf.Deg2Rad),CZ-R*Mathf.Sin(B0*Mathf.Deg2Rad));dir=new Vector2(Mathf.Sin(B0*Mathf.Deg2Rad),Mathf.Cos(B0*Mathf.Deg2Rad));p=p0+dir*s;}
 else if(s>L){p=new Vector2(CX-(s-L),CZ+R);dir=new Vector2(-1,0);}
 else{p=new Vector2(CX+R*Mathf.Cos(b*Mathf.Deg2Rad),CZ-R*Mathf.Sin(b*Mathf.Deg2Rad));dir=new Vector2(Mathf.Sin(b*Mathf.Deg2Rad),Mathf.Cos(b*Mathf.Deg2Rad));}
 var nl=new Vector2(-dir.y,dir.x);
 sb.Append("s"+s.ToString("F0").PadLeft(3)+" ("+p.x.ToString("F1")+","+p.y.ToString("F1")+") dC"+DC(p).ToString("F1")+" | C "+Hit(p.x,p.y)+"| L "+Hit(p.x+nl.x*2.25f,p.y+nl.y*2.25f)+"| R "+Hit(p.x-nl.x*2.25f,p.y-nl.y*2.25f)+"\n");}
return sb.ToString();
