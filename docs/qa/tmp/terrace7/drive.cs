// terrace7: 진입로 중심선 0.5 m 간격 — 진입로 면 높이·경사(앞 점 대비)·가로 기울기, 그 자리 가장 높은 콜라이더(진입로 위로 솟은 것 = 막힘),
//   좌우 ±2.0 m 면 높이, 옆 바깥(±2.6 m) 땅 높이. 끝: 시작 0.4 m 앞 큰길 판 / 끝 0.4 m 뒤 마당 단차. JSON.
var dg=GameObject.Find("TownV6_Gray/TerraceBlock/Driveway");if(!dg)return "no Driveway";
var dc=dg.GetComponent<MeshCollider>();var rs=dg.GetComponent<RoadSurface>();
var all=new System.Collections.Generic.List<(Collider c,string n)>();
foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){if(!c.enabled||!c.gameObject.activeInHierarchy||c.isTrigger)continue;if(c.bounds.max.x<225||c.bounds.min.x>252||c.bounds.max.z<-70||c.bounds.min.z>-40)continue;all.Add((c,c.gameObject.name));}
float Dy(Vector2 p){RaycastHit h;return dc.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000)?h.point.y:float.NaN;}
(float y,string n) Top(Vector2 p,bool skipDrive){float y=float.NegativeInfinity;string nm="-";foreach(var (c,n) in all){if(skipDrive&&c==dc)continue;RaycastHit h;if(c.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000)&&h.point.y>y){y=h.point.y;nm=n;}}return (y,nm);}
var cl=rs.centerline;var pts=new System.Collections.Generic.List<Vector2>();var L=0f;
for(int i=0;i<cl.Length-1;i++)L+=Vector3.Distance(cl[i],cl[i+1]);
Vector2 At(float s){for(int i=0;i<cl.Length-1;i++){float l=Vector2.Distance(new Vector2(cl[i].x,cl[i].z),new Vector2(cl[i+1].x,cl[i+1].z));if(s<=l||i==cl.Length-2)return Vector2.Lerp(new Vector2(cl[i].x,cl[i].z),new Vector2(cl[i+1].x,cl[i+1].z),Mathf.Clamp01(s/l));s-=l;}return Vector2.zero;}
Vector2 Dir(float s){return (At(Mathf.Min(L,s+.25f))-At(Mathf.Max(0,s-.25f))).normalized;}
var sb=new System.Text.StringBuilder();sb.Append("{\"lengthM\":"+L.ToString("F2")+",\"rows\":[");
float prev=float.NaN,maxG=0,maxStep=0,maxCross=0,maxAbove=0;string aboveAt="";int n=0;
for(float s=0;s<=L+1e-3f;s+=.5f){var p=At(s);var d=Dir(s);var nl=new Vector2(-d.y,d.x);
 float y=Dy(p),yl=Dy(p+nl*2f),yr=Dy(p-nl*2f);var t=Top(p,false);var tl=Top(p+nl*2.2f,false);var tr=Top(p-nl*2.2f,false);
 var gl=Top(p+nl*2.6f,true);var gr=Top(p-nl*2.6f,true);
 float g=float.IsNaN(prev)?0:(y-prev)/.5f;float step=float.IsNaN(prev)?0:Mathf.Abs(y-prev);prev=y;
 float above=Mathf.Max(t.y-y,Mathf.Max(tl.y-Dy(p+nl*2.2f),tr.y-Dy(p-nl*2.2f)));if(above>maxAbove){maxAbove=above;aboveAt=s.ToString("F1")+" "+t.n+"/"+tl.n+"/"+tr.n;}
 maxG=Mathf.Max(maxG,Mathf.Abs(g));float cross=Mathf.Abs(yl-yr)/4f;maxCross=Mathf.Max(maxCross,cross);n++;
 sb.Append((n>1?",":"")+"{\"s\":"+s.ToString("F1")+",\"x\":"+p.x.ToString("F2")+",\"z\":"+p.y.ToString("F2")+",\"y\":"+y.ToString("F3")+",\"grade\":"+g.ToString("F3")+",\"dStep\":"+step.ToString("F3")+",\"cross\":"+cross.ToString("F3")+",\"top\":\""+t.n+"\",\"aboveDrive\":"+above.ToString("F3")+",\"groundL\":"+gl.y.ToString("F2")+",\"groundR\":"+gr.y.ToString("F2")+",\"gL\":\""+gl.n+"\",\"gR\":\""+gr.n+"\"}");}
sb.Append("],");
// 이음매: 시작 쪽(큰길) · 끝 쪽(마당) — 중심·좌우 1.5 m 에서 진입로 끝 면 vs 0.3 m 밖 땅/판
string Joint(float s,float outward){var p=At(s);var d=Dir(s);var nl=new Vector2(-d.y,d.x);var r="[";float mx=0;for(int q=-1;q<=1;q++){var a=p+nl*q*1.5f;var b=a+d*outward*.3f;float ya=Dy(a-d*outward*.05f);var tb=Top(b,true);float dd=ya-tb.y;mx=Mathf.Max(mx,Mathf.Abs(dd));r+=(q>-1?",":"")+"{\"off\":"+(q*1.5f)+",\"drive\":"+ya.ToString("F3")+",\"other\":"+tb.y.ToString("F3")+",\"on\":\""+tb.n+"\",\"step\":"+dd.ToString("F3")+"}";}return r+"],\"max\":"+mx.ToString("F3");}
sb.Append("\"jointStart\":{\"pts\":"+Joint(0,-1)+"},\"jointEnd\":{\"pts\":"+Joint(L,1)+"},");
sb.Append("\"maxGrade\":"+maxG.ToString("F3")+",\"maxStep05\":"+(maxG*.5f).ToString("F3")+",\"maxCross\":"+maxCross.ToString("F3")+",\"maxAboveDrive\":"+maxAbove.ToString("F3")+",\"aboveAt\":\""+aboveAt+"\"}");
System.IO.File.WriteAllText("D:/Github/F1Game/docs/qa/tmp/terrace7/drive.json",sb.ToString());
return "maxGrade "+maxG.ToString("F3")+" maxCross "+maxCross.ToString("F3")+" maxAbove "+maxAbove.ToString("F3")+" @"+aboveAt;
