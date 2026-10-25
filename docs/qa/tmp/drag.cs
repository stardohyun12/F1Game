var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var item=ps.ActiveItem; var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera;
Physics.SyncTransforms();
var rend=item.GetComponentInChildren<Renderer>(); Vector2 sp=cam.WorldToScreenPoint(rend.bounds.center);
ps.BeginTrunkDrag(sp);
return "phase="+ps.GetType().GetField("phase",bf).GetValue(ps)+" dragging="+ps.IsDraggingTrunkItem+" sp="+sp+" rot="+item.transform.eulerAngles;
