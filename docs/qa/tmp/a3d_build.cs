// 과제 1145 — Scene_A3_Town 을 운전 가능하게: BayLoop 차(Race) 복사 + 추격 카메라(B 높이 2.8·거리 5.6·FOV 64, 720 Nearest) + MeshCollider + 보이지 않는 벽 + 토글 컴포넌트. 값 = 가설.
var S = "Assets/Scenes/Scene_A3_Town.unity"; var log = new System.Text.StringBuilder();
var a3 = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S, UnityEditor.SceneManagement.OpenSceneMode.Single);
var root = GameObject.Find("SceneA3");
// 지난 실행 잔여 지우기(다시 돌려도 같게)
foreach (var n in new[] { "A3_Drive", "Race", "GameCam" }) { var o = GameObject.Find(n); if (o) UnityEngine.Object.DestroyImmediate(o); }
foreach (var c in root.GetComponentsInChildren<MeshCollider>(true)) UnityEngine.Object.DestroyImmediate(c);
// ---- 차 복사
var bl = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_BayLoop.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
GameObject src = null; foreach (var g in bl.GetRootGameObjects()) if (g.name == "Race") src = g;
var car = UnityEngine.Object.Instantiate(src); car.name = "Race";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(car, a3);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(bl, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(a3);
var acc = car.GetComponent<ArcadeCarController>();
var so = new SerializedObject(acc); var fd = so.FindProperty("freeDrive"); if (fd != null) { fd.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); } log.Append("freeDrive " + (fd != null) + "; drive " + (so.FindProperty("driveMode")?.enumDisplayNames[so.FindProperty("driveMode").enumValueIndex]) + "; ");
// 시작 = 카트 자리, 길(-Z) 쪽
var kart = root.transform.Find("Kart"); var kb = new Bounds(); bool f0 = false; foreach (var r in kart.GetComponentsInChildren<Renderer>(true)) { if (!f0) { kb = r.bounds; f0 = true; } else kb.Encapsulate(r.bounds); }
kart.gameObject.SetActive(false);
// ---- 콜라이더: 길·땅·둔덕·담
int nc = 0; var colNames = new System.Collections.Generic.HashSet<string> { "Terrain", "yard_ground", "Headland", "Road", "Wall_L", "Wall_R", "DryVerge", "a3_M1_mound", "apron" };   // 풀 덩어리(a3_E*_clump)는 길 위에 턱을 만들어 뺐다(1차 시험에서 z -18.5 정지)
foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (colNames.Contains(mf.name) && mf.sharedMesh) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; nc++; }
// 바닥 높이: 카트 자리에서 아래로
Physics.SyncTransforms(); float gy = kb.min.y; RaycastHit hit;
if (Physics.Raycast(new Vector3(kb.center.x, 20, kb.center.z), Vector3.down, out hit, 60)) gy = hit.point.y;
car.transform.SetPositionAndRotation(new Vector3(kb.center.x, gy + 0.6f, kb.center.z), Quaternion.Euler(0, 180, 0));
var rb = car.GetComponent<Rigidbody>(); rb.linearVelocity = Vector3.zero;
foreach (var h in Physics.RaycastAll(new Vector3(kb.center.x, 20, kb.center.z), Vector3.down, 60)) log.Append("hit " + h.collider.name + " y" + h.point.y.ToString("F2") + "; ");
log.Append("kart " +kb.center.ToString("F2") + " ground " + gy.ToString("F2") + " (hit " + (hit.collider ? hit.collider.name : "none") + "); ");
// ---- 보이지 않는 벽: 마을 길 둘레 상자 (x -25..45, z -140..8, 높이 20)
var drive = new GameObject("A3_Drive"); drive.transform.SetParent(root.transform, false);
System.Action<string, Vector3, Vector3> Wall = (n, c, s) => { var w = new GameObject(n); w.transform.SetParent(drive.transform, false); w.transform.position = c; var bc = w.AddComponent<BoxCollider>(); bc.size = s; };
Wall("Wall_N", new Vector3(10, 0, 8.5f), new Vector3(72, 40, 1)); Wall("Wall_S", new Vector3(10, 0, -140.5f), new Vector3(72, 40, 1));
Wall("Wall_W", new Vector3(-25.5f, 0, -66), new Vector3(1, 40, 150)); Wall("Wall_E", new Vector3(45.5f, 0, -66), new Vector3(1, 40, 150));
// ---- 카메라: Cam_B_Game 복제 → GameCam(MainCamera, FollowCamera + 프리셋 B 의 FOV 64 판)
GameObject camB = null; foreach (var g in a3.GetRootGameObjects()) if (g.name == "Cam_B_Game") camB = g; camB.SetActive(true); var gc = UnityEngine.Object.Instantiate(camB); gc.name = "GameCam"; gc.tag = "MainCamera"; camB.SetActive(false);
gc.AddComponent<AudioListener>();
var pB = AssetDatabase.LoadAssetAtPath<CameraFeelPreset>("Assets/Settings/Camera/CameraFeel_B_HighRoad.asset");
var pP = "Assets/Art/SceneA3/CameraFeel_A3_B64.asset"; AssetDatabase.DeleteAsset(pP); var pr = UnityEngine.Object.Instantiate(pB); pr.baseFov = 64; AssetDatabase.CreateAsset(pr, pP);
var fc = gc.AddComponent<FollowCamera>(); var fso = new SerializedObject(fc);
fso.FindProperty("target").objectReferenceValue = car.transform; fso.FindProperty("car").objectReferenceValue = acc; fso.FindProperty("preset").objectReferenceValue = pr; fso.ApplyModifiedPropertiesWithoutUndo();
gc.GetComponent<Camera>().fieldOfView = 64;
gc.transform.position = car.transform.position + car.transform.rotation * new Vector3(0, 2.8f, -5.6f); gc.transform.LookAt(car.transform.position + Vector3.up * pr.lookHeight);
var lr = gc.GetComponent<LowResRenderTest>(); log.Append("lowres " + lr.targetHeight + " int " + lr.integerScale + " smooth " + lr.smooth + "; ");
// ---- 토글
var tg = root.GetComponent<SceneA3Toggles>(); if (!tg) tg = root.AddComponent<SceneA3Toggles>();
var ms = new System.Collections.Generic.List<Material>(); foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art/SceneA3/Mats" })) ms.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));
tg.materials = ms.ToArray();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(a3); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(a3); AssetDatabase.SaveAssets();
return "colliders " + nc + " · mats " + ms.Count + " · car " + car.transform.position.ToString("F2") + "\n" + log;
