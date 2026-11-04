// PlayerPrefs "intgo" = "<interior 이름>|a" (문 쪽 안) 또는 "|b" (반대쪽 구석)
Time.timeScale = 1f;
var key = PlayerPrefs.GetString("intgo").Split('|');
var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var it = list.Find(i => i.name == key[0]);
var doors = UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
float yaw = RoomDiorama.FixedYaw(it, list, doors, null);
Vector3 f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
Vector3 fb = it.box.MultiplyVector(f); fb.y = 0f; fb.Normalize();   // 상자 좌표에서 카메라가 보는 쪽
Vector3 lat = new Vector3(-fb.z, 0f, fb.x);
Vector3 q = key[1] == "a" ? -fb * 0.6f : fb * 0.6f + lat * 0.6f;
q.y = -1f;
var inv = it.box.inverse;
Vector3 feet = inv.MultiplyPoint3x4(q) + Vector3.up * 0.1f;
string r = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>().DebugTeleport(feet, yaw, null, 0f);
return it.name + " yaw=" + yaw.ToString("F1") + " feet=" + feet.ToString("F2") + " " + r;
