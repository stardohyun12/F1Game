DebugWindow.SetDusk(1f);
ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/room-daylight/dusk.png");
var sun = RenderSettings.sun;
return "sun int " + sun.intensity.ToString("F2") + " col " + sun.color + " rot " + sun.transform.eulerAngles.ToString("F0") + " toSun " + (-sun.transform.forward).ToString("F2");
