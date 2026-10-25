var r=Services.Run as RunController;
return UnityEditor.SessionState.GetString("proto.drive","")+" || "+UnityEditor.SessionState.GetString("p.hp","")+" || stage="+r.CurrentStage+" elapsed="+r.Elapsed+" limit="+r.ParTime+" success="+r.LastRunSuccess+" reward="+r.LastReward+" net="+r.LastNet+" wallet="+UpgradeSystem.Instance.Wallet;
