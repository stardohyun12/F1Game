// 과제 1429: 벽 낮춘 6곳 길 가장자리에 연석 콜라이더(렌더러 없음). 에디트 모드, Scene_A4e_Town 만.
//  집 정면 밖으로 Road 가까운 끝을 찾아(봇과 같은 방법) 그 끝에서 집 쪽 0.15m 에 상자: 높이 0.45m(카트 상자 0.085~0.535 를 막음), 두께 0.3m, 길이 = 집 폭 + 3m.
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var log = "";
var old = root.Find("A4e_Curbs"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var cr = new GameObject("A4e_Curbs").transform; cr.SetParent(root, true);
foreach (Transform t in g) {
  var q = t.rotation; t.rotation = Quaternion.identity; var b = t.GetComponentInChildren<Renderer>().bounds; t.rotation = q; float fz = b.max.z - t.position.z, wx = b.size.x;
  var o = t.forward; o.y = 0; o.Normalize(); var f0 = t.position + o * fz; float near = -1; float ry = 0;
  for (float d = 0; d < 25 && near < 0; d += 0.25f) foreach (var hh in Physics.RaycastAll(f0 + o * d + Vector3.up * 20, Vector3.down, 60)) if (hh.collider.name == "Road") { near = d; ry = hh.point.y; }
  if (near < 0) { log += t.name + " no road; "; continue; }
  var c = new GameObject("Curb_" + t.name); c.transform.SetParent(cr, true); var p = f0 + o * (near - 0.15f); p.y = ry + 0.225f - 0.1f;   // 0.1m 땅에 묻음
  c.transform.SetPositionAndRotation(p, Quaternion.LookRotation(o, Vector3.up)); var bc = c.AddComponent<BoxCollider>(); bc.size = new Vector3(wx + 3f, 0.45f + 0.1f, 0.3f);
  log += c.name + " len " + bc.size.x.ToString("F1") + " roadNear " + near.ToString("F2") + "; "; }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return sc.path + " " + log;
