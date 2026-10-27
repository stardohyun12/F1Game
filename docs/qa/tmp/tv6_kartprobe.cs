// 과제 0422: Scene_A6d_Town 의 Race(카트 v6e) 구성 읽기 — 추가로 열고 닫음(저장 안 함)
var sb = new System.Text.StringBuilder();
var b = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6d_Town.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
Transform race = null; foreach (var g in b.GetRootGameObjects()) { var c = g.GetComponentInChildren<ArcadeCarController>(true); if (c) { race = c.transform; break; } }
sb.Append("race " + race.name + " scale " + race.lossyScale.ToString("F3") + "\n");
foreach (Transform t in race) sb.Append(" " + t.name + " active=" + t.gameObject.activeSelf + " lp=" + t.localPosition.ToString("F2") + " lr=" + t.localEulerAngles.ToString("F0") + " ls=" + t.localScale.ToString("F2") + (t.GetComponent<Renderer>() ? " R" + t.GetComponent<Renderer>().enabled : "") + "\n");
foreach (var c in race.GetComponents<Collider>()) { var bc = c as BoxCollider; sb.Append(" col " + c.GetType().Name + (bc ? " c=" + bc.center.ToString("F2") + " s=" + bc.size.ToString("F2") : "") + "\n"); }
foreach (var r in race.GetComponentsInChildren<Renderer>(false)) if (r.enabled && !r.transform.IsChildOf(race.Find("A6_KartVisual") ?? race)) sb.Append(" visibleOther " + r.name + "\n");
var kw = race.GetComponentInChildren<KartWheelVisual>(true); sb.Append("kw on " + (kw ? kw.name : "none") + "\n");
var mine = UnityEngine.Object.FindObjectsByType<ArcadeCarController>(FindObjectsSortMode.None).First(c => c.gameObject.scene != b).transform;
sb.Append("TV6 race scale " + mine.lossyScale.ToString("F3") + "\n");
foreach (Transform t in mine) sb.Append(" " + t.name + " active=" + t.gameObject.activeSelf + " lp=" + t.localPosition.ToString("F2") + "\n");
foreach (var c in mine.GetComponents<Collider>()) { var bc = c as BoxCollider; sb.Append(" col " + (bc ? " c=" + bc.center.ToString("F2") + " s=" + bc.size.ToString("F2") : c.GetType().Name) + "\n"); }
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(b, true);
return sb.ToString();
