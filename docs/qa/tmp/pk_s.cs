var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var spot = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>();
var h = spot.Hands;
var cur = UnityEngine.Object.FindFirstObjectByType<PitCrew.PackBench.PackBenchCursor>();
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
return "{\"state\":\"" + d.State + "\",\"handsOn\":" + h.enabled.ToString().ToLower() + ",\"benchOn\":" + spot.Bench.enabled.ToString().ToLower()
     + ",\"onBench\":" + h.OnBench().Count + ",\"hands\":" + h.StateJson() + ",\"cursor\":\"" + (cur != null ? cur.Visible : "?") + "\",\"ui\":\"" + (ui != null ? ui.Visible : "?")
     + "\",\"clerk\":\"" + (d.AsideOn ? d.TalkText : "") + "\",\"ready\":" + spot.Ready.ToString().ToLower() + "}";
