var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>();
typeof(OrderBoard).GetMethod("OnCardClicked",bf).Invoke(ob,new object[]{1});
typeof(OrderBoard).GetMethod("OnBoardGoClicked",bf).Invoke(ob,null);
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var item=ps.ActiveItem;
var mats=ps.GetType().GetMethod("GetMaterials",bf).Invoke(ps,null) as PitCrew.Packing.WrapMaterialDef[];
ps.SelectMaterial(mats[2]);
var painter=item.GetComponent<PitCrew.Packing.WrapPainter>(); var ws=item.GetComponent<PitCrew.Packing.WrapState>();
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera; var col=painter.GetType().GetField("targetCollider",bf).GetValue(painter) as Collider;
string s="item="+item.name+"\n"; var g=ps.GetType().GetMethod("ComputeGrade",bf);
foreach(var yaw in new float[]{180f,270f,0f,90f}){
 item.transform.rotation=Quaternion.Euler(0,yaw,0); Physics.SyncTransforms(); var b=col.bounds; var c=b.center;
 Vector2 top=cam.WorldToScreenPoint(c+new Vector3(0,b.extents.y*0.8f,0)); Vector2 bot=cam.WorldToScreenPoint(c-new Vector3(0,b.extents.y*0.8f,0));
 painter.BeginStroke(top); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(top,bot,i/16f)); painter.EndStroke();
 object[] a=new object[]{0f}; s+="yaw "+yaw+" grade="+g.Invoke(ps,a)+" score="+a[0]+"\n"; }
return s;
