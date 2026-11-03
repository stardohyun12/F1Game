DebugWindow.GoPlace(3);
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var fc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
fc.LookAt(Vector3.Lerp(d.Car.transform.position, d.Boss.position + Vector3.up * 1.6f, 0.3f));
return "ok";
