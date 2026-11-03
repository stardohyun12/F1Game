// Play: 사장 2.4 m 옆에 세우고 사장에서 80° 돌아선 채(사장 화면 밖) → 첫 장 찍고 바로 대화 시작 → 시선 이동 포함 7장(0.1초 간격).
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null || d.State != OnFootDirector.Mode.OnFoot) return "director not on foot: " + (d != null ? d.State.ToString() : "null");
var pl = d.Player; var cam = pl.Cam;
pl.simActive = true; cam.simActive = true; pl.simMove = Vector2.zero;
Vector3 bp = d.Boss.position;
Vector3 away = Quaternion.Euler(0, 40, 0) * d.Boss.forward; away.y = 0;
Vector3 p = bp + away.normalized * 2.4f;
pl.Teleport(p, 0f);
Vector3 to = bp - p; to.y = 0;
float yawToBoss = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
cam.Yaw = yawToBoss + 80f; cam.Pitch = 5f;
var r1 = PresentGrab.Begin("D:/Github/F1Game/docs/qa/evidence/ui-v1", "talk_offscreen", 7, 0.1f, 0.4f, () => d.ForceTalk());
return "start yawToBoss=" + yawToBoss.ToString("F1") + " camYaw=" + cam.Yaw.ToString("F1") + " grab=" + r1;
