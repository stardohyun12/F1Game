Application.runInBackground = true;
Time.captureFramerate = 0;
OnFootRouteBot.PackScenario = "full";
OnFootRouteBot.FullThrottleWhenSlow = true;
var car = GameObject.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
car.SetFeelPreset(DriveFeelPreset.A_Now);
string p = car.FeelPreset.ToString();
return p + " " + OnFootRouteBot.BeginFirstOrder("D:/Github/F1Game/docs/qa/evidence/drive-feel/bot_" + p + "_fullthr", "D:/Github/F1Game/docs/qa/evidence/drive-feel/bot_" + p + "_fullthr");
