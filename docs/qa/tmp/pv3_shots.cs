var t = System.Type.GetType("PackV3Shots, PitCrew.Debug");
if (t == null) return "FAIL: type";
var s = (string)t.GetField("Status").GetValue(null);
if (s == "run") return "run";
return (string)t.GetMethod("Begin").Invoke(null, new object[] { "D:/Github/F1Game/docs/qa/evidence/packing-v3" });
