// terrace7 봇: Play 중에 실행. 큰길 165_1(진입로 갈림목 약 25 m 전) → 진입로 중심선 → 마당 안 4 m. 2 m 간격으로 LoopPts 에 넣고 정·역 1회씩.
Application.runInBackground=true;
var pts=new System.Collections.Generic.List<Vector3>();
foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform)if(ch.name=="ROAD_V2e_camo_309378634_165_1"){var c=ch.GetComponent<RoadSurface>().centerline;foreach(var q in c)if(q.x>222f&&q.x<245.5f)pts.Add(q);}
var dc=GameObject.Find("TownV6_Gray/TerraceBlock/Driveway").GetComponent<RoadSurface>().centerline;
foreach(var q in dc)pts.Add(q);
var e=dc[dc.Length-1];pts.Add(e+new Vector3(-2,0,0));pts.Add(e+new Vector3(-4,0,0));
// 2 m 간격으로 다시 뽑기
var cum=new float[pts.Count];for(int i=1;i<pts.Count;i++)cum[i]=cum[i-1]+Vector2.Distance(new Vector2(pts[i-1].x,pts[i-1].z),new Vector2(pts[i].x,pts[i].z));
var res=new System.Collections.Generic.List<Vector3>();int k=1;
for(float s=0;s<=cum[pts.Count-1];s+=2f){while(k<pts.Count-1&&cum[k]<s)k++;float l=cum[k]-cum[k-1];res.Add(Vector3.Lerp(pts[k-1],pts[k],l>1e-4f?(s-cum[k-1])/l:0));}
res.Add(pts[pts.Count-1]);
TownPathBot.LoopPts.Clear();TownPathBot.LoopPts.AddRange(res);TownPathBot.LoopN=1;TownPathBot.Only="";
TownPathBot.Begin("loopGarage",2f);
return "pts "+res.Count+" first "+res[0].ToString("F1")+" last "+res[res.Count-1].ToString("F1");
