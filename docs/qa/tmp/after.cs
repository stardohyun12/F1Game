var r=Services.Run as RunController; var n=UnityEditor.SessionState.GetString("qacapn","x");
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-1536-result-"+n+".png");
return "stage="+r.CurrentStage+" success="+r.LastRunSuccess+" reward="+r.LastReward+" pack="+r.LastPackCost+" net="+r.LastNet+" wallet="+UpgradeSystem.Instance.Wallet+" done="+r.CompletedCount;
