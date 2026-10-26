// 1548 (Play): 차를 해안 직선 main #1200 에 세우고 카메라 프리셋 B(HighRoad)로. 다음 exec 에서 l1548-shots.cs
Application.runInBackground = true;
var P = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First().centerline;
int i = int.Parse(UnityEditor.SessionState.GetString("l1548.at", "1200"));
var f = P[i + 3] - P[i - 3]; f.y = 0; f.Normalize();
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
rb.position = P[i] + Vector3.up * 0.6f; rb.rotation = Quaternion.LookRotation(f); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>();
var pre = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>("Assets/Settings/Camera/CameraFeel_B_HighRoad.asset");
string old = fc.Preset ? fc.Preset.name : "null"; fc.Preset = pre;
var rends = car.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).Select(r => r.name + ":" + (r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "-")).Take(5);
return "car at #" + i + " " + P[i].ToString("F0") + " preset " + old + " -> " + pre.name + " | " + string.Join(", ", rends) + " peds " + UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None).Length;
