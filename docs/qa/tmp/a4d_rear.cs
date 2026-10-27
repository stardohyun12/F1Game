// 과제 1331: kart_a2 뒷바퀴(반지름 0.245)가 땅에서 0.08m 떠 있음(모델 축 높이) → 뒷바퀴 피벗 2개를 0.08m 내림(외형만)
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4d_Town.unity");
var kv = GameObject.Find("Race").transform.Find("A4_KartVisual"); var s = "";
foreach (var n in new[] { "Wheel_RL", "Wheel_RR" }) { var w = kv.Find(n); w.position -= w.up * 0.08f; s += n + " " + w.localPosition.ToString("F3") + "; "; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return s;
