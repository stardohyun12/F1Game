// terrace5: 걷기 왕복 단차(5차 경로: 큰길 보도 → 직선 계단 31단 → 마당 동쪽 → 흙길 → 앞문 → 마당 서쪽). terrace4/loop.cs 와 같은 방식 —
// 0.1 m 마다 (지형 + 보도/계단 콜라이더) 최고점, 이웃 간 높이차 0.2 m 초과 개수. 계단 끝점은 build_out_b.txt 의 stairs.low/high.
var cols=new System.Collections.Generic.List<Collider>();cols.Add(GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>());
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)cols.Add(wk.GetComponent<MeshCollider>());
var walls=GameObject.Find("TownV6_Gray/TerraceBlock/RetainingWalls");Collider wc=walls?walls.GetComponent<Collider>():null;
float Y(Vector2 p,out bool wall){float y=float.NegativeInfinity;wall=false;foreach(var c in cols){RaycastHit h;if(c&&c.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);}
 if(wc){RaycastHit h;if(wc.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000)&&h.point.y>y+.05f)wall=true;}return y;}
var hT=GameObject.Find("D198_Buildings/house_home").transform;var hq=Quaternion.Euler(0,hT.eulerAngles.y,0);
Vector2 HL(float lx,float lz){var w=hT.position+hq*new Vector3(lx,0,lz);return new Vector2(w.x,w.z);}
var sLow=new Vector2(197.0f,-76.9f);var sTop=new Vector2(196.3f,-68.6f);
var ps=HL(-.7f,5.12f);var pm=HL(-6.0f,5.42f);var pe=HL(-6.35f,1.6f);var pathMid=.25f*ps+.5f*pm+.25f*pe;
var legs=new[]{("walkFront",new Vector2(197.2f,-78.2f)),("stairLow",sLow),("stairMid",Vector2.Lerp(sLow,sTop,.5f)),("stairTop",sTop),("gardenGateOut",HL(-5.9f,1.6f)),("pathMid",pathMid),("pathStart",ps),("yard",new Vector2(187.4f,-68.7f))};
var sb=new System.Text.StringBuilder();sb.Append("{\"wallsCollider\":"+(wc!=null?"true":"false")+",\"legs\":[");
int bad=0;float worstAll=0;
for(int l=0;l+1<legs.Length;l++){var a=legs[l].Item2;var b=legs[l+1].Item2;int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.1f));float prev=float.NaN,worst=0,ws=0;int jumps=0,wallHits=0;float y0=0,y1=0;
 for(int i=0;i<=n;i++){var p=Vector2.Lerp(a,b,i/(float)n);bool w;float y=Y(p,out w);if(w)wallHits++;if(i==0)y0=y;y1=y;if(!float.IsNaN(prev)){float d=Mathf.Abs(y-prev);if(d>worst){worst=d;ws=i*.1f;}if(d>.2f)jumps++;}prev=y;}
 bad+=jumps;worstAll=Mathf.Max(worstAll,worst);
 sb.Append((l>0?",":"")+"{\"leg\":\""+legs[l].Item1+">"+legs[l+1].Item1+"\",\"lenM\":"+Vector2.Distance(a,b).ToString("F1")+",\"y0\":"+y0.ToString("F2")+",\"y1\":"+y1.ToString("F2")+",\"maxStep\":"+worst.ToString("F3")+",\"atM\":"+ws.ToString("F1")+",\"jumps>0.2\":"+jumps+",\"wallSamples\":"+wallHits+"}");}
sb.Append("],\"jumps>0.2\":"+bad+",\"worst\":"+worstAll.ToString("F3")+"}");
return sb.ToString();
