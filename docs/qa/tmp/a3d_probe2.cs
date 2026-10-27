// 과제 1145 탐색 — Scene_A3_Town 구성(카트 자리·땅 이름·경계)
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A3_Town.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var sb = new System.Text.StringBuilder();
foreach (var g in sc.GetRootGameObjects()) { sb.Append("ROOT " + g.name + (g.activeSelf ? "" : "(off)") + " ["); foreach (var c in g.GetComponents<Component>()) sb.Append(c.GetType().Name + ","); sb.Append("]\n"); }
var root = GameObject.Find("SceneA3").transform;
foreach (Transform k in root) { var b = new Bounds(); bool f = false; int n = 0; var names = new System.Collections.Generic.HashSet<string>();
  foreach (var r in k.GetComponentsInChildren<MeshRenderer>(true)) { if (!f) { b = r.bounds; f = true; } else b.Encapsulate(r.bounds); n++; if (names.Count < 12) names.Add(r.name); }
  sb.Append(k.name + (k.gameObject.activeSelf ? "" : "(off)") + " n" + n + " c" + b.center.ToString("F0") + " s" + b.size.ToString("F0") + " : " + string.Join(",", names) + "\n"); }
foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { var nm = r.name.ToLower(); if (nm.Contains("kart") || nm.Contains("cart") || nm.Contains("car")) sb.Append("CARLIKE " + r.name + " parent " + r.transform.parent.name + " pos " + r.bounds.center.ToString("F2") + " size " + r.bounds.size.ToString("F2") + " rotY " + r.transform.eulerAngles.y.ToString("F0") + "\n"); }
foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("CAM " + cam.name + " on " + cam.gameObject.activeSelf + " pos " + cam.transform.position.ToString("F1") + " fwd " + cam.transform.forward.ToString("F2") + " fov " + cam.fieldOfView + " clr " + cam.clearFlags + "\n");
sb.Append("colliders " + UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length + "\n");
var pr = AssetDatabase.LoadAssetAtPath<CameraFeelPreset>("Assets/Settings/Camera/CameraFeel_B_HighRoad.asset"); sb.Append("preset B fov " + pr.baseFov + " h " + pr.height + " d " + pr.distance + "\n");
return sb.ToString();
