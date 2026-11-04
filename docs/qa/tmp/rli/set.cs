// 시간대 놓기: set.txt = "<dusk 0..1> <night 0|1>" — 밤 = 해(방향광) 끔. 카메라 yaw 41 고정(같은 카메라)
var a = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/rli/set.txt").Trim().Split(' ');
Application.runInBackground = true;
var f = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
f.Yaw = 41f;
DebugWindow.SetDusk(float.Parse(a[0]));
bool night = a[1] == "1";
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    if (l.type == LightType.Directional) { l.enabled = !night; if (!night) RenderSettings.sun = l; }
var s = RenderSettings.sun;
return "set dusk " + a[0] + " night " + night + " sun " + (s ? s.enabled + " " + s.intensity.ToString("F2") + " " + s.color + " rot " + s.transform.eulerAngles.ToString("F0") : "null");
