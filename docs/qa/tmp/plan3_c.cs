var sv = UnityEditor.SceneView.lastActiveSceneView;
var w = PitCrewPlan.PlanInputPopup.Open(new UnityEngine.Vector2(sv.position.x + 300, sv.position.y + 200), "x");
var before = w.position;
w.position = new UnityEngine.Rect(sv.position.x + 300, sv.position.y + 200, 320, 96);
return "sv=" + sv.position + " popupBefore=" + before + " after=" + w.position + " main=" + UnityEditor.EditorGUIUtility.GetMainWindowPosition();
