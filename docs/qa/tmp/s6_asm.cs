var names = new[]{"OnFootDirector","Stage_Garage","DayView","DotScreenUI","Gaze","DepotPackSpot","PitCrew.PackBench.PackBench","PitCrew.PackBench.PackBenchCursor","LowResRenderTest","IScreenFonts","OpeningSequence","RepairSpot","DayLoopCheck","DayState","OrderRules"};
var sb = new System.Text.StringBuilder();
foreach (var n in names) {
  System.Type t = null;
  foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { t = a.GetType(n); if (t != null) break; }
  sb.Append(n).Append("=").Append(t == null ? "MISSING" : t.Assembly.GetName().Name).Append("\n");
}
var d = System.Type.GetType("OnFootDirector, PitCrew.Stages");
var bf = System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
foreach (var m in new[]{"EnterPack","TakePacked","EnterCart"}) sb.Append(m).Append(":").Append(d != null && d.GetMethod(m, bf) != null).Append("\n");
sb.Append("ParkCar:").Append(d != null && d.GetMethod("ParkCar", bf, null, new[]{typeof(Vector3), typeof(float)}, null) != null);
return sb.ToString();
