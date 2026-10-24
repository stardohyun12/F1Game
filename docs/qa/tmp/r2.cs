var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var item=ps.ActiveItem;
var mats=ps.GetType().GetMethod("GetMaterials",bf).Invoke(ps,null) as PitCrew.Packing.WrapMaterialDef[];
ps.SelectMaterial(mats[2]);
var painter=item.GetComponent<PitCrew.Packing.WrapPainter>(); var ws=item.GetComponent<PitCrew.Packing.WrapState>();
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera; var col=painter.GetType().GetField("targetCollider",bf).GetValue(painter) as Collider;
var orders=ps.GetType().GetField("currentOrders",bf).GetValue(ps) as System.Collections.Generic.List<OrderJob>;
string s="order="+orders[0].name+" baseReward="+orders[0].baseReward+" cap="+ws.CostCap+" wallet="+UpgradeSystem.Instance.Wallet+"\n";
foreach(var yaw in new float[]{180f,270f,0f,90f}){
 item.transform.rotation=Quaternion.Euler(0,yaw,0); Physics.SyncTransforms(); var b=col.bounds; var c=b.center;
 for(int k=-2;k<=2;k++){ float dx=b.extents.x*0.35f*k;
  Vector2 top=cam.WorldToScreenPoint(c+new Vector3(dx,b.extents.y*0.95f,0)); Vector2 bot=cam.WorldToScreenPoint(c+new Vector3(dx,-b.extents.y*0.95f,0));
  painter.BeginStroke(top); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(top,bot,i/16f)); painter.EndStroke(); }
 s+="yaw "+yaw+" cost="+ws.TotalCost+"\n"; }
item.transform.rotation=Quaternion.Euler(0,180,0);
var g=ps.GetType().GetMethod("ComputeGrade",bf); object[] a=new object[]{0f}; s+="grade="+g.Invoke(ps,a)+" score="+a[0]+" ratio="+(ws.TotalCost/orders[0].baseReward);
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-1504-packing-full.png");
return s;
