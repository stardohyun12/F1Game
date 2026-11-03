Application.runInBackground = true;
if (!UnityEditor.EditorApplication.isPlaying) return "NOT PLAYING";
OnFootRouteBot.GrabDir = "D:/Github/F1Game/docs/qa/evidence/ui-v1b";
return OnFootRouteBot.BeginFirstOrder("D:/Github/F1Game/docs/qa/evidence/ui-v1b/bot", "D:/Github/F1Game/docs/qa/tmp/uiv1b");
