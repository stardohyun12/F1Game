var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var sit = DebugWindow.Fish();
var s = d.DebugSpot;
return sit + " | state=" + d.State + " seated=" + s.Seated + " landed=" + s.Landed + " fov=" + Camera.main.fieldOfView + " camPos=" + Camera.main.transform.position;
