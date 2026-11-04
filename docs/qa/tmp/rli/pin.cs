// 플레이어를 옮겨 디오라마 카메라를 전 그림(185.02, 33.02, -65.06)과 같은 자리에 맞춘다
var d = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>();
var pl = (Transform)typeof(RoomDiorama).GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(d);
var cam = d.GetComponent<Camera>();
var want = new Vector3(185.02f, 33.02f, -65.06f);
var delta = want - cam.transform.position; delta.y = 0f;
var cc = pl.GetComponent<CharacterController>();
if (cc) cc.enabled = false;
pl.position += delta;
if (cc) cc.enabled = true;
return "player " + pl.position.ToString("F3") + " moved " + delta.ToString("F3");
