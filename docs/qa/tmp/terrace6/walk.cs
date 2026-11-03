// terrace6: 집 대문 → 마당 → 집 계단 → 큰길 북쪽 보도 → 차고 계단 → 차고 골목 → 차고 앞 마당. 0.1 m 마다 (지형+보도/계단/골목 콜라이더+차고 콜라이더) 최고점.
// 턱 = 이웃 0.1 m 간 높이차(>0.2 개수) · 경사 = 1 m 창 기울기(계단 구간 제외) · 담 = 담 콜라이더가 발밑보다 5 cm 넘게 높음 · 폭 = 걷는 방향 수직으로 ±3 m 를 0.05 m 마다 훑어 발밑과 ±0.2 m 안인 연속 폭
var cols=new System.Collections.Generic.List<Collider>();cols.Add(GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>());
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)cols.Add(wk.GetComponent<MeshCollider>());
foreach(var c in GameObject.Find("D198_Buildings/D198_garage").GetComponentsInChildren<Collider>())if(c.name.Contains("shell")||c.name.Contains("GARAGE"))cols.Add(c);
var walls=GameObject.Find("TownV6_Gray/TerraceBlock/RetainingWalls");Collider wc=walls?walls.GetComponent<Collider>():null;
float Y(Vector2 p,out bool wall){float y=float.NegativeInfinity;wall=false;foreach(var c in cols){RaycastHit h;if(c&&c.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);}
 if(wc){RaycastHit h;if(wc.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000)&&h.point.y>y+.05f)wall=true;}return y;}
var hT=GameObject.Find("D198_Buildings/house_home").transform;var hq=Quaternion.Euler(0,hT.eulerAngles.y,0);
Vector2 HL(float lx,float lz){var w=hT.position+hq*new Vector3(lx,0,lz);return new Vector2(w.x,w.z);}
// stair=true 구간은 경사·폭을 안 잼(계단 자체 + 계단 발치 첫 챌판 0.9 m). 턱(>0.2)은 모든 구간에서 잼. 계단 끝점은 build_out2.txt stairs.low/high.
var legs=new (string n,Vector2 p,bool stair)[]{("door",HL(-.7f,5.12f),false),("yardE",new Vector2(194.6f,-68.3f),false),("houseStairTop",new Vector2(196.3f,-68.6f),true),("houseStairLow",new Vector2(197.0f,-76.9f),true),("houseFoot",new Vector2(197.3f,-77.9f),false),
 ("walk200",new Vector2(200f,-77.4f),false),("walk205",new Vector2(205f,-76.5f),false),("walk210",new Vector2(210f,-75.6f),false),("walk214",new Vector2(214.3f,-74.9f),false),("garFoot",new Vector2(215.6f,-74.6f),true),("garStairLow",new Vector2(215.6f,-73.7f),true),("garStairTop",new Vector2(215.4f,-65.0f),false),
 ("alley222",new Vector2(222f,-64.0f),false),("alley230",new Vector2(230f,-63.3f),false),("forecourt",new Vector2(229.5f,-58f),false),("garageFront",new Vector2(226f,-55f),false)};
var sb=new System.Text.StringBuilder();var wallAt=new System.Collections.Generic.List<string>();sb.Append("{\"legs\":[");int bad=0;float worstAll=0,gradeAll=0;int wallAll=0;float wMinAll=99;
for(int l=0;l+1<legs.Length;l++){var a=legs[l].p;var b=legs[l+1].p;int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.1f));var ys=new float[n+1];float worst=0,ws=0;int jumps=0,wallHits=0;
 for(int i=0;i<=n;i++){var p=Vector2.Lerp(a,b,i/(float)n);bool w;ys[i]=Y(p,out w);if(w){wallHits++;sb.Append("");if(wallHits==1)wallAt.Add(legs[l].n+"@"+p.ToString("F1"));}if(i>0){float d=Mathf.Abs(ys[i]-ys[i-1]);if(d>worst){worst=d;ws=i*.1f;}if(d>.2f)jumps++;}}
 float seg=Vector2.Distance(a,b)/n;float grade=0;int win=Mathf.Max(1,Mathf.RoundToInt(1f/seg));if(!legs[l].stair)for(int i=0;i+win<=n;i++)grade=Mathf.Max(grade,Mathf.Abs(ys[i+win]-ys[i])/(win*seg));
 // 폭: 구간 중간 3곳
 var dir=(b-a).normalized;var nn=new Vector2(-dir.y,dir.x);float wMin=99;
 foreach(float t in new[]{.25f,.5f,.75f}){var c=Vector2.Lerp(a,b,t);bool w;float y0=Y(c,out w);float lo=0,hi=0;for(float s=.05f;s<=3f;s+=.05f){if(Mathf.Abs(Y(c+nn*s,out w)-y0)>.2f||w)break;hi=s;}for(float s=.05f;s<=3f;s+=.05f){if(Mathf.Abs(Y(c-nn*s,out w)-y0)>.2f||w)break;lo=s;}wMin=Mathf.Min(wMin,lo+hi);}
 if(legs[l].stair)wMin=Mathf.Min(wMin,99);
 bad+=jumps;worstAll=Mathf.Max(worstAll,worst);if(!legs[l].stair)gradeAll=Mathf.Max(gradeAll,grade);wallAll+=wallHits;if(!legs[l].stair)wMinAll=Mathf.Min(wMinAll,wMin);
 sb.Append((l>0?",":"")+"{\"leg\":\""+legs[l].n+">"+legs[l+1].n+"\",\"stair\":"+(legs[l].stair?"true":"false")+",\"lenM\":"+Vector2.Distance(a,b).ToString("F1")+",\"y0\":"+ys[0].ToString("F2")+",\"y1\":"+ys[n].ToString("F2")+",\"maxStep\":"+worst.ToString("F3")+",\"atM\":"+ws.ToString("F1")+",\"jumps>0.2\":"+jumps+",\"maxGrade1m\":"+grade.ToString("F3")+",\"widthMin\":"+wMin.ToString("F2")+",\"wallSamples\":"+wallHits+"}");}
sb.Append("],\"jumps>0.2\":"+bad+",\"worstStep\":"+worstAll.ToString("F3")+",\"maxGradeNonStair\":"+gradeAll.ToString("F3")+",\"minWidthNonStair\":"+wMinAll.ToString("F2")+",\"wallSamples\":"+wallAll+",\"wallAt\":\""+string.Join(" ",wallAt)+"\"}");
return sb.ToString();
