// D-198 — Map_TownV6 에 라이브러리 건물 4채 배치 (에디트 모드, 한 번만 돌림)
var sb = new System.Text.StringBuilder();
var gray = GameObject.Find("TownV6_Gray").transform;
var terr = gray.Find("TownV6_Terrain"); var tmc = terr.GetComponent<MeshCollider>(); var tmf = terr.GetComponent<MeshFilter>();
if (GameObject.Find("D198_Buildings")) return "already placed";
// id, 정면 가운데 x,z, yaw, 로컬 발자국(minx,minz,maxx,maxz), 옛 자리 상자, 옛 정면 yaw(상자 yaw-90)
var P = new (string id, float x, float z, float yaw, Vector4 b, string slot)[] {
 ("room", -117.69f, -10.3f, 0.9f, new Vector4(-1.75f,-3.16f,1.75f,0f), "Q1_HomeRoom"),
 ("garage", -63.2f, -6.7f, -174.8f, new Vector4(-8f,-24.91f,8f,0f), "Q2_Garage"),
 ("depot", -43.77f, -28.2f, 2.7f, new Vector4(-5.01f,-7f,5f,1.01f), "Q3_Depot"),
 ("postoffice", -57.51f, -27.64f, -5.3f, new Vector4(-5.34f,-8.86f,5.1f,1.08f), "Q4_PostOffice") };
float TY(float x, float z) { RaycastHit h; return tmc.Raycast(new Ray(new Vector3(x, 300, z), Vector3.down), out h, 600) ? h.point.y : float.NaN; }
// 1) 터 고르기: 발자국+2 m 안 = pad-0.02, 그 밖 4 m 는 원래 높이로 녹임
var mesh = tmf.sharedMesh; var v = mesh.vertices; var pads = new Dictionary<string, float>(); int moved = 0;
foreach (var p in P) {
  float pad = TY(p.x, p.z); pads[p.id] = pad;
  var q = Quaternion.Euler(0, p.yaw, 0); var inv = Quaternion.Inverse(q); var o = new Vector3(p.x, 0, p.z);
  for (int i = 0; i < v.Length; i++) {
    var w = terr.TransformPoint(v[i]); var l = inv * new Vector3(w.x - o.x, 0, w.z - o.z);
    float dx = Mathf.Max(p.b.x - l.x, 0, l.x - p.b.z), dz = Mathf.Max(p.b.y - l.z, 0, l.z - p.b.w); float d = Mathf.Sqrt(dx * dx + dz * dz);
    if (d > 6f) continue;
    float t = d <= 2f ? 1f : Mathf.SmoothStep(1f, 0f, (d - 2f) / 4f);
    float ny = Mathf.Lerp(w.y, pad - 0.02f, t); if (Mathf.Abs(ny - w.y) < 0.001f) continue;
    w.y = ny; v[i] = terr.InverseTransformPoint(w); moved++;
  }
}
mesh.vertices = v; mesh.RecalculateNormals(); mesh.RecalculateBounds(); UnityEditor.EditorUtility.SetDirty(mesh);
tmc.sharedMesh = null; tmc.sharedMesh = mesh;
sb.Append("terrain verts moved " + moved + "\n");
// 2) 건물 놓기 + 자리 상자 옮겨 끄기
var root = new GameObject("D198_Buildings").transform; root.SetParent(gray, false);
var oldFront = new Dictionary<string, (Vector3 p, float yaw)>(); var newFront = new Dictionary<string, (Vector3 p, float yaw)>();
foreach (var p in P) {
  var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Library/Buildings/" + p.id + "/" + p.id + ".prefab");
  var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, root);
  go.name = "D198_" + p.id; go.transform.SetPositionAndRotation(new Vector3(p.x, pads[p.id], p.z), Quaternion.Euler(0, p.yaw, 0));
  var s = GameObject.Find(p.slot).transform; float oy = s.eulerAngles.y - 90f;
  var of = s.position + Quaternion.Euler(0, oy, 0) * new Vector3(0, 0, 2f); of.y = s.position.y - s.lossyScale.y * 0.5f;
  oldFront[p.id] = (of, oy); newFront[p.id] = (new Vector3(p.x, pads[p.id], p.z), p.yaw);
  var cen = Quaternion.Euler(0, p.yaw, 0) * new Vector3((p.b.x + p.b.z) / 2, 0, (p.b.y + p.b.w) / 2);
  s.SetPositionAndRotation(new Vector3(p.x + cen.x, pads[p.id] + s.lossyScale.y * 0.5f, p.z + cen.z), Quaternion.Euler(0, p.yaw + 90f, 0));
  s.gameObject.SetActive(false);
  sb.Append(p.id + " pad=" + pads[p.id].ToString("F2") + " oldFront=" + of.ToString("F1") + " oy=" + oy.ToString("F0") + "\n");
}
// 3) 정비소 밑을 지나는 마을길 47 끄기
var r47 = gray.Find("Roads/ROAD_tomo_726989353_47"); if (r47) { r47.gameObject.SetActive(false); sb.Append("road47 off\n"); } else sb.Append("road47 NOT FOUND\n");
// 4) 튜토리얼 묶음을 건물 정면 기준으로 통째로 옮김
void Rigid(Transform t, string id) {
  var a = oldFront[id]; var b = newFront[id]; var dq = Quaternion.Euler(0, b.yaw - a.yaw, 0);
  var rel = t.position - a.p; var np = b.p + dq * new Vector3(rel.x, 0, rel.z); np.y = b.p.y + rel.y;
  t.SetPositionAndRotation(np, dq * t.rotation);
}
Transform F(string n) { foreach (var r in UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
Rigid(F("TUT_Room"), "room");
Rigid(F("TUT_Garage_PitCrew"), "garage");
foreach (var n in new[] { "TUT_PackingSpot", "TUT_MartOwner", "Shot_3a", "Shot_3b" }) Rigid(F(n), "depot");
// 도로 위 점(loop.py anchors.json): 출발·귀환 = 정비소 앞, 포장 = 보관소 앞
float RY(float x, float z) { var hs = Physics.RaycastAll(new Vector3(x, 300, z), Vector3.down, 600); return hs.Length == 0 ? float.NaN : hs.Max(h => h.point.y); }
var cs = F("TUT_CarStart"); var oldCs = cs.position; var oldCsYaw = cs.eulerAngles.y;
cs.SetPositionAndRotation(new Vector3(-63.35f, RY(-63.35f, -19.06f) + 0.3f, -19.06f), Quaternion.Euler(0, 90.7f, 0));
var s4 = F("Shot_4a"); var r4 = s4.position - oldCs; var dq4 = Quaternion.Euler(0, 90.7f - oldCsYaw, 0);
s4.SetPositionAndRotation(cs.position + dq4 * r4, dq4 * s4.rotation);
F("TUT_ReturnGarage").SetPositionAndRotation(new Vector3(-61.35f, RY(-61.35f, -19.1f) + 0.3f, -19.1f), Quaternion.Euler(0, 90.7f, 0));
F("TUT_PackTrigger").SetPositionAndRotation(new Vector3(-43.37f, RY(-43.37f, -24.64f) + 0.5f, -24.64f), Quaternion.Euler(0, 96.4f, 0));
foreach (var n in new[] { "TUT_Room", "Shot_1a", "TUT_Garage_PitCrew", "TUT_GarageCarSpot", "TUT_Mechanic", "ShutterRoll", "Shot_2a", "TUT_CarStart", "Shot_4a", "TUT_ReturnGarage", "TUT_PackTrigger", "TUT_MartOwner", "Shot_3a" }) { var t = F(n); sb.Append(n + " -> " + (t ? t.position.ToString("F1") + " yaw " + t.eulerAngles.y.ToString("F0") : "null") + "\n"); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
return sb.ToString();
