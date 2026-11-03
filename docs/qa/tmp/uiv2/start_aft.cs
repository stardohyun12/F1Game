Application.runInBackground = true;
if (!UnityEditor.EditorApplication.isPlaying) return "NOT PLAYING";
var sl = SunsetLook.Find(); if (sl != null) { sl.dusk = 0f; if (!sl.IsOn) sl.SetOn(true); else sl.Apply(); }
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>(); ui.dirMode = DotScreenUI.DirMode.Compass;
OnFootRouteBot.GrabDir = "D:/Github/F1Game/docs/qa/evidence/ui-v2/aft";
return OnFootRouteBot.BeginFirstOrder("D:/Github/F1Game/docs/qa/evidence/ui-v2/aft/bot", "D:/Github/F1Game/docs/qa/tmp/uiv2/aft") + " dusk=" + (sl != null ? sl.dusk : -1);
