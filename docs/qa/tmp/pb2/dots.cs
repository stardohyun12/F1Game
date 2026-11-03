var b = PitCrew.PackBench.PackBench.I;
var f = typeof(PitCrew.PackBench.PackBench).GetField("dots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var a = f.GetValue(b) as UnityEngine.Transform[];
int nn = 0; if (a != null) foreach (var t in a) if (t == null) nn++;
return "dots=" + (a == null ? "null" : a.Length.ToString()) + " nullEntries=" + nn + " | " + PitCrew.PackBench.PackBench.Qa2Status + " log=" + PitCrew.PackBench.PackBench.Qa2Log.Count;