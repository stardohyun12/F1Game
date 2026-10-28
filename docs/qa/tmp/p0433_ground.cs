// 과제 0433: 기획 도구 GroundCheck 를 지금 씬에 돌린다(저장 없이)
if (UnityEditor.EditorApplication.isCompiling) return "compiling";
var w = PitCrewPlan.PlanIO.GroundCheck(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return "warn " + w.Count + "\n" + string.Join("\n", w);
