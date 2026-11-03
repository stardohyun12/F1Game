Application.runInBackground = true;
var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var sp = GameObject.Find("OnFoot_Spawn_Room").transform;
var o = "state=" + dir.State + " cal=" + (dir.Calendar != null) + " bed=" + (dir.Bed != null) + " day=" + DayLog.Day + "\n";
if (dir.Calendar != null) o += "calWritten: " + dir.Calendar.Written + "\n";
// 달력 쪽을 보고 선다(스폰에서 달력 방향, 아래로 12도)
Vector3 to = dir.Calendar.Center - sp.position; to.y = 0f;
float yaw = Quaternion.LookRotation(to).eulerAngles.y;
o += dir.DebugTeleport(sp.position, yaw, null, 0f) + "\n";
dir.Player.Cam.Pitch = 12f;
return o;
