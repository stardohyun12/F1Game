UnityEngine.Application.runInBackground = true;
PitCrew.PackBench.PackBench.Qa2Dir = "D:/Github/F1Game/docs/qa/evidence/packbench-v2";
System.IO.Directory.CreateDirectory(PitCrew.PackBench.PackBench.Qa2Dir);
PitCrew.PackBench.PackBench.QaShotDir = "";
PitCrew.PackBench.PackBench.QaFrameEvery = 0f; // 흔들기 프레임은 안 찍음(1판에 있음)
PitCrew.PackBench.PackBench.Qa2Log.Clear();
return PitCrew.PackBench.PackBench.Qa2Run(UnityEditor.SessionState.GetString("pb2.what", "all"));