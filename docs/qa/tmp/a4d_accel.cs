// 과제 1331: 이 씬 차 accelTime 2.4 (가설 — Wheels 굴림 끌림 3.2m/s²·Box 미끄럼 마찰 0.15g 만큼 실제 가속이 덜 나와 오르막이 22~27km/h 에 머묾)
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4d_Town.unity");
var so = new SerializedObject(GameObject.Find("Race").GetComponent<ArcadeCarController>()); so.FindProperty("accelTime").floatValue = 2.4f; so.ApplyModifiedProperties();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "accelTime " + so.FindProperty("accelTime").floatValue + " max " + so.FindProperty("maxSpeedKmh").floatValue;
