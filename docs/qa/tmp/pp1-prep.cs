Application.runInBackground = true;
UnityEditor.SessionState.SetString("pp.botId", "");
var rc = Services.Run as RunController;
typeof(RunController).GetMethod("EnterFreeDrive").Invoke(rc, null);
UnityEditor.SessionState.SetString("pp.queue", "D1_main,D1_short,D2_main,D2_short,D3_main,D3_short,D4_main,D4_short");
return "stage=" + rc.CurrentStage;
