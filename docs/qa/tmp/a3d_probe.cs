// 과제 1145 탐색 — Map_BayLoop 루트·차·카메라 구성
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_BayLoop.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
var sb = new System.Text.StringBuilder();
foreach (var g in sc.GetRootGameObjects()) {
  sb.Append(g.name + (g.activeSelf ? "" : "(off)") + " [");
  foreach (var c in g.GetComponents<Component>()) sb.Append(c.GetType().Name + ",");
  sb.Append("] kids " + g.transform.childCount + "\n");
}
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
if (car) { sb.Append("CAR path: "); var t = car.transform; string p = t.name; while (t.parent) { t = t.parent; p = t.name + "/" + p; } sb.Append(p + " scene " + car.gameObject.scene.name + "\n");
  foreach (var c in car.GetComponents<Component>()) sb.Append(" " + c.GetType().Name); sb.Append("\n");
  foreach (Transform k in car.transform) sb.Append("  kid " + k.name + "\n"); }
foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  if (cam.gameObject.scene != sc) continue; sb.Append("CAM " + cam.name + " fov " + cam.fieldOfView + " :");
  foreach (var c in cam.GetComponents<Component>()) sb.Append(" " + c.GetType().Name); sb.Append("\n"); }
foreach (var pl in UnityEngine.Object.FindObjectsByType<PCLookScene>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("PCLook on " + pl.name + " scene " + pl.gameObject.scene.name + "\n");
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(sc, true);
return sb.ToString();
