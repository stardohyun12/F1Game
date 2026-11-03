PitCrewPlan.PlanMode.On = false;   // 팝업도 닫힘
var jl = PitCrewPlan.PlanIO.JsonlPath("Map_TownV6");
var lines = System.IO.File.ReadAllLines(jl).ToList();
int removed = 0;
foreach (var l in lines.Where(l => l.Contains("한글 테스트 가나다")).ToList())
{
    var m = System.Text.RegularExpressions.Regex.Match(l, "\"screenshot\":\"([^\"]+)\"");
    if (m.Success) { var f = System.IO.Path.Combine(PitCrewPlan.PlanIO.FeedbackDir, m.Groups[1].Value); if (System.IO.File.Exists(f)) System.IO.File.Delete(f); }
    lines.Remove(l); removed++;
}
System.IO.File.WriteAllText(jl, string.Join("\n", lines) + "\n");
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return "removed=" + removed + " popup=" + (PitCrewPlan.PlanInputPopup.Current != null) + " dirty=" + sc.isDirty + " play=" + UnityEditor.EditorApplication.isPlaying;
