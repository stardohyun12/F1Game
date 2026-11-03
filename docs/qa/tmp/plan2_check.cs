string found = "";
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
{
    var t = a.GetType("PitCrewPlan.PlanMode"); if (t != null) found += a.GetName().Name + ";";
}
var w = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PitCrewPlan.PitCrewPlanWindow")).FirstOrDefault(x => x != null);
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " failed=" + UnityEditor.EditorUtility.scriptCompilationFailed + " PlanMode in=" + found + " window=" + (w != null);
