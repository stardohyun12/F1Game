var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>();
return "{\"state\":\"" + d.State + "\",\"talked\":" + d.Talked.ToString().ToLower() + ",\"needsKey\":" + d.CartNeedsBossTalk.ToString().ToLower()
     + ",\"lockedTries\":" + d.CartLockedTries + ",\"aside\":" + d.AsideOn.ToString().ToLower() + ",\"talkText\":\"" + d.TalkText + "\",\"prompt\":\"" + d.Prompt
     + "\",\"gaze\":\"" + d.GazeTarget + "\",\"ui\":\"" + (ui != null ? ui.Visible : "?") + "\",\"inTalk\":" + d.InTalk.ToString().ToLower() + "}";
