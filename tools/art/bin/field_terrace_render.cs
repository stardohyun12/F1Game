// mapshot D-208 정정: 밭 질감·형체 목업. 임시 배치 -> 촬영 -> 삭제. 씬 저장 없음.
// Allowed writes: art/_review/2026-10-30-field_terrace/*.png, *.txt (이 폴더 안만).
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Pre-existing dirty scene; do not discard");
string outDir="D:/Github/F1Game/art/_review/2026-10-30-field_terrace/";
var owned=new System.Collections.Generic.List<UnityEngine.Object>();
var log=new System.Text.StringBuilder();
var terrain=new System.Collections.Generic.List<Collider>();
foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.enabled && c.gameObject.activeInHierarchy && (c is TerrainCollider || c.name.Contains("Terrain")))terrain.Add(c);
int rays=0;
float Ground(float x,float z){float best=float.NaN;var ray=new Ray(new Vector3(x,500,z),Vector3.down);foreach(var c in terrain){RaycastHit h;if(c.Raycast(ray,out h,1000)&& (float.IsNaN(best)||h.point.y>best))best=h.point.y;}rays++;if(float.IsNaN(best))throw new System.Exception("Ground miss "+x+","+z);return best;}
var P=new System.Collections.Generic.Dictionary<string,Color>();
foreach(var line in System.IO.File.ReadAllLines(outDir+"params.txt")){var kv=line.Split('=');if(kv.Length!=2)continue;var f=kv[1].Split(',');P[kv[0].Trim()]=new Color(float.Parse(f[0],System.Globalization.CultureInfo.InvariantCulture),float.Parse(f[1],System.Globalization.CultureInfo.InvariantCulture),float.Parse(f[2],System.Globalization.CultureInfo.InvariantCulture),1);}
bool restoreProbe=false;UnityEngine.Rendering.SphericalHarmonicsL2 probeBackup=RenderSettings.ambientProbe;
var root=new GameObject("_FIELDT_ROOT"){hideFlags=HideFlags.DontSave};owned.Add(root);
try {
var terrainMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_TownV6_Mats/TV6_Terrain.mat");
if(!terrainMaterial)throw new System.Exception("Terrain material missing");var shader=terrainMaterial.shader;
Texture2D LoadTex(string file){var t=new Texture2D(2,2,TextureFormat.RGBA32,true){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};t.LoadImage(System.IO.File.ReadAllBytes(outDir+file));owned.Add(t);return t;}
Material Mat(string name,Color tint,Texture2D tex){var m=new Material(terrainMaterial){name="_FIELDT_"+name,hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetColor("_Tint",tint);if(tex!=null)m.SetTexture("_MainTex",tex);return m;}
var soilTex=LoadTex("soil_albedo.png");var pathTex=LoadTex("path_albedo.png");
var soilMat=Mat("soil",P["soil"],soilTex);var pathMat=Mat("path",P["path"],pathTex);
var cropMat=Mat("crop",P["crop"],null);var crop2Mat=Mat("crop2",P["crop2"],null);var woodMat=Mat("wood",P["wood"],null);
var tuftMat=Mat("tuft",new Color(terrainMaterial.GetColor("_Tint").r*.88f,terrainMaterial.GetColor("_Tint").g*.9f,terrainMaterial.GetColor("_Tint").b*.8f,1),null);
Vector3 center=new Vector3(198,0,-57);var rot=Quaternion.Euler(0,10,0);
Vector3 ToWorld(float lx,float lz){var q=rot*new Vector3(lx,0,lz)+center;q.y=Ground(q.x,q.z);return q;}
float Smooth(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);}
GameObject MeshObj(string name,Transform parent,Mesh mesh,Material[] mats){mesh.hideFlags=HideFlags.HideAndDontSave;owned.Add(mesh);var go=new GameObject(name){hideFlags=HideFlags.DontSave};go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=mats;return go;}
void Tri(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 a,Vector3 b,Vector3 c){int i=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(i);t.Add(i+1);t.Add(i+2);i=v.Count;v.Add(a);v.Add(c);v.Add(b);t.Add(i);t.Add(i+1);t.Add(i+2);}
void Leaf(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 b,Vector3 dirH,float len,float rise,float w,float droop){
 Vector3 side=Vector3.Cross(Vector3.up,dirH).normalized;Vector3 tip=b+dirH*len+Vector3.up*(rise-droop);Vector3 mid=b+dirH*len*.45f+Vector3.up*rise*.8f;
 Vector3 L=mid-side*w*.5f,R=mid+side*w*.5f,M=mid+Vector3.up*w*.16f;Tri(v,t,b,L,M);Tri(v,t,b,M,R);Tri(v,t,L,tip,M);Tri(v,t,M,tip,R);}
void Plant(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 b,float h,System.Random rnd){
 int n=6+rnd.Next(3);float r=.15f+.32f*h;float off=(float)rnd.NextDouble()*6.28f;
 for(int i=0;i<n;i++){float a=off+i*6.2832f/n+((float)rnd.NextDouble()-.5f)*.5f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));bool inner=(i%2==1);
  float len=r*(inner?.55f:.95f)*(.85f+.3f*(float)rnd.NextDouble());float rise=h*(inner?.95f:.62f)*(.85f+.3f*(float)rnd.NextDouble());
  Leaf(v,t,b+d*.02f,d,len,rise,r*.72f,rise*(inner?.05f:.28f));}}
void Stake(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t,Vector3 b,Vector3 top,float rad){
 int s=5;var d=(top-b).normalized;var side=Vector3.Cross(d,Vector3.right).normalized;var up2=Vector3.Cross(d,side).normalized;
 for(int i=0;i<s;i++){float a0=i*6.2832f/s,a1=(i+1)*6.2832f/s;var o0=(side*Mathf.Cos(a0)+up2*Mathf.Sin(a0))*rad;var o1=(side*Mathf.Cos(a1)+up2*Mathf.Sin(a1))*rad;
  Tri(v,t,b+o0,b+o1,top+o0);Tri(v,t,b+o1,top+o1,top+o0);Tri(v,t,top+o0,top+o1,top);}}
float Noise(float th,float x){return .09f*Mathf.Sin(3*th+.7f)+.06f*Mathf.Sin(5*th+2.1f)+.04f*Mathf.Sin(8*th+.3f)+.035f*Mathf.Sin(x*1.3f+1f);}
var variants=new System.Collections.Generic.List<GameObject>();
foreach(bool bv in new[]{false,true}){
 string vname=bv?"B":"A";var rnd=new System.Random(bv?2610:1030);
 var variant=new GameObject(vname+"_field"){hideFlags=HideFlags.DontSave};variant.transform.SetParent(root.transform,false);variants.Add(variant);
 float wide=bv?12f:10f,deep=bv?9f:8f,a=wide*.5f,b=deep*.5f;
 // 줄 위치(local z). 가운데 흙길 1줄 + 양쪽 이랑. 흙길 위치는 약간 치우침.
 int perBed=bv?4:3;float zp=.30f,sp=1.05f;var rowZ=new System.Collections.Generic.List<float>();
 for(int k=0;k<perBed;k++){rowZ.Add(zp-1.0f-k*sp);rowZ.Add(zp+1.0f+k*sp);}
 rowZ.Sort();
 // 줄 중심선 = 등고선 따라 걷는다 (경사 클수록 등고선, 평평하면 밭 축)
 var bd=rot*Vector3.right;var bd2=new Vector2(bd.x,bd.z);
 System.Collections.Generic.List<Vector2> March(float lz){
  var s0w=ToWorld(0,lz);var pos=new Vector2(s0w.x,s0w.z);var res=new System.Collections.Generic.List<Vector2>();
  foreach(int sign in new[]{1,-1}){var p=pos;var prev=bd2*sign;var list=new System.Collections.Generic.List<Vector2>();float walked=0;
   while(walked<a+.4f){float e=1.2f;float gx=(Ground(p.x+e,p.y)-Ground(p.x-e,p.y))/(2*e),gz=(Ground(p.x,p.y+e)-Ground(p.x,p.y-e))/(2*e);var g=new Vector2(gx,gz);var ct=new Vector2(-g.y,g.x);
    if(ct.sqrMagnitude>1e-8f){ct.Normalize();if(Vector2.Dot(ct,prev)<0)ct=-ct;}else ct=prev;
    float w=Mathf.Clamp01((g.magnitude-.03f)/.12f)*.85f;var dir=Vector2.Lerp(bd2*sign,ct,w).normalized;dir=(prev*.7f+dir*.3f).normalized;prev=dir;p+=dir*.25f;walked+=.25f;list.Add(p);}
   if(sign==1)res.AddRange(list);else{list.Reverse();res.InsertRange(0,list);}}
  return res;}
 // 위 March 는 양방향을 이어붙인다 (시작점은 양쪽 첫 점 사이)
 var rowsPts=new System.Collections.Generic.List<System.Collections.Generic.List<Vector2>>();foreach(var lz in rowZ)rowsPts.Add(March(lz));
 var pathPts=March(zp);
 // 호길이
 float[] Cum(System.Collections.Generic.List<Vector2> p){var c=new float[p.Count];for(int i=1;i<p.Count;i++)c[i]=c[i-1]+Vector2.Distance(p[i-1],p[i]);return c;}
 var rowCum=new System.Collections.Generic.List<float[]>();foreach(var p in rowsPts)rowCum.Add(Cum(p));
 void Near(System.Collections.Generic.List<Vector2> pts,float[] cum,Vector2 q,out float dist,out float arc){dist=1e9f;arc=0;for(int i=0;i<pts.Count-1;i++){var s0=pts[i];var s1=pts[i+1];var d=s1-s0;float tt=Mathf.Clamp01(Vector2.Dot(q-s0,d)/Mathf.Max(d.sqrMagnitude,1e-6f));float dd=Vector2.Distance(q,s0+d*tt);if(dd<dist){dist=dd;arc=cum[i]+tt*d.magnitude;}}}
 var pathCum=Cum(pathPts);
 float[] rowH=new float[rowZ.Count];for(int i=0;i<rowH.Length;i++)rowH[i]=.21f+.07f*(float)rnd.NextDouble();
 // 밭 바닥+이랑: 한 장의 높이장
 float hw=.40f;float step=.12f;int nx=Mathf.CeilToInt((wide+1.2f)/step),nz=Mathf.CeilToInt((deep+1.2f)/step);
 var vv=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var fadeA=new System.Collections.Generic.List<float>();var pathA=new System.Collections.Generic.List<float>();
 float minGap=999,maxRidge=-999;
 for(int iz=0;iz<=nz;iz++)for(int ix=0;ix<=nx;ix++){float lx=-a-.6f+ix*step,lz=-b-.6f+iz*step;
  float q=Mathf.Pow(Mathf.Pow(Mathf.Abs(lx/a),4)+Mathf.Pow(Mathf.Abs(lz/b),4),.25f);float th=Mathf.Atan2(lz/b,lx/a);float bq=1+Noise(th,lx);
  float fade=1-Smooth(bq-.10f,bq+.02f,q);
  var w=ToWorld(lx,lz);var q2=new Vector2(w.x,w.z);float ridge=0;
  for(int r=0;r<rowsPts.Count;r++){float dist,arc;Near(rowsPts[r],rowCum[r],q2,out dist,out arc);if(dist<hw){float total=rowCum[r][rowCum[r].Length-1];float taper=Smooth(.5f,2.3f,Mathf.Min(arc,total-arc));float prof=Mathf.Pow(.5f*(1+Mathf.Cos(Mathf.PI*dist/hw)),.7f);float hh=rowH[r]*(1+.12f*Mathf.Sin(arc*1.7f+r));ridge=Mathf.Max(ridge,hh*prof*(.25f+.75f*taper));}}
  float dp,ap;Near(pathPts,pathCum,q2,out dp,out ap);float pathW=.5f+.07f*Mathf.Sin(ap*1.9f)+.0f;float endT=Mathf.Clamp01((Mathf.Min(ap,pathCum[pathCum.Length-1]-ap)-.6f)/1.4f);float isPath=dp<pathW*endT?1f:0f;
  float y=w.y+.025f+(isPath>0?-.012f:0f)+ridge*fade-(1-fade)*.16f;if(fade>.01f&&ridge>maxRidge)maxRidge=ridge;
  vv.Add(new Vector3(w.x,y,w.z));uv.Add(new Vector2(w.x/4f,w.z/4f));fadeA.Add(fade);pathA.Add(isPath>0?0f:9f);}
 var tSoil=new System.Collections.Generic.List<int>();var tPath=new System.Collections.Generic.List<int>();
 for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i0=iz*(nx+1)+ix,i1=i0+1,i2=i0+nx+1,i3=i2+1;
  if(fadeA[i0]<.001f&&fadeA[i1]<.001f&&fadeA[i2]<.001f&&fadeA[i3]<.001f)continue;
  float dpm=(pathA[i0]+pathA[i1]+pathA[i2]+pathA[i3])*.25f;var tl=dpm<=4.6f?tPath:tSoil;tl.AddRange(new[]{i0,i2,i1,i1,i2,i3});}
 var mesh=new Mesh(){indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vv);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(tSoil,0);mesh.SetTriangles(tPath,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
 MeshObj("tilled_soil",variant.transform,mesh,new[]{soilMat,pathMat});
 // 작물 포기
 var cv=new System.Collections.Generic.List<Vector3>();var ct=new System.Collections.Generic.List<int>();var cv2=new System.Collections.Generic.List<Vector3>();var ct2=new System.Collections.Generic.List<int>();
 var sv=new System.Collections.Generic.List<Vector3>();var st=new System.Collections.Generic.List<int>();
 int plants=0,gaps=0,stakes=0;float hMin=9,hMax=0;
 for(int r=0;r<rowsPts.Count;r++){var pts=rowsPts[r];var cum=rowCum[r];float total=cum[cum.Length-1];
  // 줄마다 자란 정도가 다르다: 한 줄은 덜 자랐다
  bool late=(r==(bv?3:2));float grow=late?.30f:(.42f+.14f*(float)rnd.NextDouble());float emptyP=late?.28f:.10f;
  float s=.9f;while(s<total-.9f){
   float jit=((float)rnd.NextDouble()-.5f)*.14f;float ss=s+jit;
   if(rnd.NextDouble()<emptyP){gaps++;s+=.56f;continue;}
   int j=0;while(j<cum.Length-2&&cum[j+1]<ss)j++;float tt=Mathf.Clamp01((ss-cum[j])/Mathf.Max(cum[j+1]-cum[j],1e-4f));var p2=Vector2.Lerp(pts[j],pts[j+1],tt);
   var gp=new Vector3(p2.x,Ground(p2.x,p2.y),p2.y);float q=Mathf.Pow(Mathf.Pow(Mathf.Abs((Quaternion.Inverse(rot)*(gp-center)).x/a),4)+Mathf.Pow(Mathf.Abs((Quaternion.Inverse(rot)*(gp-center)).z/b),4),.25f);
   if(q>.88f){s+=.56f;continue;}
   float h=Mathf.Clamp(grow*(.7f+.65f*(float)rnd.NextDouble()),.2f,.6f);hMin=Mathf.Min(hMin,h);hMax=Mathf.Max(hMax,h);
   float crest=rowH[r]*(1+.12f*Mathf.Sin(ss*1.7f+r));var baseP=new Vector3(p2.x+((float)rnd.NextDouble()-.5f)*.08f,gp.y+.025f+crest*.95f,p2.y);
   if(rnd.NextDouble()<.3)Plant(cv2,ct2,baseP,h,rnd);else Plant(cv,ct,baseP,h,rnd);plants++;
   if(r==(bv?4:3)&&(plants%4==0)&&stakes<6){Stake(sv,st,baseP+new Vector3(.12f,-.15f,.05f),baseP+new Vector3(.09f,.95f+.15f*(float)rnd.NextDouble(),.07f),.028f);stakes++;}
   s+=.56f+.08f*(float)rnd.NextDouble();}}
 Mesh CropMesh(System.Collections.Generic.List<Vector3> v,System.Collections.Generic.List<int> t){var m=new Mesh(){indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
 MeshObj("crop_plants",variant.transform,CropMesh(cv,ct),new[]{cropMat});MeshObj("crop_plants_light",variant.transform,CropMesh(cv2,ct2),new[]{crop2Mat});if(sv.Count>0)MeshObj("stakes",variant.transform,CropMesh(sv,st),new[]{woodMat});
 // 가장자리: 풀 덤불 + 흙 부스러기가 풀 쪽으로 번진다
 var tv=new System.Collections.Generic.List<Vector3>();var tt2=new System.Collections.Generic.List<int>();var fv=new System.Collections.Generic.List<Vector3>();var fuv=new System.Collections.Generic.List<Vector2>();var ft=new System.Collections.Generic.List<int>();
 int tufts=0,flecks=0;
 for(int k=0;k<56;k++){float th=6.2832f*(k+(float)rnd.NextDouble()*.8f)/56f;float cx=Mathf.Cos(th),sz=Mathf.Sin(th);float lx=a*Mathf.Sign(cx)*Mathf.Sqrt(Mathf.Abs(cx)),lz=b*Mathf.Sign(sz)*Mathf.Sqrt(Mathf.Abs(sz));
  float th2=Mathf.Atan2(lz/b,lx/a);float sc=1+Noise(th2,lx)+((float)rnd.NextDouble()-.35f)*.09f;lx*=sc;lz*=sc;var w=ToWorld(lx,lz);
  if(k%2==0){int nb=4+rnd.Next(3);for(int i=0;i<nb;i++){float ang=(float)rnd.NextDouble()*6.28f;var d=new Vector3(Mathf.Cos(ang),0,Mathf.Sin(ang));Leaf(tv,tt2,w+d*.06f+Vector3.up*.0f,d,.14f+.1f*(float)rnd.NextDouble(),.14f+.16f*(float)rnd.NextDouble(),.05f,.06f);}tufts++;}
  else{float rr=.10f+.2f*(float)rnd.NextDouble();float rz=rr*(.5f+.4f*(float)rnd.NextDouble());int c0=fv.Count;var cp=ToWorld(lx*(1.0f+.02f),lz*(1.0f+.02f));fv.Add(new Vector3(cp.x,cp.y+.03f,cp.z));fuv.Add(new Vector2(cp.x/4,cp.z/4));
   for(int i=0;i<8;i++){float ang=i*.7854f;float jr=.55f+.9f*(float)rnd.NextDouble();var q3=ToWorld(lx*1.02f+Mathf.Cos(ang)*rr*jr,lz*1.02f+Mathf.Sin(ang)*rz*jr);fv.Add(new Vector3(q3.x,q3.y+.012f,q3.z));fuv.Add(new Vector2(q3.x/4,q3.z/4));}
   for(int i=0;i<8;i++){ft.Add(c0);ft.Add(c0+1+(i+1)%8);ft.Add(c0+1+i);}flecks++;}}
 MeshObj("edge_tufts",variant.transform,CropMesh(tv,tt2),new[]{tuftMat});
 var fm=new Mesh(){indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};fm.SetVertices(fv);fm.SetUVs(0,fuv);fm.SetTriangles(ft,0);fm.RecalculateNormals();MeshObj("soil_flecks",variant.transform,fm,new[]{soilMat});
 int tris=0;foreach(var mf in variant.GetComponentsInChildren<MeshFilter>()){var m=mf.sharedMesh;for(int sm=0;sm<m.subMeshCount;sm++)tris+=(int)m.GetIndexCount(sm)/3;}
 log.AppendLine(vname+" rows="+rowZ.Count+" plants="+plants+" gaps="+gaps+" stakes="+stakes+" cropH="+hMin.ToString("F2")+".."+hMax.ToString("F2")+" ridgeH(max)="+maxRidge.ToString("F3")+" tufts="+tufts+" flecks="+flecks+" tris="+tris+" soilVerts="+vv.Count);
 variant.SetActive(false);
}
var sview=UnityEditor.SceneView.lastActiveSceneView;var src=sview!=null?sview.camera:Camera.main;if(!src)throw new System.Exception("No source camera");
var camGo=new GameObject("_FIELDT_CAMERA"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;var pcl=UnityEngine.Object.FindFirstObjectByType<PCLookScene>();if(pcl!=null)pcl.ApplySky(cam);else{cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.56f,.72f,.90f,1);}restoreProbe=true;var shp=new UnityEngine.Rendering.SphericalHarmonicsL2();shp.AddAmbientLight(RenderSettings.ambientLight*RenderSettings.ambientIntensity);RenderSettings.ambientProbe=shp;log.AppendLine("PCLookScene="+(pcl!=null)+" look="+(pcl!=null?pcl.look:-1));
void Shoot(string file,Vector3 pos,Vector3 tgt,float fov,int h){cam.fieldOfView=fov;camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 if(!PitCrewPlan.PlanIO.RenderPng(cam,outDir+file,1280,h,null))throw new System.Exception("Capture failed "+file);log.AppendLine(file+" pos="+pos+" target="+tgt+" fov="+fov+" res=1280x"+h);}
// 카트 눈높이: 남쪽 길 쪽에서 밭을 올려다본다
Vector3 cartPos=new Vector3(195f,Ground(195f,-66f)+1.0f,-66f),cartTgt=new Vector3(198,Ground(198,-57f)+.35f,-57f);
Shoot("new_base_cart.png",cartPos,cartTgt,70,720);
foreach(var v in variants){v.SetActive(true);string L=v.name.Substring(0,1);
 Shoot("new_"+L+"_high.png",new Vector3(196.881f,53.714f,-81.658f),new Vector3(197.27f,-95.469f,39.031f),60,611);
 Shoot("new_"+L+"_eye.png",new Vector3(190,Ground(190,-51)+1.65f,-51),new Vector3(198,Ground(198,-57)+.20f,-57),78,720);
 var tp=new Vector3(198,Ground(198,-57)+15f,-57);camGo.transform.SetPositionAndRotation(tp,Quaternion.LookRotation(Vector3.down,rot*Vector3.forward));cam.fieldOfView=40;PitCrewPlan.PlanIO.RenderPng(cam,outDir+"dbg_"+L+"_top.png",1280,720,null);
 Shoot("new_"+L+"_cart.png",cartPos,cartTgt,70,720);
 v.SetActive(false);}
log.AppendLine("raycasts="+rays+" colliders="+terrain.Count+" compiling="+UnityEditor.EditorApplication.isCompiling+" shaderErrors="+UnityEditor.ShaderUtil.ShaderHasError(shader));
}finally{
 if(restoreProbe)RenderSettings.ambientProbe=probeBackup;
 for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);
 bool dirtyBefore=scene.isDirty;
 if(scene.isDirty)UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path,UnityEditor.SceneManagement.OpenSceneMode.Single);
 scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 int remaining=0;foreach(var o in Resources.FindObjectsOfTypeAll<GameObject>())if(o.name.StartsWith("_FIELDT")||o.name=="A_field"||o.name=="B_field")remaining++;
 log.AppendLine("cleanup remaining="+remaining+" dirtyBeforeDiscard="+dirtyBefore+" dirty="+scene.isDirty+" playing="+Application.isPlaying);
 System.IO.File.WriteAllText(outDir+"render_evidence.txt",log.ToString());
}
return log.ToString();
