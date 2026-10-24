// 손해 판: 주행 중 시계를 끝 직전으로 → 배달 0건 시간 초과(보상 = 0). 지갑이 0 에서 멈추는지.
var r=Services.Run as RunController; var w=UpgradeSystem.Instance.Wallet;
var p=typeof(RunController).GetProperty("Elapsed"); p.SetValue(r, r.ParTime-1.5f);
return "stage="+r.CurrentStage+" walletBefore="+w+" elapsed="+r.Elapsed+" par="+r.ParTime;
