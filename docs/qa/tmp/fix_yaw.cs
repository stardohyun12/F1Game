var f = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
f.Yaw = 41f;
DebugWindow.SetDusk(float.Parse(System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/dusk_val.txt").Trim()));
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
  if (l.type == LightType.Directional) l.enabled = !System.IO.File.Exists("D:/Github/F1Game/docs/qa/tmp/night.flag");
return "yaw set";
