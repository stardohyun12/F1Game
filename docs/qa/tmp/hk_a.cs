Application.runInBackground = true;
var r = DebugWindow.GoPlace(3);
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var fc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
var mid = Vector3.Lerp(d.Car.transform.position, d.Boss.position + Vector3.up * 1.2f, 0.25f);
fc.LookAt(mid);
return r + " state=" + d.State + " talked=" + d.Talked + " day=" + DayLog.Day + " car=" + d.Car.transform.position + " boss=" + d.Boss.position;
