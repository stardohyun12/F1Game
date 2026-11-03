// Play 앉은 상태: v9 = __V9__, yaw __YAW__ 에서 8초 깜빡임·프레임 ms(FrameFlickerProbe) → evidence/sunset10/play/__TAG___flicker.json
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset10/play/"; System.IO.Directory.CreateDirectory(dir);
Application.runInBackground = true;
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
look.dusk = 1f; look.v9 = __V9__; look.Apply();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(FishingSpot).GetField("pitch", bf).SetValue(spot, 0f);
typeof(FishingSpot).GetField("yaw", bf).SetValue(spot, Mathf.Clamp(__YAW__f, -spot.yawLeft, spot.yawRange));
FrameFlickerProbe.Begin(8f, dir, "__TAG__");
return "flick __TAG__ v9=" + look.v9;
