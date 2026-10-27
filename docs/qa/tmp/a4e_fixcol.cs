// 과제 1429: 몸체 상자 가로세로 중심 보정 — 정점 기준이 앞(−Y) 소품에 끌려감. kit.json: 원점 = 바닥 가운데 → 일반 집 (0,0).
//  비탈 집: 원점 = 축대 가운데, 앞마당·계단이 앞(−Y) → 몸체는 축대 뒤 끝에 붙인다(y = 축대 max.y − D/2).
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); var g = GameObject.Find("SceneA4").transform.Find("A4b_Houses"); var log = "";
foreach (Transform t in g) { var mf = t.GetComponentInChildren<MeshFilter>(); var bcs = mf.GetComponents<BoxCollider>(); var b = bcs[0]; var mb = mf.sharedMesh.bounds;
  float y = t.name == "house_slope" ? mb.max.y - b.size.y * 0.5f : 0f; b.center = new Vector3(0f, y, b.center.z);
  log += t.name + " " + b.center.ToString("F2") + " mesh y " + mb.min.y.ToString("F1") + "~" + mb.max.y.ToString("F1") + " x " + mb.min.x.ToString("F1") + "~" + mb.max.x.ToString("F1") + "; "; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return sc.path + " " + log;
