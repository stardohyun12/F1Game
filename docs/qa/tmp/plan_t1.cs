UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var w = PitCrewPlan.PitCrewPlanWindow.Open();
var g = UnityEngine.GameObject.Find("TownV6_Gray/D198_Buildings/D198_garage");
UnityEditor.Selection.activeGameObject = g;
var sv = UnityEditor.SceneView.lastActiveSceneView;
if (sv != null) sv.Frame(new UnityEngine.Bounds(g.transform.position + UnityEngine.Vector3.up * 3, UnityEngine.Vector3.one * 30), true);
var id = w.Send("테스트 코멘트 (지울 것)", false);
return id + " sv=" + (sv != null) + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
