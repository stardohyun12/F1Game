// 같은 카메라: 고개 yaw 41 을 눈·디오라마 양쪽에 바로 넣고(부드럽게 따라가기 건너뜀) 이 프레임 끝에 찍는다
var name = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/rli/shot.txt").Trim();
var f = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
f.Yaw = 41f;
var d = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>();
var fl = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(RoomDiorama).GetField("yawNow", fl).SetValue(d, 41f);
typeof(RoomDiorama).GetField("yawVel", fl).SetValue(d, 0f);
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName("D:/Github/F1Game/" + name));
ScreenCapture.CaptureScreenshot("D:/Github/F1Game/" + name);
var cam = Camera.main;
return name + " cam " + cam.transform.position.ToString("F2") + " rot " + cam.transform.eulerAngles.ToString("F0") + " dio " + RoomDiorama.Active;
