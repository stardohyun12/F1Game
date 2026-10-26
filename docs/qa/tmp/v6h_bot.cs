// 과제 0640 — 봇 cover 를 주행 방식 지정해서 시작 (Play 중). 첫 줄 MODE 를 Box / Wheels 로 바꿔 쓴다(v6h_bot.sh 가 치환).
var MODE = ArcadeCarController.DriveMode.Box;
Application.runInBackground = true;
foreach (var b in UnityEngine.Object.FindObjectsByType<TownPathBot>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(b.gameObject);
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
car.SetDriveMode(MODE);
TownPathBot.Only = ""; TownPathBot.Status = "starting"; TownPathBot.Begin("cover", 8f);
return "started " + car.Mode;
