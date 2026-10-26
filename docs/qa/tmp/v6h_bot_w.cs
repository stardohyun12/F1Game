// 과제 0640 — 봇 cover, Wheels 주행 (Play 중)
Application.runInBackground = true;
foreach (var b in UnityEngine.Object.FindObjectsByType<TownPathBot>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(b.gameObject);
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
car.SetDriveMode(ArcadeCarController.DriveMode.Wheels);
TownPathBot.Only = ""; TownPathBot.Status = "starting"; TownPathBot.Begin("cover", 8f);
return "started " + car.Mode;
