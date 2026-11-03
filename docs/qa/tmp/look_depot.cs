var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var s = DebugWindow.GoPlace(1);
d.Player.Cam.HoldLook = false;
d.Player.Cam.LookAt(new UnityEngine.Vector3(271.9f, 33f, -32.4f));
return s + " | player=" + d.Player.transform.position.ToString("F1") + " yaw=" + d.Player.Cam.Yaw + " state=" + d.State;
