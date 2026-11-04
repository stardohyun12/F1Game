var name = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/cap_name.txt").Trim();
ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/room-daylight/" + name + ".png");
var cam = Camera.main;
return name + " cam " + cam.transform.position.ToString("F2") + " rot " + cam.transform.eulerAngles.ToString("F0");
