// 과제 0311: 고리 갈림길 안쪽 모서리 포장 판 — 지형 턱·길 판 가장자리 위를 덮는 0.5m 격자 메시(콜라이더 포함).
// 높이 = 그 칸 길·지형 최고점(raw) → max(3×3 평균, raw) + 3cm. 칸 = 중심 반경 R 안 & 고리 중심선 5.5m 안. 다시 돌리면 TV6_LoopFix 를 새로.
var corners = new (float x, float z, float r)[] { (160f, 17f, 9f), (69f, -55f, 9f) };
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
foreach (var g in sc.GetRootGameObjects()) if (g.name == "TV6_LoopFix") UnityEngine.Object.DestroyImmediate(g);
var root = new GameObject("TV6_LoopFix");
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var L = System.IO.File.ReadAllLines(dir + "tv6_loop_pts.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector2(float.Parse(a[0]), float.Parse(a[1])); }).ToList();
Material roadMat = null; RoadSurface any = null;
foreach (var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { var rr = rs.GetComponent<Renderer>(); if (rr && rs.kind.ToString() == "village") { roadMat = rr.sharedMaterial; any = rs; break; } }
var sb = new System.Text.StringBuilder("mat " + (roadMat ? roadMat.name : "none") + " layer " + (any ? any.gameObject.layer : -1) + "; ");
int ci = 0; const float S = 0.5f;
foreach (var c in corners)
{
    int N = Mathf.CeilToInt(c.r * 2 / S) + 1; float x0 = c.x - c.r, z0 = c.z - c.r;
    var raw = new float[N, N]; var on = new bool[N, N];
    for (int i = 0; i < N; i++) for (int j = 0; j < N; j++)
    {
        float x = x0 + i * S, z = z0 + j * S; raw[i, j] = float.NaN;
        if ((x - c.x) * (x - c.x) + (z - c.z) * (z - c.z) > c.r * c.r) continue;
        float dmin = 1e9f; foreach (var q in L) dmin = Mathf.Min(dmin, (q.x - x) * (q.x - x) + (q.y - z) * (q.y - z)); if (dmin > 5.5f * 5.5f) continue;
        float y = float.NegativeInfinity;
        foreach (var h in Physics.RaycastAll(new Vector3(x, 200, z), Vector3.down, 400, ~0, QueryTriggerInteraction.Ignore)) if (h.collider.name.StartsWith("ROAD") || h.collider.name.Contains("Terrain")) y = Mathf.Max(y, h.point.y);
        if (float.IsInfinity(y)) continue; raw[i, j] = y; on[i, j] = true;
    }
    var mx = new float[N, N]; var hgt = new float[N, N];
    for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { float m = float.NegativeInfinity; for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++) { int u = i + a, v = j + b; if (u >= 0 && v >= 0 && u < N && v < N && on[u, v]) m = Mathf.Max(m, raw[u, v]); } mx[i, j] = on[i, j] ? raw[i, j] : float.NegativeInfinity; }
    for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { float s = 0; int k = 0; for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++) { int u = i + a, v = j + b; if (u >= 0 && v >= 0 && u < N && v < N && !float.IsInfinity(mx[u, v])) { s += mx[u, v]; k++; } } hgt[i, j] = k > 0 ? Mathf.Max(s / k, on[i, j] ? raw[i, j] : 0) + 0.03f : 0; }
    var V = new List<Vector3>(); var T = new List<int>(); var idx = new int[N, N]; float lift = 0;
    for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { idx[i, j] = -1; if (on[i, j]) { idx[i, j] = V.Count; V.Add(new Vector3(x0 + i * S, hgt[i, j], z0 + j * S)); lift = Mathf.Max(lift, hgt[i, j] - raw[i, j]); } }
    for (int i = 0; i + 1 < N; i++) for (int j = 0; j + 1 < N; j++) { int a = idx[i, j], b = idx[i + 1, j], d = idx[i, j + 1], e = idx[i + 1, j + 1]; if (a < 0 || b < 0 || d < 0 || e < 0) continue; T.AddRange(new[] { a, d, b, b, d, e }); }
    var m2 = new Mesh { name = "LoopPave_" + ci }; m2.SetVertices(V); m2.SetTriangles(T, 0); m2.SetUVs(0, V.Select(p => new Vector2(p.x * .25f, p.z * .25f)).ToList()); m2.RecalculateNormals(); m2.RecalculateBounds();
    string path = "Assets/Scenes/Map_TownV6_Mats/LoopPave_" + ci + ".asset"; UnityEditor.AssetDatabase.DeleteAsset(path); UnityEditor.AssetDatabase.CreateAsset(m2, path);
    var go = new GameObject("LoopPave_" + ci + "_" + c.x + "_" + c.z); go.transform.SetParent(root.transform); if (any) go.layer = any.gameObject.layer;
    go.AddComponent<MeshFilter>().sharedMesh = m2; go.AddComponent<MeshRenderer>().sharedMaterial = roadMat; go.AddComponent<MeshCollider>().sharedMesh = m2;
    sb.Append(go.name + " verts " + V.Count + " tris " + T.Count / 3 + " maxLift " + lift.ToString("F2") + "; "); ci++;
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return sb.ToString();
