PitCrewPlan.PlanMode.On = true;
var sv = UnityEditor.SceneView.lastActiveSceneView;
var p = sv.position;
var w = PitCrewPlan.PlanInputPopup.Open(new UnityEngine.Vector2(p.x + p.width * 0.5f, p.y + p.height * 0.4f), "D198_depot");
return "popup=" + (w != null) + " pos=" + w.position;
