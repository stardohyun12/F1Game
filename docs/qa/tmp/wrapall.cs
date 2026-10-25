// 화물 5종 × 버블/하드케이스: 6방향으로 돌려가며 세로 띠(0.2m 간격, 살짝 겹침)로 겉면 전체를 감고 비용/보상 비율을 잰다. 상한은 측정 동안 끔.
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera;
var mats=Resources.LoadAll<PitCrew.Packing.WrapMaterialDef>("WrapMaterials");
var jobs=new System.Collections.Generic.Dictionary<string,OrderJob>(); foreach(var o in Resources.FindObjectsOfTypeAll<OrderJob>()) jobs[o.name]=o;
string[] names={"Order_B_Balanced","Order_04_WeddingCake","Order_02_Glass","Order_03_Fish","Order_01_Eggs"};
Vector3[] eul={new Vector3(0,180,0),new Vector3(0,90,0),new Vector3(0,0,0),new Vector3(0,270,0),new Vector3(90,180,0),new Vector3(-90,180,0)};
Vector3[] sides={Vector3.forward,Vector3.back,Vector3.left,Vector3.right,Vector3.up,Vector3.down};
var sb=new System.Text.StringBuilder();
foreach(var n in names){ foreach(var m in mats){ if(m.channelIndex==1) continue;
 ps.Show(jobs[n]); var item=ps.ActiveItem; var painter=item.GetComponent<PitCrew.Packing.WrapPainter>(); var ws=item.GetComponent<PitCrew.Packing.WrapState>();
 var col=painter.GetType().GetField("targetCollider",bf).GetValue(painter) as Collider;
 float cap=ws.CostCap; ws.CostCap=float.PositiveInfinity; painter.SetMaterial(m); int strokes=0;
 foreach(var e in eul){ item.transform.rotation=Quaternion.Euler(e); Physics.SyncTransforms(); var b=col.bounds;
  Vector3 R=cam.transform.right, U=cam.transform.up; float halfW=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(R.x),Mathf.Abs(R.y),Mathf.Abs(R.z)))); float halfH=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(U.x),Mathf.Abs(U.y),Mathf.Abs(U.z))));
  int cols=Mathf.Max(1,Mathf.CeilToInt(2f*halfW/0.2f));
  for(int s=0;s<cols;s++){ float x=-halfW+(2f*halfW)*(s+0.5f)/cols;
   Vector3 top=b.center+R*x+U*halfH*1.05f, bot=b.center+R*x-U*halfH*1.05f;
   // 물체 위 시작점 찾기
   Vector2 st=Vector2.zero; bool hit=false; for(int k=0;k<=20&&!hit;k++){ Vector3 w=Vector3.Lerp(top,bot,k/20f); Vector2 sp=cam.WorldToScreenPoint(w); if(col.Raycast(cam.ScreenPointToRay(sp),out var h,100f)){ st=sp; hit=true; } }
   if(!hit) continue; Vector2 ts=cam.WorldToScreenPoint(top), bs=cam.WorldToScreenPoint(bot);
   painter.BeginStroke(st); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(st,ts,i/16f)); painter.EndStroke();
   painter.BeginStroke(st); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(st,bs,i/16f)); painter.EndStroke(); strokes++; } }
 item.transform.rotation=Quaternion.Euler(0,180,0); Physics.SyncTransforms();
 float raw=ws.TotalCost; int rw=jobs[n].baseReward; string sa=""; foreach(var d in sides){ sa+=ws.QuerySideAbsorption(d,out int ch).ToString("F2")+","; }
 sb.Append(n+"/"+m.name+": strokes="+strokes+" raw="+raw.ToString("F0")+" ratio="+(raw/rw*100f).ToString("F1")+"% capped="+Mathf.Min(raw,cap).ToString("F0")+"(cap "+cap+") uvCov="+ws.ComputeCoverage().ToString("F2")+" sideAbs=["+sa+"]\n");
 ws.CostCap=cap; } }
return sb.ToString();
