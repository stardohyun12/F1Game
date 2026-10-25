// 다른 손: 가로 띠, 간격 0.14~0.26m 랜덤(시드 고정), 약간 기울임. 유리·케이크 × 버블/하드. 6방향.
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera;
var mats=Resources.LoadAll<PitCrew.Packing.WrapMaterialDef>("WrapMaterials");
var jobs=new System.Collections.Generic.Dictionary<string,OrderJob>(); foreach(var o in Resources.FindObjectsOfTypeAll<OrderJob>()) jobs[o.name]=o;
string[] names={"Order_02_Glass","Order_04_WeddingCake"};
Vector3[] eul={new Vector3(0,180,0),new Vector3(0,90,0),new Vector3(0,0,0),new Vector3(0,270,0),new Vector3(90,180,0),new Vector3(-90,180,0)};
var sb=new System.Text.StringBuilder(); var rng=new System.Random(7);
foreach(var n in names){ foreach(var m in mats){ if(m.channelIndex==1) continue;
 ps.Show(jobs[n]); var item=ps.ActiveItem; var painter=item.GetComponent<PitCrew.Packing.WrapPainter>(); var ws=item.GetComponent<PitCrew.Packing.WrapState>();
 var col=painter.GetType().GetField("targetCollider",bf).GetValue(painter) as Collider;
 float cap=ws.CostCap; ws.CostCap=float.PositiveInfinity; painter.SetMaterial(m); int strokes=0; float spacingSum=0;
 foreach(var e in eul){ item.transform.rotation=Quaternion.Euler(e); Physics.SyncTransforms(); var b=col.bounds;
  Vector3 R=cam.transform.right, U=cam.transform.up; float halfW=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(R.x),Mathf.Abs(R.y),Mathf.Abs(R.z)))); float halfH=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(U.x),Mathf.Abs(U.y),Mathf.Abs(U.z))));
  float y=-halfH+0.08f;
  while(y<halfH+0.05f){ float tilt=(float)(rng.NextDouble()-0.5)*0.12f;
   Vector3 L=b.center+U*y-R*halfW*1.05f+U*tilt, Rr=b.center+U*y+R*halfW*1.05f-U*tilt;
   Vector2 st=Vector2.zero; bool hit=false; for(int k=0;k<=24&&!hit;k++){ Vector3 w=Vector3.Lerp(L,Rr,k/24f); Vector2 sp=cam.WorldToScreenPoint(w); if(col.Raycast(cam.ScreenPointToRay(sp),out var h,100f)){ st=sp; hit=true; } }
   float step=0.14f+(float)rng.NextDouble()*0.12f;
   if(hit){ Vector2 ls=cam.WorldToScreenPoint(L), rs=cam.WorldToScreenPoint(Rr);
    painter.BeginStroke(st); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(st,ls,i/16f)); painter.EndStroke();
    painter.BeginStroke(st); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(st,rs,i/16f)); painter.EndStroke(); strokes++; spacingSum+=step; }
   y+=step; } }
 item.transform.rotation=Quaternion.Euler(0,180,0); Physics.SyncTransforms();
 float raw=ws.TotalCost; int rw=jobs[n].baseReward;
 sb.Append(n+"/"+m.name+": strokes="+strokes+" avgStep="+(spacingSum/Mathf.Max(1,strokes)).ToString("F2")+" raw="+raw.ToString("F0")+" ratio="+(raw/rw*100f).ToString("F1")+"% uvCov="+ws.ComputeCoverage().ToString("F2")+" label="+"\n");
 ws.CostCap=cap; } }
return sb.ToString();
