UnityEngine.Application.runInBackground = true;
string sc = UnityEditor.SessionState.GetString("pb.scenario", "bare");
float sec = UnityEditor.SessionState.GetFloat("pb.sec", 10f);
PitCrew.PackBench.PackBench.QaShotDir = UnityEditor.SessionState.GetString("pb.dir", "");
PitCrew.PackBench.PackBench.QaFrameEvery = 0.5f;
return PitCrew.PackBench.PackBench.BotRun(sc, sec);
