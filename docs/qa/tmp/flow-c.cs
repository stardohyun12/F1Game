var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var item=ps.ActiveItem; var painter=item.GetComponent<PitCrew.Packing.WrapPainter>();
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera; var col=painter.GetType().GetField("targetCollider",bf).GetValue(painter) as Collider;
Physics.SyncTransforms(); var b=col.bounds; var c=b.center;
Vector2 top=cam.WorldToScreenPoint(c+new Vector3(0,b.extents.y*0.7f,0)); Vector2 bot=cam.WorldToScreenPoint(c-new Vector3(0,b.extents.y*0.7f,0));
painter.BeginStroke(top); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(top,bot,i/16f)); painter.EndStroke();
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-flow-2-packing.png");
return "painted item="+item.name;
