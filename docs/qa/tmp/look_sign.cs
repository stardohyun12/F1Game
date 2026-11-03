var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var s = DebugWindow.GoPlace(1);
var sign = d.transform.Find("OnFoot_Depot/DepotRoadSign/Plate");
d.Player.Cam.HoldLook = false;
d.Player.Cam.LookAt(sign.position);
return s + " | player=" + d.Player.transform.position.ToString("F1") + " yaw=" + d.Player.Cam.Yaw + " state=" + d.State + " cart=" + (d.Car != null ? d.Car.transform.position.ToString("F1") : "null");
