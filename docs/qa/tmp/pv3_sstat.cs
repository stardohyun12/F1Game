var t = System.Type.GetType("PackV3Shots, PitCrew.Debug");
var d = System.Type.GetType("DayLoopCheck, PitCrew.Debug");
return "shots=" + t.GetField("Status").GetValue(null) + " day=" + d.GetField("Status").GetValue(null) + " playing=" + UnityEditor.EditorApplication.isPlaying;
