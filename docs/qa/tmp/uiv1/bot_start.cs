Application.runInBackground = true;
if (!UnityEditor.EditorApplication.isPlaying) return "NOT PLAYING";
OnFootRouteBot.GrabDir = "D:/Github/F1Game/docs/qa/evidence/ui-v1";
return OnFootRouteBot.BeginFirstOrder("D:/Github/F1Game/docs/qa/evidence/ui-v1/bot", "D:/Github/F1Game/docs/qa/tmp/uiv1");
