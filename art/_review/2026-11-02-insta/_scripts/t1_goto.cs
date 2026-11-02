var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
Application.runInBackground = true;
var r1 = DebugWindow.GoPlace(5);
return r1 + " | state=" + d.State + " feet=" + d.Player.transform.position + " prompt=" + d.Prompt;
