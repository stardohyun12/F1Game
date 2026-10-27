// 과제 0248: Map_TownV6 에 튜토리얼 고리 — BayLoop 의 Tutorial 묶음을 복사해 새 지점으로 옮기고, Q1~Q5 회색 자리 상자. 다시 돌려도 같은 결과(TV6_Loop 지우고 새로).
var sb = new System.Text.StringBuilder();
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_TownV6.unity");
foreach (var g in sc.GetRootGameObjects()) if (g.name == "TV6_Loop") UnityEngine.Object.DestroyImmediate(g);
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
System.Func<string, List<Vector3>> Load = f => System.IO.File.ReadAllLines(dir + f).Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
var loop = Load("tv6_loop_pts.txt"); var go = Load("tv6_go_pts.txt");
System.Func<Vector3, Vector3> G = p => { foreach (var h in Physics.RaycastAll(new Vector3(p.x, 300f, p.z), Vector3.down, 600f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance)) if (h.collider.GetComponentInParent<RoadSurface>() || h.collider is TerrainCollider || h.collider is MeshCollider) return h.point; return p; };
System.Func<List<Vector3>, float, float, int> Near = (L, x, z) => { int bi = 0; float bd = 1e9f; for (int i = 0; i < L.Count; i++) { float d = (L[i].x - x) * (L[i].x - x) + (L[i].z - z) * (L[i].z - z); if (d < bd) { bd = d; bi = i; } } return bi; };
System.Func<List<Vector3>, int, Vector3> Fw = (L, i) => { var f = L[Mathf.Min(L.Count - 1, i + 3)] - L[Mathf.Max(0, i - 3)]; f.y = 0; return f.normalized; };
var root = new GameObject("TV6_Loop"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, sc);
// 1) BayLoop 튜토리얼 묶음 복사
var b = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_BayLoop.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
OpeningTutorial ot0 = null; foreach (var g in b.GetRootGameObjects()) { ot0 = g.GetComponentInChildren<OpeningTutorial>(true); if (ot0) break; }
var tut = UnityEngine.Object.Instantiate(ot0.transform.parent.gameObject); tut.name = "Tutorial";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tut, sc);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(b, true);
tut.transform.SetParent(root.transform, true);
System.Func<string, Transform> K = n => tut.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
System.Action<string[], Vector3> Move = (names, off) => { foreach (var n in names) { var t = K(n); if (t) t.position += off; else sb.Append("missing " + n + "; "); } };
// 정비소 묶음: 반납점 = go 첫 점(정비소 앞 길)
var gStart = G(go[0]);
var gOff = gStart - K("TUT_ReturnGarage").position;
Move(new[] { "TUT_Garage_PitCrew", "TUT_Room", "TUT_CarStart", "Shot_4a", "TUT_ReturnGarage" }, gOff);
var cs = K("TUT_CarStart"); cs.position = G(go[2]) + Vector3.up * 0.3f; cs.rotation = Quaternion.LookRotation(Fw(go, 2));
K("TUT_ReturnGarage").position = G(loop[loop.Count - 4]);
// MART 묶음: 포장 트리거 = go 끝(MART 앞 길)
var mOff = G(go[go.Count - 1]) - K("TUT_PackTrigger").position;
Move(new[] { "TUT_PackingSpot", "TUT_MartOwner", "Shot_3a", "Shot_3b", "TUT_PackTrigger" }, mOff);
// 찻집: 배달점 = 고리에서 언덕 밭(181.4, 5.7)에 가장 가까운 점
int qi4 = Near(loop, 181.4f, 5.7f); var teaP = G(loop[qi4]);
var tOff = teaP - K("TUT_TeaDelivery").position;
Move(new[] { "TUT_TeaHouse", "TUT_TeaHouse_NPC" }, tOff);
// 해안 기록 구간: Q5(100,-180) → c_west(55,-203)
int c0 = Near(loop, 100f, -180f), c1 = Near(loop, 55f, -203f);
K("TUT_CoastGateStart").position = G(loop[c0]); K("TUT_CoastGateEnd").position = G(loop[c1]);
var sg = K("TUT_CoastStraightEnd_Sign"); if (sg) sg.position = G(loop[c1]) + Vector3.Cross(Vector3.up, Fw(loop, c1)) * 5f;
// 2) Q 자리 상자: 길 옆 7m, 콜라이더 없음
var box = new GameObject("AssetSlots"); box.transform.SetParent(root.transform);
var Q = new (string n, float x, float z)[] { ("Q1_MART", 225.1f, -78.5f), ("Q2_Plaza", 118f, -88f), ("Q3_Shops", 95f, -30f), ("Q4_Teahouse", 181.4f, 5.7f), ("Q5_CoastRail", 100f, -180f) };
foreach (var q in Q)
{
    int i = Near(loop, q.x, q.z); var side = Vector3.Cross(Vector3.up, Fw(loop, i));
    var p = G(loop[i] + side * 7f);
    var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.name = q.n; UnityEngine.Object.DestroyImmediate(c.GetComponent<Collider>());
    c.transform.SetParent(box.transform); c.transform.position = p + Vector3.up * 1.5f; c.transform.localScale = new Vector3(4f, 3f, 4f); c.transform.rotation = Quaternion.LookRotation(Fw(loop, i));
    sb.Append(q.n + " " + c.transform.position.ToString("F1") + " road" + G(loop[i]).ToString("F1") + "; ");
}
sb.Append(" carStart " + cs.position.ToString("F1") + " pack " + K("TUT_PackTrigger").position.ToString("F1") + " tea " + K("TUT_TeaDelivery").position.ToString("F1") + " gates " + K("TUT_CoastGateStart").position.ToString("F1") + "→" + K("TUT_CoastGateEnd").position.ToString("F1") + " return " + K("TUT_ReturnGarage").position.ToString("F1") + " room " + K("TUT_Room").position.ToString("F1"));
// 차도 출발점에
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); car.transform.SetPositionAndRotation(cs.position, cs.rotation);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return sb.ToString();
