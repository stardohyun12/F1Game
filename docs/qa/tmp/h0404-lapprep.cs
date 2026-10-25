// 과제 0404 사본(가장 긴 main 중심선): 본선 1바퀴 준비 — 0226 보고서대로 차를 본선 중심선 8번 점에 두고 EnterFreeDrive
var rc = Services.Run as RunController;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
var main = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var p = main.centerline[8]; var f = main.centerline[12] - main.centerline[4]; f.y = 0;
rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
rb.position = p + Vector3.up * 0.6f; rb.rotation = Quaternion.LookRotation(f.normalized);
car.transform.SetPositionAndRotation(rb.position, rb.rotation);
var m = typeof(RunController).GetMethod("EnterFreeDrive");
m.Invoke(rc, null);
UnityEditor.SessionState.SetString("bl.road", "main");
UnityEditor.SessionState.SetString("bl.drive", "");
return "prep stage=" + rc.CurrentStage + " at=" + p;
