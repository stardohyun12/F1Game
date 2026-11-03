// Play 앉은 상태: 시선 yaw = 35 로 맞추고 Present 거친 화면 + 8초 깜빡임·프레임(right)
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset8/play/"; System.IO.Directory.CreateDirectory(dir);
Application.runInBackground = true;
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(FishingSpot).GetField("pitch", bf).SetValue(spot, 0f);
typeof(FishingSpot).GetField("yaw", bf).SetValue(spot, Mathf.Clamp(35f, -spot.yawLeft, spot.yawRange));
var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
string info = "fov=" + cam.fieldOfView.ToString("F0") + " lrt=" + (lr ? lr.RenderSize.ToString() : "-") + " yaw=" + typeof(FishingSpot).GetField("yaw", bf).GetValue(spot);
System.IO.File.AppendAllText(dir + "rt_log.txt", "right " + info + "\n");
FrameFlickerProbe.Begin(8f, dir, "right");
return info;
