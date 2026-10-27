// 과제 1429: 연석을 바퀴 SphereCast 가 타고 넘음 → 연석 = 내장 레이어 2(Ignore Raycast), 이 씬 차 groundLayer 에서 2 제외. 연석 위치 높이 보정(길 −0.1 ~ +0.45).
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); var log = "";
int n2 = 0; foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c.gameObject.layer == 2) { n2++; log += "L2:" + c.name + ","; }
foreach (Transform c in GameObject.Find("SceneA4").transform.Find("A4e_Curbs")) { c.gameObject.layer = 2; c.position += Vector3.up * 0.05f; }
var car = GameObject.Find("Race").GetComponent<ArcadeCarController>(); var so = new SerializedObject(car); var gl = so.FindProperty("groundLayer"); gl.intValue = ~(1 << 2); so.ApplyModifiedProperties();
log += " prevL2 " + n2 + " groundLayer " + gl.intValue + " collide(car,2) " + !Physics.GetIgnoreLayerCollision(car.gameObject.layer, 2);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return sc.path + log;
