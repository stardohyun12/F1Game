// HP 는 /d/Github/F1Game/docs/qa/tmp/handover/hp.txt 에서 읽는다(검사마다 바꿈).
Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null) return "no dir";
float hp = float.Parse(System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/handover/hp.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
return "hp=" + hp + " " + d.DebugArriveForHandover(hp, new Vector3(97.5f, 4.4f, -281.0f), 270f, new Vector3(94.0f, 4.0f, -283.65f), 180f);
