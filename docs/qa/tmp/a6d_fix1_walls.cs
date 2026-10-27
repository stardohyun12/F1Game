// 과제 1815 돌담만 다시 짓기(에디트 모드). 높이 H0 는 docs/qa/tmp/fix1_wallh.txt.
//  보이는 담 = 땅을 따라가는 돌 띠(앞면 zF, 두께 0.42), 콜라이더 = 구간마다 속이 찬 BoxCollider(보이는 담 높이 그대로, 뒤로만 1m 흙 속).
//  봇2 실측: 0.55m 는 카트가 타고 넘음(12회 중 7회), MeshCollider 껍데기도 같은 결과.
const string D = "Assets/Art/SceneA6d/"; const float zF = 0.15f, zB = -0.27f, step = 0.5f, TH = 1.0f, EXT = 1.0f;   // EXT: 담을 옛 연석 양 끝보다 1m 씩 길게(35° 로 긁다 끝 모서리를 돌아 나감 — 봇2)
float H0 = float.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/fix1_wallh.txt")).Trim(), System.Globalization.CultureInfo.InvariantCulture);
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); var root = GameObject.Find("SceneA4").transform; var log = new System.Text.StringBuilder("H0 " + H0 + "\n");
var sm = AssetDatabase.LoadAssetAtPath<Material>(D + "Mats/SA6d_stonewall.mat");
var wmesh = GameObject.Find("Wall_L").GetComponent<MeshFilter>().sharedMesh; var wtf = GameObject.Find("Wall_L").transform; var wv = wmesh.vertices; var wu = wmesh.uv; var wt = wmesh.triangles; double sa = 0, su = 0;
for (int i = 0; i < wt.Length; i += 3) { var a = wtf.TransformPoint(wv[wt[i]]); var b = wtf.TransformPoint(wv[wt[i + 1]]); var c = wtf.TransformPoint(wv[wt[i + 2]]); sa += Vector3.Cross(b - a, c - a).magnitude; Vector3 ua = wu[wt[i]], ub = wu[wt[i + 1]], uc = wu[wt[i + 2]]; su += Vector3.Cross(ub - ua, uc - ua).magnitude; }
float uvPerM = (float)System.Math.Sqrt(su / System.Math.Max(1e-6, sa));
var wroot = root.Find("A6d_StoneWalls"); if (wroot) UnityEngine.Object.DestroyImmediate(wroot.gameObject); wroot = new GameObject("A6d_StoneWalls").transform; wroot.SetParent(root, false);
var rnd = new System.Random(1815);
// 마찰 0 (옆으로 긁고 지나갈 때 담을 붙잡고 올라타지 않게) — 씬 전용 에셋
string pmp = D + "Walls/StoneWall_slide.physicsMaterial"; var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmp);
if (!pm) { pm = new PhysicsMaterial("StoneWall_slide"); AssetDatabase.CreateAsset(pm, pmp); }
pm.dynamicFriction = 0f; pm.staticFriction = 0f; pm.frictionCombine = PhysicsMaterialCombine.Minimum; pm.bounciness = 0f; EditorUtility.SetDirty(pm);
System.Func<Vector3, float> Ground = wp => { float best = float.NaN; foreach (var hh in Physics.RaycastAll(wp + Vector3.up * 6f, Vector3.down, 20f)) { var nm = hh.collider.name; if (nm == "Road" || nm == "DryVerge" || nm.Contains("Grass") || nm.Contains("Ground") || nm.Contains("Terrain")) if (float.IsNaN(best) || hh.point.y > best) best = hh.point.y; } return best; };
foreach (Transform cb in root.Find("A4e_Curbs")) {
  var cbc = cb.GetComponent<BoxCollider>(); cbc.enabled = false; float L = cbc.size.x + 2f * EXT; int N = Mathf.Max(2, Mathf.CeilToInt(L / step)); float dx = L / N;
  var go = new GameObject("StoneWall_" + cb.name.Substring(5)); go.transform.SetParent(wroot, false); go.transform.SetPositionAndRotation(cb.position, cb.rotation); go.layer = cb.gameObject.layer;
  var top = new float[N + 1]; var bot = new float[N + 1]; float last = 0f;
  for (int i = 0; i <= N; i++) { float x = -L * 0.5f + i * dx; float gF = Ground(cb.TransformPoint(new Vector3(x, 0, zF))), gB = Ground(cb.TransformPoint(new Vector3(x, 0, zB)));
    float gy = float.IsNaN(gF) && float.IsNaN(gB) ? last : float.IsNaN(gF) ? gB : float.IsNaN(gB) ? gF : Mathf.Max(gF, gB); last = gy;
    float gl = float.IsNaN(gF) || float.IsNaN(gB) ? gy : Mathf.Min(gF, gB);
    top[i] = gy - cb.position.y + H0 + (float)(rnd.NextDouble() * 2 - 1) * 0.03f; bot[i] = gl - cb.position.y - 0.25f; }
  var V = new System.Collections.Generic.List<Vector3>(); var U = new System.Collections.Generic.List<Vector2>(); var T = new System.Collections.Generic.List<int>();
  System.Action<Vector3, Vector3, Vector3, Vector3> Quad = (a, b, c, d) => { int o = V.Count; V.Add(a); V.Add(b); V.Add(c); V.Add(d); T.Add(o); T.Add(o + 1); T.Add(o + 2); T.Add(o); T.Add(o + 2); T.Add(o + 3); };
  for (int i = 0; i < N; i++) { float x0 = -L * 0.5f + i * dx, x1 = x0 + dx;
    Quad(new Vector3(x1, bot[i + 1], zF), new Vector3(x1, top[i + 1], zF), new Vector3(x0, top[i], zF), new Vector3(x0, bot[i], zF));
    Quad(new Vector3(x0, bot[i], zB), new Vector3(x0, top[i], zB), new Vector3(x1, top[i + 1], zB), new Vector3(x1, bot[i + 1], zB));
    Quad(new Vector3(x0, top[i], zB), new Vector3(x0, top[i], zF), new Vector3(x1, top[i + 1], zF), new Vector3(x1, top[i + 1], zB));
    var cg = new GameObject("Col_" + i); cg.layer = go.layer; cg.transform.SetParent(go.transform, false); float ang = Mathf.Atan2(top[i + 1] - top[i], dx), len = Mathf.Sqrt(dx * dx + (top[i + 1] - top[i]) * (top[i + 1] - top[i]));
    float h = Mathf.Min(top[i] - bot[i], top[i + 1] - bot[i + 1]); cg.transform.localPosition = new Vector3((x0 + x1) * 0.5f, (top[i] + top[i + 1]) * 0.5f, zF); cg.transform.localRotation = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg);
    var bc = cg.AddComponent<BoxCollider>(); bc.center = new Vector3(0, -h * 0.5f, -TH * 0.5f); bc.size = new Vector3(len + 0.04f, h, TH); bc.sharedMaterial = pm; }
  float xa = -L * 0.5f, xb = L * 0.5f;
  Quad(new Vector3(xa, bot[0], zF), new Vector3(xa, top[0], zF), new Vector3(xa, top[0], zB), new Vector3(xa, bot[0], zB));
  Quad(new Vector3(xb, bot[N], zB), new Vector3(xb, top[N], zB), new Vector3(xb, top[N], zF), new Vector3(xb, bot[N], zF));
  // UV: 앞·뒷면은 (x, y), 윗면은 (x, z), 마구리는 (z, y) — 1m = uvPerM (길 벽과 같은 결 크기)
  for (int q = 0; q < V.Count; q += 4) { var n = Vector3.Cross(V[q + 1] - V[q], V[q + 2] - V[q]); for (int j = 0; j < 4; j++) { var p = V[q + j]; U.Add(Mathf.Abs(n.y) > Mathf.Max(Mathf.Abs(n.x), Mathf.Abs(n.z)) ? new Vector2(p.x, p.z) * uvPerM : Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) * uvPerM : new Vector2(p.x, p.y) * uvPerM); } }
  var mesh = new Mesh { name = go.name }; mesh.SetVertices(V); mesh.SetUVs(0, U); mesh.SetTriangles(T, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
  string mp = D + "Walls/" + go.name + ".asset"; AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
  go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = sm;
  log.Append(go.name + " segs " + N + "; "); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
return log.ToString();
