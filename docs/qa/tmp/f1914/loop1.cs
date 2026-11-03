// 1914 검증: stuck0338_verify.cs 사본, LoopN=1 (정·역 1회씩). 봇은 이 스크립트가 Play 중에만 만든다(씬 저장 금지).
var MODE = ArcadeCarController.DriveMode.Box;
Application.runInBackground = true;
foreach (var b in UnityEngine.Object.FindObjectsByType<TownPathBot>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(b.gameObject);
var ot = UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>(); if (ot) { ot.StopAllCoroutines(); ot.enabled = false; }
var rcx = UnityEngine.Object.FindFirstObjectByType<RunController>(); var bfx = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; typeof(RunController).GetMethod("SetStage", bfx).Invoke(rcx, new object[] { RunStage.Driving }); typeof(RunController).GetMethod("StartFollowCameraAndCloseBoard", bfx).Invoke(rcx, null);
var fc = Camera.main.GetComponent<FollowCamera>(); if (fc) fc.enabled = true;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); car.SetDriveMode(MODE);
string f = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/d198/loop_pts.txt"));
TownPathBot.LoopPts = System.IO.File.ReadAllLines(f).Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
TownPathBot.LoopN = 1; TownPathBot.StuckProbe = false; TownPathBot.Only = ""; TownPathBot.Status = "starting"; TownPathBot.Begin("loop" + MODE, 8f);
return "started " + car.Mode + " pts " + TownPathBot.LoopPts.Count;
