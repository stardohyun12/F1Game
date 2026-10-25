// 민감도: wrapall.cs 와 같은 패턴인데 띠 간격만 SessionState "qasp"(m) 로 바꾼다. 버블만. 보정(0.2m) 대비 사람 습관에 따른 흔들림.
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
float SP=float.Parse(UnityEditor.SessionState.GetString("qasp","0.2"));
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var cam=ps.PackingCamera;
var m=Resources.Load<PitCrew.Packing.WrapMaterialDef>("WrapMaterials/Bubble");
var jobs=new System.Collections.Generic.Dictionary<string,OrderJob>(); foreach(var o in Resources.FindObjectsOfTypeAll<OrderJob>()) jobs[o.name]=o;
string[] names={"Order_B_Balanced","Order_04_WeddingCake","Order_02_Glass","Order_03_Fish","Order_01_Eggs"};
Vector3[] eul={new Vector3(0,180,0),new Vector3(0,90,0),new Vector3(0,0,0),new Vector3(0,270,0),new Vector3(90,180,0),new Vector3(-90,180,0)};
Vector3[] sides={Vector3.forward,Vector3.back,Vector3.left,Vector3.right,Vector3.up,Vector3.down};
var sb=new System.Text.StringBuilder("spacing="+SP+"\n");
foreach(var n in names){
 ps.Show(jobs[n]); var item=ps.ActiveItem; var painter=item.GetComponent<PitCrew.Packing.WrapPainter>(); var ws=item.GetComponent<PitCrew.Packing.WrapState>();
 var col=painter.TargetCollider; float cap=ws.CostCap; ws.CostCap=float.PositiveInfinity; painter.StripWidthUV=0.06f; painter.SetMaterial(m); int strokes=0;
 foreach(var e in eul){ item.transform.rotation=Quaternion.Euler(e); Physics.SyncTransforms(); var b=col.bounds;
  Vector3 R=cam.transform.right, U=cam.transform.up; float halfW=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(R.x),Mathf.Abs(R.y),Mathf.Abs(R.z)))); float halfH=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(U.x),Mathf.Abs(U.y),Mathf.Abs(U.z))));
  int cols=Mathf.Max(1,Mathf.CeilToInt(2f*halfW/SP));
  for(int s=0;s<cols;s++){ float x=-halfW+(2f*halfW)*(s+0.5f)/cols;
   Vector3 top=b.center+R*x+U*halfH*1.05f, bot=b.center+R*x-U*halfH*1.05f;
   Vector2 st=Vector2.zero; bool hit=false; for(int k=0;k<=20&&!hit;k++){ Vector3 w=Vector3.Lerp(top,bot,k/20f); Vector2 sp=cam.WorldToScreenPoint(w); if(col.Raycast(cam.ScreenPointToRay(sp),out var h,100f)){ st=sp; hit=true; } }
   if(!hit) continue; Vector2 ts=cam.WorldToScreenPoint(top), bs=cam.WorldToScreenPoint(bot);
   painter.BeginStroke(st); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(st,ts,i/16f)); painter.EndStroke();
   painter.BeginStroke(st); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(st,bs,i/16f)); painter.EndStroke(); strokes++; } }
 item.transform.rotation=Quaternion.Euler(0,180,0); Physics.SyncTransforms();
 float raw=ws.TotalCost; int rw=jobs[n].baseReward; int full=0; foreach(var d in sides){ if(ws.QuerySideAbsorption(d,out int ch)>=0.24f) full++; }
 sb.Append(n+": strokes="+strokes+" ratio="+(raw/rw*100f).ToString("F1")+"% uvCov="+ws.ComputeCoverage().ToString("F2")+" fullSides="+full+"/6\n");
 ws.CostCap=cap; }
return sb.ToString();
