// 과제 0248 조사: Map_TownV6 상태 + Map_BayLoop 튜토리얼 묶음 구조
var sb = new System.Text.StringBuilder();
var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
sb.Append("car " + (car ? car.transform.position.ToString("F1") + " parent " + (car.transform.parent ? car.transform.parent.name : "-") : "none"));
sb.Append(" | OT " + (UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>(FindObjectsInactive.Include) != null));
sb.Append(" | rc " + (UnityEngine.Object.FindFirstObjectByType<RunController>(FindObjectsInactive.Include) != null));
sb.Append(" | mgr " + (UnityEngine.Object.FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include) != null));
sb.Append(" | roots: "); foreach (var g in s.GetRootGameObjects()) sb.Append(g.name + ",");
sb.Append(" | DP: "); foreach (var d in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append(d.name + "@" + d.transform.position.ToString("F0") + "<" + (d.transform.parent ? d.transform.parent.name : "") + ">,");
var b = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_BayLoop.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
OpeningTutorial ot = null; foreach (var g in b.GetRootGameObjects()) { ot = g.GetComponentInChildren<OpeningTutorial>(true); if (ot) break; }
if (ot) { var p = ot.transform.parent; sb.Append(" || BayOT parent " + p.name + " root " + ot.transform.root.name + " kids: "); foreach (Transform c in p) sb.Append(c.name + ","); }
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(b, true);
return sb.ToString();
