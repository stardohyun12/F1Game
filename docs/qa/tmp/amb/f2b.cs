Application.runInBackground = true;
Time.captureFramerate = 0;
AmbienceProbe.StartLog("D:/Github/F1Game/docs/qa/evidence/ambience-zones/walk_f2_fine.tsv", "D:/Github/F1Game/docs/qa/evidence/ambience-zones", 0.1f);
return OnFootRouteBot.BeginFirstOrder("D:/Github/F1Game/docs/qa/evidence/ambience-zones/f2b", "D:/Github/F1Game/docs/qa/evidence/ambience-zones/f2b");
