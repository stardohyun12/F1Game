// terrace3: 블록(중심 185,-61 반경 58) 안 차길 가장자리 — 길 끝 0.1 m 안쪽 높이 vs 0.3 m 바깥 땅(지형+보도) 높이.
// 뜸 = 바깥 땅이 길보다 0.15 m 넘게 낮음(길 모서리가 떠 보임). 다른 길 위면 건너뜀(교차로).
var c0=new Vector2(185,-61);
var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");Collider wc=wk?wk.GetComponent<MeshCollider>():null;
var roads=new System.Collections.Generic.List<Collider>();var chains=new System.Collections.Generic.List<RoadSurface>();
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform){if(!ch.gameObject.activeInHierarchy)continue;var c=ch.GetComponent<Collider>();if(c&&!ch.name.StartsWith("ROAD_V2p"))roads.Add(c);var rs=ch.GetComponent<RoadSurface>();if(rs!=null&&ch.name.StartsWith("ROAD_V2e")&&rs.centerline!=null&&rs.centerline.Length>=2)chains.Add(rs);}
float R(Vector2 p,out bool hit){hit=false;float y=float.NegativeInfinity;foreach(var c in roads){RaycastHit h;if(c.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000)){hit=true;y=Mathf.Max(y,h.point.y);}}return y;}
float G(Vector2 p){float y=float.NegativeInfinity;RaycastHit h;if(terr.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000))y=h.point.y;if(wc&&wc.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);return y;}
int n=0,flo=0,flo30=0,high=0;float worst=0;string wat="";var hist=new int[6];
foreach(var rs in chains){var cl=rs.centerline;
 for(int i=0;i+1<cl.Length;i++){var a=new Vector2(cl[i].x,cl[i].z);var b=new Vector2(cl[i+1].x,cl[i+1].z);float L=(b-a).magnitude;if(L<1e-3f)continue;var d=(b-a)/L;var nn=new Vector2(-d.y,d.x);
  for(float s=0;s<L;s+=.5f){var c=a+d*s;if((c-c0).magnitude>58)continue;
   foreach(var sd in new[]{1f,-1f}){var e=c+nn*sd*rs.width*.5f;bool h1,h2;float ry=R(e-nn*sd*.1f,out h1);if(!h1)continue;R(e+nn*sd*.3f,out h2);if(h2)continue;
    float gy=G(e+nn*sd*.3f);if(float.IsInfinity(gy))continue;float df=gy-ry;n++;
    if(df<-.15f){flo++;if(df<-.3f)flo30++;if(-df>worst){worst=-df;wat=e.ToString("F1");}}
    if(df>.5f)high++;
    int bk=df<-.3f?0:df<-.15f?1:df<-.05f?2:df<=.2f?3:df<=.5f?4:5;hist[bk]++;}}}}
return "{\"samples\":"+n+",\"floating>0.15\":"+flo+",\"floating>0.30\":"+flo30+",\"worstFloat\":"+worst.ToString("F2")+",\"at\":\""+wat+"\",\"bankAbove>0.5\":"+high+",\"hist[<-.3,<-.15,<-.05,<=.2,<=.5,>.5]\":["+string.Join(",",hist)+"]}";
