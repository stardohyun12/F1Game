var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Proto_CoastCourse.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var s = "roots: " + string.Join(", ", sc.GetRootGameObjects().Select(g => g.name)) + "\n";
foreach (var n in new[] { "DeliveryManager", "DeliveryPoint", "BayLoopDriveOnly", "RunController", "GameContext", "CargoBox", "CargoShakeSimulator", "FollowCamera", "ArcadeCarController", "DeliveryHUD", "GameFlow" })
{
    var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).FirstOrDefault(x => x != null);
    if (t == null) { s += n + ":notype "; continue; }
    var objs = UnityEngine.Object.FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None);
    s += n + ":" + objs.Length + (objs.Length > 0 ? "(" + ((Component)objs[0]).gameObject.name + ")" : "") + " ";
}
return s;
