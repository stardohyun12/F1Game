UnityEngine.Application.runInBackground = true;
// 시나리오·찍기 폴더는 SessionState 로 넘긴다 (pb.scenario = bare|bottom|full, pb.dir = 비우면 안 찍음)
PitCrew.PackBench.PackBench.QaShotDir = UnityEditor.SessionState.GetString("pb.dir", "");
PitCrew.PackBench.PackBench.QaFrameEvery = 0.5f;
return PitCrew.PackBench.PackBench.BotRun(UnityEditor.SessionState.GetString("pb.scenario", "bare"), 10f);
