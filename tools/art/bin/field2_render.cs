// mapshot 밭 2차: 땅에 붙이기. 임시 배치 -> 촬영 -> 삭제. 씬 저장 없음.
// Allowed writes: art/_review/2026-10-30-field_terrace2/*.png, *.txt (이 폴더 안만).
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Pre-existing dirty scene; do not discard");
string outDir="D:/Github/F1Game/art/_review/2026-10-30-field_terrace2/";
string texDir="D:/Github/F1Game/art/_review/2026-10-30-field_terrace/";
var owned=new System.Collections.Generic.List<UnityEngine.Object>();
var log=new System.Text.StringBuilder();
var terrain=new System.Collections.Generic.List<Collider>();
foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.enabled && c.gameObject.activeInHierarchy && (c is TerrainCollider || c.name.Contains("Terrain")))terrain.Add(c);
int rays=0;
float Ground(float x,float z){float best=float.NaN;var ray=new Ray(new Vector3(x,500,z),Vector3.down);foreach(var c in terrain){RaycastHit h;if(c.Raycast(ray,out h,1000)&& (float.IsNaN(best)||h.point.y>best))best=h.point.y;}rays++;if(float.IsNaN(best))throw new System.Exception("Ground miss "+x+","+z);return best;}
var root=new GameObject("_FIELDT_ROOT"){hideFlags=HideFlags.DontSave};owned.Add(root);
try {
var terrainMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_TownV6_Mats/TV6_Terrain.mat");
if(!terrainMaterial)throw new System.Exception("Terrain material missing");var shader=terrainMaterial.shader;
Texture2D LoadTex(string file){var t=new Texture2D(2,2,TextureFormat.RGBA32,true){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};t.LoadImage(System.IO.File.ReadAllBytes(texDir+file));owned.Add(t);return t;}
Material Mat(string name,Color tint,Texture2D tex){var m=new Material(terrainMaterial){name="_FIELDT_"+name,hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetColor("_Tint",tint);if(tex!=null)m.SetTexture("_MainTex",tex);return m;}
var soilTex=LoadTex("soil_albedo.png");var pathTex=LoadTex("path_albedo.png");
// 가장자리 색 섞임: 지형 재질은 텍스처 없이 틴트뿐(TV6_Terrain _Tint). 띠마다 흙 텍스처를 그 색 쪽으로 구워 섞는다 -> 맨 바깥 띠 = 풀과 같은 조명 경로의 같은 색.
var grassC=terrainMaterial.GetColor("_Tint");var soilTint=new Color(.74f*1.12f,.77f*1.12f,.78f*1.12f,1);
const int NB=8;var srcPx=soilTex.GetPixels();var bandMats=new Material[NB];
for(int k=0;k<NB;k++){float t=(k+.5f)/NB;t=t*t*(3-2*t);var px=new Color[srcPx.Length];for(int i=0;i<px.Length;i++){var c0=srcPx[i];px[i]=new Color(Mathf.Lerp(grassC.r,c0.r*soilTint.r,t),Mathf.Lerp(grassC.g,c0.g*soilTint.g,t),Mathf.Lerp(grassC.b,c0.b*soilTint.b,t),1);}
 var bt=new Texture2D(soilTex.width,soilTex.height,TextureFormat.RGBA32,true){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};bt.SetPixels(px);bt.Apply(true);owned.Add(bt);bandMats[k]=Mat("soil"+k,Color.white,bt);}
var pathMat=Mat("path",new Color(.89f,.94f,.99f,1),pathTex);
var cabMat=Mat("cab",new Color(.46f,.62f,.42f,1),null);var cab2Mat=Mat("cab2",new Color(.55f,.68f,.45f,1),null);var leekMat=Mat("leek",new Color(.62f,.76f,.40f,1),null);var pepMat=Mat("pep",new Color(.38f,.54f,.32f,1),null);var chiliMat=Mat("chili",new Color(.9f,.22f,.16f,1),null);var woodMat=Mat("wood",new Color(.45f,.39f,.32f,1),null);
var tuftMat=Mat("tuft",new Color(terrainMaterial.GetColor("_Tint").r*.88f,terrainMaterial.GetColor("_Tint").g*.9f,terrainMaterial.GetColor("_Tint").b*.8f,1),null);
Vector3 center=new Vector3(198,0,-57);var rot=Quaternion.Euler(0,10,0);var rotInv=Quaternion.Inverse(rot);
Vector3 ToWorld(float lx,float lz){var q=rot*new Vector3(lx,0,lz)+center;q.y=Ground(q.x,q.z);return q;}
float Smooth(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);}
GameObject MeshObj(string name,Transform parent,Mesh mesh,Material[] mats){mesh.hideFlags=HideFlags.HideAndDontSave;owned.Add(mesh);var go=new GameObject(name){hideFlags=HideFlags.DontSave};go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=mats;return go;}
void Tri(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 a,Vector3 b,Vector3 c){int i=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(i);t.Add(i+1);t.Add(i+2);i=v.Count;v.Add(a);v.Add(c);v.Add(b);t.Add(i);t.Add(i+1);t.Add(i+2);}
void Leaf(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 b,Vector3 dirH,float len,float rise,float w,float droop){
 Vector3 side=Vector3.Cross(Vector3.up,dirH).normalized;Vector3 tip=b+dirH*len+Vector3.up*(rise-droop);Vector3 mid=b+dirH*len*.45f+Vector3.up*rise*.8f;
 Vector3 L=mid-side*w*.5f,R=mid+side*w*.5f,M=mid+Vector3.up*w*.16f;Tri(v,t,b,L,M);Tri(v,t,b,M,R);Tri(v,t,L,tip,M);Tri(v,t,M,tip,R);}
float Rn(System.Random r){return (float)r.NextDouble();}
// 배추형: 바깥 넓은 잎(낮게 처짐) -> 가운데 -> 안쪽 속잎(모아 세움) = 둥근 포기
void Cabbage(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 b,float h,System.Random rnd){
 float r=.14f+.30f*h;float off=Rn(rnd)*6.28f;
 for(int i=0;i<6;i++){float a=off+i*6.2832f/6+(Rn(rnd)-.5f)*.4f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Leaf(v,t,b+d*.02f,d,r*(.95f+.15f*Rn(rnd)),h*.34f,r*1.0f,h*.20f);}
 for(int i=0;i<5;i++){float a=off+.5f+i*6.2832f/5+(Rn(rnd)-.5f)*.4f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Leaf(v,t,b+d*.02f,d,r*.62f,h*.78f,r*.85f,h*.10f);}
 for(int i=0;i<4;i++){float a=off+1f+i*6.2832f/4+(Rn(rnd)-.5f)*.3f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Leaf(v,t,b+d*.01f,d,r*.30f,h*1.0f,r*.65f,0);}}
// 파: 가늘고 긴 잎 한 뭉치
void Blade(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 b,Vector3 lean,float h,float w){
 var side=Vector3.Cross(Vector3.up,new Vector3(lean.x,0,lean.z)+Vector3.right*.001f).normalized*w*.5f;var m=b+Vector3.up*h*.55f+lean*.5f;var tip=b+Vector3.up*h+lean;
 Tri(v,t,b-side,b+side,m-side*.8f);Tri(v,t,b+side,m+side*.8f,m-side*.8f);Tri(v,t,m-side*.8f,m+side*.8f,tip);}
void Leek(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 b,float h,System.Random rnd){
 int n=7+rnd.Next(3);for(int i=0;i<n;i++){float a=Rn(rnd)*6.28f;var o=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(.03f+.04f*Rn(rnd));var lean=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(.05f+.12f*Rn(rnd))*h*2f;Blade(v,t,b+o,lean,h*(.7f+.4f*Rn(rnd)),.032f);}}
// 고추: 작은 덤불 + 지주대 + 빨간 열매
void Pepper(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,System.Collections.Generic.List<Vector3> cv,System.Collections.Generic.List<int> ct,System.Collections.Generic.List<Vector3> sv,System.Collections.Generic.List<int> st,Vector3 b,float h,System.Random rnd){
 float r=.13f+.18f*h;float off=Rn(rnd)*6.28f;int n=7;
 for(int i=0;i<n;i++){float a=off+i*6.2832f/n+(Rn(rnd)-.5f)*.5f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Leaf(v,t,b+d*.02f+Vector3.up*h*.15f*(i%3),d,r*(.7f+.4f*Rn(rnd)),h*(.5f+.5f*(i%3)/2f),r*.55f,h*.12f);}
 for(int i=0;i<4;i++){float a=Rn(rnd)*6.28f;var p=b+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(r*.7f)+Vector3.up*(h*(.25f+.4f*Rn(rnd)));var s=.016f;var e=p+Vector3.down*.075f;
  Tri(cv,ct,p+Vector3.right*s,p-Vector3.right*s,e);Tri(cv,ct,p+Vector3.forward*s,p-Vector3.forward*s,e);}
 float top=.95f+.2f*Rn(rnd);var sb=b+new Vector3(.11f,-.12f,.04f);var tp=b+new Vector3(.09f,top,.06f);var sd=(tp-sb).normalized;var sideV=Vector3.Cross(sd,Vector3.right).normalized;var up2=Vector3.Cross(sd,sideV).normalized;
 for(int i=0;i<5;i++){float a0=i*6.2832f/5,a1=(i+1)*6.2832f/5;var o0=(sideV*Mathf.Cos(a0)+up2*Mathf.Sin(a0))*.026f;var o1=(sideV*Mathf.Cos(a1)+up2*Mathf.Sin(a1))*.026f;Tri(sv,st,sb+o0,sb+o1,tp+o0);Tri(sv,st,sb+o1,tp+o1,tp+o0);Tri(sv,st,tp+o0,tp+o1,tp);}}
float Noise(float th,float x){return .09f*Mathf.Sin(3*th+.7f)+.06f*Mathf.Sin(5*th+2.1f)+.04f*Mathf.Sin(8*th+.3f)+.035f*Mathf.Sin(x*1.3f+1f);}
var variants=new System.Collections.Generic.List<GameObject>();
foreach(bool bv in new[]{false,true}){
 string vname=bv?"B":"A";var rnd=new System.Random(bv?2610:1030);
 var variant=new GameObject(vname+"_field"){hideFlags=HideFlags.DontSave};variant.transform.SetParent(root.transform,false);variants.Add(variant);
 float wide=bv?12f:10f,deep=bv?9f:8f,a=wide*.5f,b=deep*.5f;
 float FadeAt(float lx,float lz){float q=Mathf.Pow(Mathf.Pow(Mathf.Abs(lx/a),4)+Mathf.Pow(Mathf.Abs(lz/b),4),.25f);float th=Mathf.Atan2(lz/b,lx/a);float bq=1+Noise(th,lx);return 1-Smooth(bq-.22f,bq+.02f,q);}
 int perBed=bv?4:3;float zp=.30f,sp=1.05f;var rowZ=new System.Collections.Generic.List<float>();
 for(int k=0;k<perBed;k++){rowZ.Add(zp-1.0f-k*sp);rowZ.Add(zp+1.0f+k*sp);}
 rowZ.Sort();
 var bd=rot*Vector3.right;var bd2=new Vector2(bd.x,bd.z);
 System.Collections.Generic.List<Vector2> March(float lz){
  var s0w=ToWorld(0,lz);var pos=new Vector2(s0w.x,s0w.z);var res=new System.Collections.Generic.List<Vector2>();
  foreach(int sign in new[]{1,-1}){var p=pos;var prev=bd2*sign;var list=new System.Collections.Generic.List<Vector2>();float walked=0;
   while(walked<a+.4f){float e=1.2f;float gx=(Ground(p.x+e,p.y)-Ground(p.x-e,p.y))/(2*e),gz=(Ground(p.x,p.y+e)-Ground(p.x,p.y-e))/(2*e);var g=new Vector2(gx,gz);var ct=new Vector2(-g.y,g.x);
    if(ct.sqrMagnitude>1e-8f){ct.Normalize();if(Vector2.Dot(ct,prev)<0)ct=-ct;}else ct=prev;
    float w=Mathf.Clamp01((g.magnitude-.03f)/.12f)*.85f;var dir=Vector2.Lerp(bd2*sign,ct,w).normalized;dir=(prev*.7f+dir*.3f).normalized;prev=dir;p+=dir*.25f;walked+=.25f;list.Add(p);}
   if(sign==1)res.AddRange(list);else{list.Reverse();res.InsertRange(0,list);}}
  return res;}
 var rowsPts=new System.Collections.Generic.List<System.Collections.Generic.List<Vector2>>();foreach(var lz in rowZ)rowsPts.Add(March(lz));
 var pathPts=March(zp);
 float[] Cum(System.Collections.Generic.List<Vector2> p){var c=new float[p.Count];for(int i=1;i<p.Count;i++)c[i]=c[i-1]+Vector2.Distance(p[i-1],p[i]);return c;}
 var rowCum=new System.Collections.Generic.List<float[]>();foreach(var p in rowsPts)rowCum.Add(Cum(p));
 void Near(System.Collections.Generic.List<Vector2> pts,float[] cum,Vector2 q,out float dist,out float arc){dist=1e9f;arc=0;for(int i=0;i<pts.Count-1;i++){var s0=pts[i];var s1=pts[i+1];var d=s1-s0;float tt=Mathf.Clamp01(Vector2.Dot(q-s0,d)/Mathf.Max(d.sqrMagnitude,1e-6f));float dd=Vector2.Distance(q,s0+d*tt);if(dd<dist){dist=dd;arc=cum[i]+tt*d.magnitude;}}}
 var pathCum=Cum(pathPts);
 float[] rowH=new float[rowZ.Count];for(int i=0;i<rowH.Length;i++)rowH[i]=.15f+.10f*Rn(rnd);// 이랑 꼭대기 = 지면 +0.15~0.25
 // 줄별 작물 종류: 0 배추 1 파 2 고추
 int[] types=bv?new[]{0,0,1,2,0,1,2,0}:new[]{0,1,0,2,1,0};
 float hw=.40f;float step=.09f;int nx=Mathf.CeilToInt((wide+1.6f)/step),nz=Mathf.CeilToInt((deep+1.6f)/step);
 var vv=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var fadeA=new System.Collections.Generic.List<float>();var pathA=new System.Collections.Generic.List<float>();
 float maxRidge=-999,edgeLift=0,edgeN=0,edgeMaxLift=-9,minLift=9;
 float cu=Mathf.Cos(.31f),su=Mathf.Sin(.31f);
 for(int iz=0;iz<=nz;iz++)for(int ix=0;ix<=nx;ix++){float lx=-a-.8f+ix*step,lz=-b-.8f+iz*step;
  float fade=FadeAt(lx,lz);
  var w=ToWorld(lx,lz);var q2=new Vector2(w.x,w.z);float ridge=0;
  for(int r=0;r<rowsPts.Count;r++){float dist,arc;Near(rowsPts[r],rowCum[r],q2,out dist,out arc);if(dist<hw){float total=rowCum[r][rowCum[r].Length-1];float taper=Smooth(.5f,2.3f,Mathf.Min(arc,total-arc));float prof=Mathf.Pow(.5f*(1+Mathf.Cos(Mathf.PI*dist/hw)),.7f);float hh=rowH[r]*(1+.12f*Mathf.Sin(arc*1.7f+r));ridge=Mathf.Max(ridge,hh*prof*(.25f+.75f*taper));}}
  float dp,ap;Near(pathPts,pathCum,q2,out dp,out ap);float pathW=.5f+.07f*Mathf.Sin(ap*1.9f);float endT=Mathf.Clamp01((Mathf.Min(ap,pathCum[pathCum.Length-1]-ap)-.6f)/1.4f);float isPath=dp<pathW*endT?1f:0f;
  float rf=Smooth(.40f,.85f,fade);// 가장자리 쪽에서 이랑이 0 으로
  float lift=.016f+.012f*fade+(isPath>0?-.010f:0f)+ridge*rf;// 가장자리 = 지면 +1.6cm(겹침 방지만)
  float y=w.y+lift;if(fade>.4f&&ridge>maxRidge)maxRidge=ridge;
  if(fade>.02f&&fade<.4f){edgeLift+=lift;edgeN++;edgeMaxLift=Mathf.Max(edgeMaxLift,lift);}
  vv.Add(new Vector3(w.x,y,w.z));
  float ux=w.x*cu-w.z*su,uz=w.x*su+w.z*cu;uv.Add(new Vector2(ux/1.3f,uz/1.15f));// 타일 약 1.3×1.15m, 살짝 돌려서 결 방향 반복을 깬다
  fadeA.Add(fade);pathA.Add(isPath>0?0f:9f);}
 var tBand=new System.Collections.Generic.List<int>[NB];for(int k=0;k<NB;k++)tBand[k]=new System.Collections.Generic.List<int>();var tPath=new System.Collections.Generic.List<int>();
 for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i0=iz*(nx+1)+ix,i1=i0+1,i2=i0+nx+1,i3=i2+1;
  float fm=(fadeA[i0]+fadeA[i1]+fadeA[i2]+fadeA[i3])*.25f;float fmax=Mathf.Max(Mathf.Max(fadeA[i0],fadeA[i1]),Mathf.Max(fadeA[i2],fadeA[i3]));
  if(fmax<.02f)continue;
  float dpm=(pathA[i0]+pathA[i1]+pathA[i2]+pathA[i3])*.25f;var tl=dpm<=4.6f?tPath:tBand[Mathf.Clamp(Mathf.FloorToInt(fm*NB),0,NB-1)];tl.AddRange(new[]{i0,i2,i1,i1,i2,i3});}
 var mesh=new Mesh(){indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vv);mesh.SetUVs(0,uv);mesh.subMeshCount=NB+1;for(int k=0;k<NB;k++)mesh.SetTriangles(tBand[k],k);mesh.SetTriangles(tPath,NB);mesh.RecalculateNormals();mesh.RecalculateBounds();
 var allMats=new Material[NB+1];for(int k=0;k<NB;k++)allMats[k]=bandMats[k];allMats[NB]=pathMat;
 MeshObj("tilled_soil",variant.transform,mesh,allMats);
 // 작물: 줄마다 종류가 다르다
 var cab=new System.Collections.Generic.List<Vector3>();var cabT=new System.Collections.Generic.List<int>();var cab2=new System.Collections.Generic.List<Vector3>();var cab2T=new System.Collections.Generic.List<int>();
 var lk=new System.Collections.Generic.List<Vector3>();var lkT=new System.Collections.Generic.List<int>();var pp=new System.Collections.Generic.List<Vector3>();var ppT=new System.Collections.Generic.List<int>();
 var ch=new System.Collections.Generic.List<Vector3>();var chT=new System.Collections.Generic.List<int>();var sv=new System.Collections.Generic.List<Vector3>();var st=new System.Collections.Generic.List<int>();
 int nCab=0,nLeek=0,nPep=0,gaps=0;float cabMin=9,cabMax=0,leekMin=9,leekMax=0,pepMin=9,pepMax=0;
 for(int r=0;r<rowsPts.Count;r++){var pts=rowsPts[r];var cum=rowCum[r];float total=cum[cum.Length-1];int ty=types[r%types.Length];
  bool late=(r==(bv?4:2));float grow=late?.30f:(.5f+.2f*Rn(rnd));float emptyP=late?.25f:.08f;
  float gap=ty==0?.66f:ty==1?.34f:.78f;float s=.9f;
  while(s<total-.9f){
   float ss=s+(Rn(rnd)-.5f)*.12f;
   if(rnd.NextDouble()<emptyP){gaps++;s+=gap;continue;}
   int j=0;while(j<cum.Length-2&&cum[j+1]<ss)j++;float tt=Mathf.Clamp01((ss-cum[j])/Mathf.Max(cum[j+1]-cum[j],1e-4f));var p2=Vector2.Lerp(pts[j],pts[j+1],tt);
   var gp=new Vector3(p2.x,Ground(p2.x,p2.y),p2.y);var lp=rotInv*(gp-center);
   if(FadeAt(lp.x,lp.z)<.9f){s+=gap;continue;}
   float crest=rowH[r]*(1+.12f*Mathf.Sin(ss*1.7f+r));var baseP=new Vector3(p2.x+(Rn(rnd)-.5f)*.07f,gp.y+.022f+crest*.95f,p2.y);
   float sz=grow*(.7f+.6f*Rn(rnd));
   if(ty==0){float h=Mathf.Clamp(sz*.75f,.18f,.46f);cabMin=Mathf.Min(cabMin,h);cabMax=Mathf.Max(cabMax,h);if(Rn(rnd)<.3f)Cabbage(cab2,cab2T,baseP,h,rnd);else Cabbage(cab,cabT,baseP,h,rnd);nCab++;}
   else if(ty==1){float h=Mathf.Clamp(sz*1.0f,.25f,.62f);leekMin=Mathf.Min(leekMin,h);leekMax=Mathf.Max(leekMax,h);Leek(lk,lkT,baseP,h,rnd);Leek(lk,lkT,baseP+new Vector3(.13f,0,.08f),h*(.8f+.3f*Rn(rnd)),rnd);nLeek++;}
   else{float h=Mathf.Clamp(sz*.85f,.28f,.55f);pepMin=Mathf.Min(pepMin,h);pepMax=Mathf.Max(pepMax,h);Pepper(pp,ppT,ch,chT,sv,st,baseP,h,rnd);nPep++;}
   s+=gap+.08f*Rn(rnd);}}
 Mesh CropMesh(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t){var m=new Mesh(){indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
 MeshObj("cabbage",variant.transform,CropMesh(cab,cabT),new[]{cabMat});MeshObj("cabbage2",variant.transform,CropMesh(cab2,cab2T),new[]{cab2Mat});MeshObj("leek",variant.transform,CropMesh(lk,lkT),new[]{leekMat});MeshObj("pepper",variant.transform,CropMesh(pp,ppT),new[]{pepMat});MeshObj("chili",variant.transform,CropMesh(ch,chT),new[]{chiliMat});MeshObj("stakes",variant.transform,CropMesh(sv,st),new[]{woodMat});
 // 가장자리 풀 덤불(흩뿌린 흙 판은 없음 — 번짐은 메시 안 색 섞임으로만)
 var tv=new System.Collections.Generic.List<Vector3>();var tt2=new System.Collections.Generic.List<int>();int tufts=0;
 for(int k=0;k<28;k++){float th=6.2832f*(k+Rn(rnd)*.8f)/28f;float cx=Mathf.Cos(th),sz2=Mathf.Sin(th);float lx=a*Mathf.Sign(cx)*Mathf.Sqrt(Mathf.Abs(cx)),lz=b*Mathf.Sign(sz2)*Mathf.Sqrt(Mathf.Abs(sz2));
  float th2=Mathf.Atan2(lz/b,lx/a);float sc=1.04f+Noise(th2,lx)+(Rn(rnd)-.35f)*.09f;lx*=sc;lz*=sc;var w=ToWorld(lx,lz);
  int nb=4+rnd.Next(3);for(int i=0;i<nb;i++){float ang=Rn(rnd)*6.28f;var d=new Vector3(Mathf.Cos(ang),0,Mathf.Sin(ang));Leaf(tv,tt2,w+d*.06f,d,.14f+.1f*Rn(rnd),.14f+.16f*Rn(rnd),.05f,.06f);}tufts++;}
 MeshObj("edge_tufts",variant.transform,CropMesh(tv,tt2),new[]{tuftMat});
 int tris=0;foreach(var mf in variant.GetComponentsInChildren<MeshFilter>()){var m=mf.sharedMesh;for(int sm=0;sm<m.subMeshCount;sm++)tris+=(int)m.GetIndexCount(sm)/3;}
 log.AppendLine(vname+" rows="+rowZ.Count+" cabbage="+nCab+" (h "+cabMin.ToString("F2")+".."+cabMax.ToString("F2")+") leek="+nLeek+" (h "+leekMin.ToString("F2")+".."+leekMax.ToString("F2")+") pepper="+nPep+" (h "+pepMin.ToString("F2")+".."+pepMax.ToString("F2")+") gaps="+gaps+" ridgeTop(max)="+maxRidge.ToString("F3")+" edgeRing: mean lift="+(edgeN>0?edgeLift/edgeN:0).ToString("F3")+" max lift="+edgeMaxLift.ToString("F3")+" tufts="+tufts+" tris="+tris+" soilVerts="+vv.Count);
 variant.SetActive(false);
}
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;if(!src)throw new System.Exception("No source camera");
var camGo=new GameObject("_FIELDT_CAMERA"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);else{cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.56f,.72f,.90f,1);}
log.AppendLine("PCLookScene="+(pcl!=null)+" look="+(pcl!=null?pcl.look:-1)+" A7Shade="+Shader.GetGlobalColor("_A7Shade"));
void Shoot(string file,Vector3 pos,Vector3 tgt,float fov,int h){cam.fieldOfView=fov;camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 if(!PitCrewPlan.PlanIO.RenderPng(cam,outDir+file,1280,h,null))throw new System.Exception("Capture failed "+file);log.AppendLine(file+" pos="+pos+" target="+tgt+" fov="+fov+" res=1280x"+h);}
Vector3 cartPos=new Vector3(195f,Ground(195f,-66f)+1.0f,-66f),cartTgt=new Vector3(198,Ground(198,-57f)+.35f,-57f);
foreach(var v in variants){v.SetActive(true);string L=v.name.Substring(0,1);
 Shoot("new_"+L+"_high.png",new Vector3(196.881f,53.714f,-81.658f),new Vector3(197.27f,-95.469f,39.031f),60,611);
 Shoot("new_"+L+"_eye.png",new Vector3(190,Ground(190,-51)+1.65f,-51),new Vector3(198,Ground(198,-57)+.20f,-57),78,720);
 var tp=new Vector3(198,Ground(198,-57)+15f,-57);camGo.transform.SetPositionAndRotation(tp,Quaternion.LookRotation(Vector3.down,rot*Vector3.forward));cam.fieldOfView=40;PitCrewPlan.PlanIO.RenderPng(cam,outDir+"dbg_"+L+"_top.png",1280,720,null);
 Shoot("new_"+L+"_cart.png",cartPos,cartTgt,70,720);
 // 가까이: 이랑 옆면이 보이는 낮은 컷(스커트·접지 확인)
 Shoot("close_"+L+"_edge.png",new Vector3(193.5f,Ground(193.5f,-61.5f)+.55f,-61.5f),new Vector3(198,Ground(198,-57)+.1f,-57),70,720);
 v.SetActive(false);}
log.AppendLine("raycasts="+rays+" colliders="+terrain.Count+" compiling="+UnityEditor.EditorApplication.isCompiling+" shaderErrors="+UnityEditor.ShaderUtil.ShaderHasError(shader));
}finally{
 for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);
 bool dirtyBefore=scene.isDirty;
 if(scene.isDirty)UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path,UnityEditor.SceneManagement.OpenSceneMode.Single);
 scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 int remaining=0;foreach(var o in Resources.FindObjectsOfTypeAll<GameObject>())if(o.name.StartsWith("_FIELDT")||o.name=="A_field"||o.name=="B_field")remaining++;
 log.AppendLine("cleanup remaining="+remaining+" dirtyBeforeDiscard="+dirtyBefore+" dirty="+scene.isDirty+" playing="+Application.isPlaying);
 System.IO.File.WriteAllText(outDir+"render_evidence.txt",log.ToString());
}
return log.ToString();
