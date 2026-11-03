// 0338: 끼임 구간만 짧게 정·역 — loop_pts 의 [A,B] 구간. 실험 토글은 아래 줄을 고쳐서.
int A = 540, B = 811; var MODE = ArcadeCarController.DriveMode.Box; int N = 1;
Application.runInBackground = true;
foreach (var b in UnityEngine.Object.FindObjectsByType<TownPathBot>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(b.gameObject);
var rcx = UnityEngine.Object.FindFirstObjectByType<RunController>(); var bfx = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; typeof(RunController).GetMethod("SetStage", bfx).Invoke(rcx, new object[] { RunStage.Driving });
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); car.SetDriveMode(MODE);
string f = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/d198/loop_pts.txt"));
var all = System.IO.File.ReadAllLines(f).Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
TownPathBot.LoopPts = all.Skip(A).Take(B - A).ToList();
TownPathBot.LoopN = N; TownPathBot.Only = ""; TownPathBot.Status = "starting"; TownPathBot.Begin("loopSeg" + MODE, 8f);
return "seg " + A + "-" + B + " " + car.Mode;
