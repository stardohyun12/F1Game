var sv = UnityEditor.SceneView.lastActiveSceneView;
if (sv == null) return "no sceneview";
var g = UnityEngine.GameObject.Find("TownV6_Gray/D198_Buildings/D198_depot");
UnityEditor.Selection.activeGameObject = null;
PitCrewPlan.PlanIO.TryBounds(g, out var b);
sv.LookAt(b.center, UnityEngine.Quaternion.Euler(30, 200, 0), 30f, false, true);
sv.Focus(); sv.Repaint();
// 오버레이 표시 확인
string ov = "";
if (sv.TryGetOverlay("pitcrew-plan", out var o)) { o.displayed = true; o.collapsed = false; ov = "overlay displayed=" + o.displayed; } else ov = "overlay NOT found";
return ov + " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
