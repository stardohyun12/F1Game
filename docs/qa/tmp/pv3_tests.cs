var t = System.Type.GetType("CoreTestRun, PitCrew.Core.Tests");
if (t == null) return "FAIL: CoreTestRun 없음";
return (string)t.GetMethod("Start").Invoke(null, new object[] { "D:/Github/F1Game/docs/qa/evidence/packing-v3/core-tests.json" });
