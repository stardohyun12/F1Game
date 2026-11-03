var t = System.Type.GetType("CoreTestRun, PitCrew.Core.Tests");
return (string)t.GetField("Status").GetValue(null);
