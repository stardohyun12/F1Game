// (c) 정리: TEST 줄·png 삭제, 모드 끄기
var jl = PitCrewPlan.PlanIO.JsonlPath("Map_TownV6");
var lines = System.IO.File.ReadAllLines(jl).ToList();
int removed = 0;
foreach (var l in lines.Where(l => l.Contains("TEST 그림 합성")).ToList())
{
    var m = System.Text.RegularExpressions.Regex.Match(l, "\"screenshot\":\"([^\"]+)\"");
    if (m.Success) { var f = System.IO.Path.Combine(PitCrewPlan.PlanIO.FeedbackDir, m.Groups[1].Value); if (System.IO.File.Exists(f)) System.IO.File.Delete(f); }
    lines.Remove(l); removed++;
}
System.IO.File.WriteAllText(jl, string.Join("\n", lines) + "\n");
PitCrewPlan.PlanMode.On = false;
int before = lines.Count;
var yamlBefore = System.IO.File.ReadAllText(PitCrewPlan.PlanIO.PlacementsPath);

// (d) depot 을 x +1m 옮기고 씬 저장 → 자동 기록
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var g = UnityEngine.GameObject.Find("TownV6_Gray/D198_Buildings/D198_depot");
var p0 = g.transform.position;
g.transform.position = p0 + new UnityEngine.Vector3(1, 0, 0);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
var yamlMoved = System.IO.File.ReadAllText(PitCrewPlan.PlanIO.PlacementsPath);
var after = System.IO.File.ReadAllLines(jl);
string newLine = after.Length > before ? after[after.Length - 1] : "(없음)";

// 되돌리고 다시 저장 → yaml 원래대로
g.transform.position = p0;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
var yamlBack = System.IO.File.ReadAllText(PitCrewPlan.PlanIO.PlacementsPath);
int added = System.IO.File.ReadAllLines(jl).Length - before;
// 테스트 [배치] 줄 지움
System.IO.File.WriteAllText(jl, string.Join("\n", lines) + "\n");
// 3회째 저장(변경 없음) → 안 씀
var t0 = System.IO.File.GetLastWriteTimeUtc(PitCrewPlan.PlanIO.PlacementsPath);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
bool untouched = t0 == System.IO.File.GetLastWriteTimeUtc(PitCrewPlan.PlanIO.PlacementsPath) && System.IO.File.ReadAllLines(jl).Length == before;
return "removed TEST=" + removed + "\nmoved: yaml changed=" + (yamlMoved != yamlBefore) + " newJsonl=" + newLine.Substring(0, System.Math.Min(240, newLine.Length))
  + "\nback: yaml==orig " + (yamlBack == yamlBefore) + " jsonl lines added=" + added + " (지움)"
  + "\nno-change save: untouched=" + untouched + " dirty=" + sc.isDirty;
