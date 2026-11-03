// 밭 자리 측정: 집 터 사각(Footprint), 흙길 끝, 후보 자리(A: 입구가 집 쪽 +x, 흙길 끝에 맞춤) 바닥 높이·겹치는 콜라이더
var sb=new System.Text.StringBuilder();
var h=GameObject.Find("D198_Buildings/house_home").transform;var hq=Quaternion.Euler(0,h.eulerAngles.y,0);
Vector3 HL(float x,float z)=>h.position+hq*new Vector3(x,0,z);
var fm=typeof(TerraceBlock).GetMethod("Footprint",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var args=new object[]{h,1.2f,null,0f,0f,0f,0f,0f};fm.Invoke(null,args);
float hMin=(float)args[3],hx0=(float)args[4],hx1=(float)args[5],hz0=(float)args[6],hz1=(float)args[7];
sb.AppendLine($"house hMin={hMin:F3} lx=[{hx0:F2},{hx1:F2}] lz=[{hz0:F2},{hz1:F2}] yx0={hx0-1.2f:F2}");
// 흙길 메시: 집 로컬로 바꿔 끝점 근처
var pg=GameObject.Find("TownV6_Gray/TerraceBlock/DirtPath_door_garden");var pm=pg.GetComponent<MeshFilter>().sharedMesh;var inv=Quaternion.Inverse(hq);
float minX=1e9f,maxZ=-1e9f;var pv=pm.vertices;Vector3 far=Vector3.zero;float best=1e9f;
foreach(var v in pv){var w=pg.transform.TransformPoint(v);var l=inv*(w-h.position);if(l.x<best){best=l.x;far=l;}}
sb.AppendLine($"path verts={pv.Length} minLocalX={best:F2} at z={far.z:F2} y={(far.y+h.position.y):F3}");
// 흙길 정점 중 x<-5.5 인 것들의 z 범위
float zlo=1e9f,zhi=-1e9f;foreach(var v in pv){var w=pg.transform.TransformPoint(v);var l=inv*(w-h.position);if(l.x<-5.8f){zlo=Mathf.Min(zlo,l.z);zhi=Mathf.Max(zhi,l.z);}}
sb.AppendLine($"path x<-5.8 z=[{zlo:F2},{zhi:F2}]");
// 흙길 중심선 대략: x 로컬 구간별 z 평균
for(float bx=-6.8f;bx<=-0.5f;bx+=.5f){float s=0;int n=0;float zmin=1e9f,zmax=-1e9f;foreach(var v in pv){var l=inv*(pg.transform.TransformPoint(v)-h.position);if(l.x>=bx&&l.x<bx+.5f){s+=l.z;n++;zmin=Mathf.Min(zmin,l.z);zmax=Mathf.Max(zmax,l.z);}}if(n>0)sb.Append($"x{bx:F1}:z[{zmin:F2},{zmax:F2}] ");}
sb.AppendLine();
var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways").GetComponent<MeshCollider>();
float G(Vector3 w){float y=float.NegativeInfinity;foreach(var c in new Collider[]{terr,wk}){RaycastHit r;if(c.Raycast(new Ray(new Vector3(w.x,500,w.z),Vector3.down),out r,1000))y=Mathf.Max(y,r.point.y);}return y;}
// 후보 A 중심(집 로컬) = (-8.94, 2.01), yaw = hyaw+90
float cxA=-6.35f-2.59f,czA=1.6f+.41f;
sb.AppendLine($"A center local=({cxA:F2},{czA:F2}) world={HL(cxA,czA).ToString("F3")}");
float ymin=1e9f,ymax=-1e9f;
for(float lx=cxA-2.8f;lx<=cxA+2.81f;lx+=.4f)for(float lz=czA-3.05f;lz<=czA+3.06f;lz+=.4f){float y=G(HL(lx,lz));ymin=Mathf.Min(ymin,y);ymax=Mathf.Max(ymax,y);}
sb.AppendLine($"A grid ground y=[{ymin:F3},{ymax:F3}]");
foreach(var (lx,lz) in new[]{(cxA-2.77f,czA-3.02f),(cxA-2.77f,czA+3.02f),(cxA+2.8f,czA-3.02f),(cxA+2.8f,czA+3.02f)})sb.AppendLine($" corner local({lx:F2},{lz:F2}) y={G(HL(lx,lz)):F3}");
// 겹치는 콜라이더
var cols=Physics.OverlapBox(HL(cxA,czA)+Vector3.up*(hMin+.6f-h.position.y),new Vector3(3.0f,.5f,2.9f),hq*Quaternion.Euler(0,90,0));
foreach(var c in cols)sb.Append(c.name+"("+c.GetType().Name+") ");
sb.AppendLine();
// 슬롯(원래 칸) 바닥
float s0=1e9f,s1=-1e9f;for(float lx=-12.6f;lx<=-6.6f;lx+=.4f)for(float lz=-2.5f;lz<=3.0f;lz+=.4f){float y=G(HL(lx,lz));s0=Mathf.Min(s0,y);s1=Mathf.Max(s1,y);}
sb.AppendLine($"slot ground y=[{s0:F3},{s1:F3}]");
return sb.ToString();
