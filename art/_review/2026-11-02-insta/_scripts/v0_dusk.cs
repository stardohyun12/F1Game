var r = DebugWindow.SetDusk(1f);
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return r + " state=" + d.State + " landed=" + d.DebugSpot.Landed + " gulls=" + UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.ToLower().Contains("gull") || t.name.ToLower().Contains("갈매기"));
