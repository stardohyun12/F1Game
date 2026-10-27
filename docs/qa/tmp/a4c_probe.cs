// 과제 1317 탐색: 집마다 바닥 y vs 집 앞 길 벽 꼭대기 y, Cam_B_Game LowRes 높이
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4b_Town.unity");
var g = GameObject.Find("SceneA4").transform.Find("A4b_Houses"); var sb = new System.Text.StringBuilder();
foreach (Transform t in g) {
  float top = -999; string wn = "";
  for (float d = 0; d < 8; d += 0.25f) for (float s = -2; s <= 2; s += 1) { var p = t.position + t.forward * d + t.right * s + Vector3.up * 40;
    foreach (var h in Physics.RaycastAll(p, Vector3.down, 100)) if (h.collider.name.StartsWith("Wall_") && h.point.y > top) { top = h.point.y; wn = h.collider.name + " d" + d; } }
  sb.Append(t.name + " floor " + t.position.y.ToString("F2") + " wallTop " + top.ToString("F2") + " (" + wn + ") diff " + (top - t.position.y).ToString("F2") + "\n"); }
foreach (var c in UnityEngine.Object.FindObjectsByType<LowResRenderTest>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append(c.name + " h " + c.targetHeight + "; ");
sb.Append(" PCLook " + (UnityEngine.Object.FindFirstObjectByType<PCLookScene>(FindObjectsInactive.Include) != null));
return sb.ToString();
