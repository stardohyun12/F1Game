// 2203: 무드 적용 + 차를 본선 1040 에 세우고 주행 준비. SessionState ts3.mood = "a"|"s"
Application.runInBackground = true;
bool sunset = UnityEditor.SessionState.GetString("ts3.mood", "a") == "s";
MoodSwitcher.Apply(sunset ? MoodSwitcher.PathSunset : MoodSwitcher.PathAfternoon);
UnityEngine.Object.FindFirstObjectByType<PCLookScene>().Tick();
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var P = rs.centerline;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var rb = car.GetComponent<Rigidbody>();
rb.isKinematic = false;
var f = P[1044] - P[1036]; f.y = 0;
rb.position = P[1040] + Vector3.up * .6f; rb.rotation = Quaternion.LookRotation(f.normalized);
car.transform.SetPositionAndRotation(rb.position, rb.rotation);
rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
UnityEditor.SessionState.SetString("l5c.tag", sunset ? "ts3_sunset" : "ts3_afternoon");
return "placed " + (sunset ? "sunset" : "afternoon") + " stage=" + (Services.Run as RunController).CurrentStage + " kin=" + rb.isKinematic + " ts=" + Time.timeScale;
