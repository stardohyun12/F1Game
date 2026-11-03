var l = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
return OnFootRouteBot.Status + " | " + (l != null ? l.StyleState : "-") + " | t=" + Time.time.ToString("F0");
