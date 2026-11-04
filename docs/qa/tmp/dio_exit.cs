// 방 문(DOOR_ROOM) 바로 바깥 복도로 — 방 상자 좌표에서 문 쪽(-z)으로 상자 밖. 방을 등지고 선다
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var room = UnityEngine.GameObject.Find("ROOM_2F").transform;
A7ShadeGlobals.TryRoomBox(room, out var box);
var inv = box.inverse;
UnityEngine.Transform door = null;
foreach (var t in room.parent.GetComponentsInChildren<UnityEngine.Transform>()) if (t.name == "DOOR_ROOM") door = t;
var dq = box.MultiplyPoint3x4(door.GetComponent<UnityEngine.Renderer>().bounds.center);
var p = inv.MultiplyPoint3x4(new UnityEngine.Vector3(dq.x, 0f, -1.45f)); p.y = 28.5f;
var outDir = inv.MultiplyVector(UnityEngine.Vector3.back); outDir.y = 0f;
float yaw = UnityEngine.Mathf.Atan2(outDir.x, outDir.z) * UnityEngine.Mathf.Rad2Deg;
d.DebugTeleport(p, yaw, null, 0f);
var f = UnityEngine.Camera.main.GetComponent<OnFootCamera>(); f.Pitch = 5f;
return "door q=" + dq.ToString("F2") + " tp=" + p.ToString("F2") + " yaw=" + yaw.ToString("F0");
