System.IO.File.Delete("D:/Github/F1Game/docs/qa/tmp/night.flag");
var n = 0;
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
  if (l.type == LightType.Directional) { l.enabled = true; RenderSettings.sun = l; n++; }
DebugWindow.SetDusk(0f);
var dlc = UnityEngine.Object.FindFirstObjectByType<DayLoopCheck>();
return "restored " + n + " dlc " + (dlc ? "present" : "none");
