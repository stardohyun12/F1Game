// 이전(10/29) 밭 목업을 같은 환경(사본 에디터)에서 재촬영 — 위/아래 비교 시트용. Allowed writes: art/_review/2026-10-30-field_terrace/old_*.png, old_evidence.txt
// Reuses field_build.cs Blob and rounded-ridge construction, adapted to actual terrain.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Pre-existing dirty scene; do not discard");
string outDir="D:/Github/F1Game/art/_review/2026-10-30-field_terrace/";
var owned=new System.Collections.Generic.List<UnityEngine.Object>();
var log=new System.Text.StringBuilder();
var terrain=new System.Collections.Generic.List<Collider>();
foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.enabled && c.gameObject.activeInHierarchy && (c is TerrainCollider || c.name.Contains("Terrain")))terrain.Add(c);
int rays=0;
float Ground(float x,float z){float best=float.NaN;var ray=new Ray(new Vector3(x,500,z),Vector3.down);foreach(var c in terrain){RaycastHit h;if(c.Raycast(ray,out h,1000)&& (float.IsNaN(best)||h.point.y>best))best=h.point.y;}rays++;if(float.IsNaN(best))throw new System.Exception("Ground miss "+x+","+z);return best;}
Vector3 Point(float x,float z,float lift){var q=Quaternion.Euler(0,10,0)*new Vector3(x,0,z)+new Vector3(198,0,-57);q.y=Ground(q.x,q.z)+lift;return q;}
var root=new GameObject("_FIELD_MAP_REVIEW_2354"){hideFlags=HideFlags.DontSave};owned.Add(root);
try {
var terrainMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_TownV6_Mats/TV6_Terrain.mat");
if(!terrainMaterial)throw new System.Exception("Terrain material missing");var shader=terrainMaterial.shader;
// Actual map shader has different light response from Studio A7. Only clone it in memory.
Color Sample(int r,int g,int b){return new Color(r/255f/.82f,g/255f/.78f,b/255f/.71f,1);}
Material Mat(string name,int r,int g,int b,float unusedBlotch){var m=new Material(terrainMaterial){name="_FIELD2354_"+name,hideFlags=HideFlags.HideAndDontSave};owned.Add(m);
 m.SetColor("_Tint",Sample(r,g,b));return m;}
log.AppendLine("materialTemplate=TV6_Terrain shader="+shader.name+" originalMaterialUnchanged=True");
var soil=Mat("soil",117,108,95,.04f);var ridge=Mat("ridge",164,148,126,.045f);
var crop=Mat("crop",102,112,89,.05f);var cropLight=Mat("crop_light",109,120,95,.04f);
var wood=Mat("wood",102,99,91,.03f);var stone=Mat("stone",153,148,134,.02f);var grass=Mat("grass",139,140,98,.06f);
GameObject MeshObject(string name,Transform parent,Vector3[] v,int[] t,Material mat){
 foreach(var p in v)if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.y))throw new System.Exception("Nonfinite "+name);
 var mesh=new Mesh(){name="_FIELD2354_"+name,hideFlags=HideFlags.HideAndDontSave};owned.Add(mesh);mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();
 var go=new GameObject(name){hideFlags=HideFlags.DontSave};go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;}
GameObject Blob(string name,Transform p,Vector3 center,Vector3 size,Material mat,int seed){const int rings=6,sides=12;var vv=new System.Collections.Generic.List<Vector3>();var tt=new System.Collections.Generic.List<int>();vv.Add(center+new Vector3(0,size.y,0));
 for(int y=1;y<rings;y++)for(int j=0;j<sides;j++){float v=Mathf.PI*y/rings,a=2*Mathf.PI*j/sides,noise=1+.09f*Mathf.Sin(a*3+seed)+.06f*Mathf.Cos(v*4+a*2+seed);vv.Add(center+Vector3.Scale(new Vector3(Mathf.Sin(v)*Mathf.Cos(a)*noise,Mathf.Cos(v),Mathf.Sin(v)*Mathf.Sin(a)*noise),size));}
 int bottom=vv.Count;vv.Add(center-new Vector3(0,size.y,0));for(int j=0;j<sides;j++){int next=(j+1)%sides;tt.AddRange(new[]{0,1+next,1+j,bottom,1+(rings-2)*sides+j,1+(rings-2)*sides+next});}
 for(int y=0;y<rings-2;y++)for(int j=0;j<sides;j++){int next=(j+1)%sides,a=1+y*sides+j,ar=1+y*sides+next;tt.AddRange(new[]{a,ar,ar+sides,a,ar+sides,a+sides});}return MeshObject(name,p,vv.ToArray(),tt.ToArray(),mat);}
void Pole(string name,Transform p,Vector3 bottom,Vector3 top,float radius){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.hideFlags=HideFlags.DontSave;go.transform.SetParent(p,false);go.transform.position=(bottom+top)*.5f;go.transform.up=(top-bottom).normalized;go.transform.localScale=new Vector3(radius*2,(top-bottom).magnitude*.5f,radius*2);go.GetComponent<Renderer>().sharedMaterial=wood;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());}
var variants=new System.Collections.Generic.List<GameObject>();
foreach(bool b in new[]{false,true}){
 var variant=new GameObject(b?"B_curved_12x9":"A_aligned_10x8"){hideFlags=HideFlags.DontSave};variant.transform.SetParent(root.transform,false);variants.Add(variant);
 float wide=b?12:10,deep=b?9:8;int rows=b?5:4;
 var vv=new System.Collections.Generic.List<Vector3>();var tt=new System.Collections.Generic.List<int>();int nx=60,nz=48;float minY=999,maxY=-999;
 for(int iz=0;iz<=nz;iz++)for(int ix=0;ix<=nx;ix++){float u=ix*2f/nx-1,v=iz*2f/nz-1;float x=wide*.5f*u*Mathf.Sqrt(1-.20f*Mathf.Pow(v,8)),z=deep*.5f*v*Mathf.Sqrt(1-.20f*Mathf.Pow(u,8));var p=Point(x,z,.025f);vv.Add(p);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
 for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int a=iz*(nx+1)+ix;tt.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});}
 var patch=MeshObject("worked_soil",variant.transform,vv.ToArray(),tt.ToArray(),soil);
 // Measure triangle interiors too; vertex-only tests miss a terrain crease.
 float minClear=999,maxClear=-999;int tests=0;
 var bary=new[]{new Vector3(1f/3,1f/3,1f/3),new Vector3(.5f,.5f,0),new Vector3(0,.5f,.5f),new Vector3(.5f,0,.5f)};
 for(int t=0;t<tt.Count;t+=3)foreach(var w in bary){var q=vv[tt[t]]*w.x+vv[tt[t+1]]*w.y+vv[tt[t+2]]*w.z;float gap=q.y-Ground(q.x,q.z);minClear=Mathf.Min(minClear,gap);maxClear=Mathf.Max(maxClear,gap);tests++;}
 if(minClear<-.002f||maxClear>.065f)throw new System.Exception("Patch clearance "+minClear+".."+maxClear);
 // Close the 2.5 cm surface bias into the ground at the perimeter.
 var edge=new System.Collections.Generic.List<Vector3>();for(int i=0;i<=nx;i++)edge.Add(vv[i]);for(int i=1;i<=nz;i++)edge.Add(vv[i*(nx+1)+nx]);for(int i=nx-1;i>=0;i--)edge.Add(vv[nz*(nx+1)+i]);for(int i=nz-1;i>0;i--)edge.Add(vv[i*(nx+1)]);
 var ev=new System.Collections.Generic.List<Vector3>();var et=new System.Collections.Generic.List<int>();for(int i=0;i<edge.Count;i++){ev.Add(edge[i]);ev.Add(edge[i]-Vector3.up*.065f);}for(int i=0;i<edge.Count;i++){int a=i*2,c=((i+1)%edge.Count)*2;et.AddRange(new[]{a,c,a+1,c,c+1,a+1,a,a+1,c,c,a+1,c+1});}MeshObject("soil_edge_to_ground",variant.transform,ev.ToArray(),et.ToArray(),soil);
 float len=deep-1.25f,spacing=b?2.2f:2.25f;float ridgeMin=999,ridgeMax=-999;
 for(int row=0;row<rows;row++){
  float rx=(row-(rows-1)*.5f)*spacing;vv.Clear();tt.Clear();int along=40,across=12;
  for(int j=0;j<=along;j++){float t=j/(float)along,z=(t-.5f)*len,curve=b?.68f*Mathf.Sin(t*Mathf.PI)-.30f:0,taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.24f);
   for(int k=0;k<=across;k++){float a=Mathf.PI*k/across,x=rx+curve+Mathf.Cos(a)*.70f*taper;vv.Add(Point(x,z,.035f+.21f*Mathf.Sin(a)*taper));}}
  for(int j=0;j<along;j++)for(int k=0;k<across;k++){int a=j*(across+1)+k;tt.AddRange(new[]{a,a+1,a+across+1,a+1,a+across+2,a+across+1});}
  MeshObject("rounded_ridge_"+(row+1),variant.transform,vv.ToArray(),tt.ToArray(),ridge);
  for(int j=0;j<=along;j++){foreach(int k in new[]{0,across}){var p=vv[j*(across+1)+k];float gap=p.y-Ground(p.x,p.z);ridgeMin=Mathf.Min(ridgeMin,gap);ridgeMax=Mathf.Max(ridgeMax,gap);}}
  for(int j=0;j<7;j++){if(row==2&&j>3)continue;float t=(j+.55f)/7.1f,z=(t-.5f)*len,x=rx+(b?.68f*Mathf.Sin(t*Mathf.PI)-.30f:0),sy=row==rows-1?.30f:.17f;
   // Crop lower tip enters the ridge crest; no floating crop centers on slopes.
   var center=Point(x,z,.21f+sy*.70f);Blob("crop_mass_"+row+"_"+j,variant.transform,center,new Vector3(.38f,sy,.39f),j%3==0?cropLight:crop,row*9+j);
   if(row==rows-1&&j%2==0)Pole("support_"+j,variant.transform,Point(x+.13f,z,-.025f),Point(x+.10f,z+.06f,1.1f),.026f);
  }
 }
 for(int j=0;j<5;j++){float x=-wide*.44f+j*wide*.22f,z=deep*.51f;Pole("weathered_post_"+j,variant.transform,Point(x,z,-.04f),Point(x+.035f*Mathf.Sin(j),z+.03f,.56f),.048f);}
 for(int j=0;j<5;j++){float x=-wide*.5f-1.35f+j*.45f,z=-deep*.40f+.1f*Mathf.Sin(j);Blob("door_step_"+j,variant.transform,Point(x,z,.035f),new Vector3(.28f,.065f,.22f),stone,50+j);}
 if(b)for(int j=0;j<12;j++){float a=j*Mathf.PI*2/12,x=Mathf.Cos(a)*wide*.49f,z=Mathf.Sin(a)*deep*.49f;Blob("soft_grass_edge_"+j,variant.transform,Point(x,z,.01f),new Vector3(.42f,.075f,.32f),grass,70+j);}
 int tris=0;foreach(var mf in variant.GetComponentsInChildren<MeshFilter>())tris+=mf.sharedMesh.triangles.Length/3;
 log.AppendLine(variant.name+" rows="+rows+" tris="+tris+" groundHeightSpan="+(maxY-minY)+" soilInteriorTests="+tests+" soilClearance="+minClear+".."+maxClear+" ridgeFeet="+ridgeMin+".."+ridgeMax);
 variant.SetActive(false);
}
var sv=UnityEditor.SceneView.lastActiveSceneView;var src=sv!=null?sv.camera:Camera.main;if(!src)throw new System.Exception("No source camera");
var camGo=new GameObject("_FIELD2354_CAMERA"){hideFlags=HideFlags.HideAndDontSave};owned.Add(camGo);var cam=camGo.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.56f,.72f,.90f,1);
foreach(var v in variants){v.SetActive(true);string letter=v.name.Substring(0,1);
 foreach(string view in new[]{"high","eye","cart"}){int h;Vector3 pos,tgt;
 if(view=="high"){pos=new Vector3(196.881f,53.714f,-81.658f);tgt=new Vector3(197.27f,-95.469f,39.031f);cam.fieldOfView=60;h=611;}
 else if(view=="eye"){pos=new Vector3(190,Ground(190,-51)+1.65f,-51);tgt=new Vector3(198,Ground(198,-57)+.20f,-57);cam.fieldOfView=78;h=720;}
 else {pos=new Vector3(195f,Ground(195f,-66f)+1.0f,-66f);tgt=new Vector3(198,Ground(198,-57f)+.35f,-57f);cam.fieldOfView=70;h=720;}
 camGo.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
 if(!PitCrewPlan.PlanIO.RenderPng(cam,outDir+"old_"+letter+"_"+view+".png",1280,h,null))throw new System.Exception("Capture failed");
 log.AppendLine(letter+"_"+view+" pos="+pos+" target="+tgt+" fov="+cam.fieldOfView+" resolution=1280x"+h);
 }
 v.SetActive(false);
}
log.AppendLine("raycasts="+rays+" terrainColliders="+terrain.Count+" compiling="+UnityEditor.EditorApplication.isCompiling+" shaderErrors="+UnityEditor.ShaderUtil.ShaderHasError(shader));
}finally{
 for(int i=owned.Count-1;i>=0;i--)if(owned[i])UnityEngine.Object.DestroyImmediate(owned[i]);
 bool dirtyBeforeDiscard=scene.isDirty;
 if(scene.isDirty)UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path,UnityEditor.SceneManagement.OpenSceneMode.Single);
 scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 int remaining=0;foreach(var o in Resources.FindObjectsOfTypeAll<GameObject>())if(o.name.StartsWith("_FIELD2354")||o.name=="_FIELD_MAP_REVIEW_2354")remaining++;
 log.AppendLine("cleanup remaining="+remaining+" dirtyBeforeDiscard="+dirtyBeforeDiscard+" dirty="+scene.isDirty+" playing="+Application.isPlaying+" compiling="+UnityEditor.EditorApplication.isCompiling);
 System.IO.File.WriteAllText(outDir+"old_evidence.txt",log.ToString());
}
return log.ToString();
