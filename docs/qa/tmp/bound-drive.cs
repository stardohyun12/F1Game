Application.runInBackground=true; Time.timeScale=1f;
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=c.GetComponent<Rigidbody>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); typeof(ArcadeCarController).GetField("fallRespawnY",bf|System.Reflection.BindingFlags.Public).SetValue(c,-9999f);
Vector3[] dirs={Vector3.right,Vector3.left,Vector3.forward,Vector3.back}; string[] names={"EAST","WEST","NORTH","SOUTH"};
// 각 방향: 벽까지 60~100m 직선이 비어 있는 출발점 탐색 (차 높이 구체 캐스트)
var starts=new Vector3[4]; var info=new System.Text.StringBuilder();
for(int d=0; d<4; d++){ var dir=dirs[d]; bool found=false;
 for(int k=0;k<400 && !found;k++){ float a=(k*7919)%1000/1000f, b=(k*104729)%1000/1000f; Vector3 p;
  if(d<2) p=new Vector3(d==0?Mathf.Lerp(80,120,a):Mathf.Lerp(60,100,a), 1f, Mathf.Lerp(0,130,b)); else p=new Vector3(Mathf.Lerp(0,180,b), 1f, d==2?Mathf.Lerp(30,60,a):Mathf.Lerp(70,100,a));
  var pc=new Vector3(p.x,0.55f,p.z); float dist=(d==0?(183-p.x):d==1?(p.x+9):d==2?(135-p.z):(p.z+9))-2f;
  if(!Physics.CheckSphere(pc+Vector3.up*0.5f,0.9f) && !Physics.SphereCast(pc,0.45f,dir,out var h,dist) && !Physics.SphereCast(pc+Vector3.up*1f,0.9f,dir,out var h2,dist)){ starts[d]=p; found=true; } }
 info.Append(names[d]+" start="+(found?starts[d].ToString():"NONE")+" ; "); }
if(starts[2]==Vector3.zero){ starts[2]=new Vector3(83f,1f,124f); info.Append("NORTH fallback start=(83,1,124) past quay; "); }
int cur=-1; float t0=0; float minY=0; int falls=0; float maxOut=0; var res=new System.Text.StringBuilder(); Vector3 last=Vector3.zero;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(cur>=0){ var p=rb.position; minY=Mathf.Min(minY,p.y); float outx=Mathf.Max(-9-p.x, p.x-183); float outz=Mathf.Max(-9-p.z, p.z-135); maxOut=Mathf.Max(maxOut,Mathf.Max(outx,outz)); if(p.y<-3f) falls++; }
 if(cur<0 || Time.time-t0>9f){ if(cur>=0){ res.Append(names[cur]+": end="+rb.position.ToString("F1")+" minY="+minY.ToString("F2")+" falls="+falls+" maxOutsideEdge="+maxOut.ToString("F2")+" v="+(rb.linearVelocity.magnitude*3.6f).ToString("F0")+"km/h; "); }
  cur++; if(cur>=4){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); UnityEditor.SessionState.SetString("qabound",res.ToString()); return; }
  if(starts[cur]==Vector3.zero){ res.Append(names[cur]+": no clear lane; "); t0=Time.time-100; return; }
  rb.position=starts[cur]; rb.rotation=Quaternion.LookRotation(dirs[cur]); rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; Physics.SyncTransforms(); minY=0; falls=0; maxOut=0; t0=Time.time; }
 fMove.SetValue(c,new Vector2(0f,1f)); };
UnityEditor.EditorApplication.update+=cb; return info.ToString();
