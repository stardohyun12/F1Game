// 과제 0311: Walls_0(씬 전용 합친 낮은 벽) 중 고리 중심선(tv6_loop_pts + tv6_go_pts) 수평 2.3m 안으로 들어온 삼각형을 뺀 사본 메시
// → Assets/Scenes/Map_TownV6_Mats/Mesh_Walls_0_loopfix.asset, Walls_0 의 MeshFilter·MeshCollider 에 끼움. 원본 Mesh_Walls_0 은 그대로.
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var L = new List<Vector2>();
foreach (var f in new[] { "tv6_loop_pts.txt", "tv6_go_pts.txt" }) if (System.IO.File.Exists(dir + f)) foreach (var l in System.IO.File.ReadAllLines(dir + f)) { if (l.Trim() == "") continue; var a = l.Split(' '); L.Add(new Vector2(float.Parse(a[0]), float.Parse(a[1]))); }
var w = GameObject.Find("Walls_0"); var mf = w.GetComponent<MeshFilter>(); var mc = w.GetComponent<MeshCollider>();
var src = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_Walls_0.asset");
var m = UnityEngine.Object.Instantiate(src); m.name = "Mesh_Walls_0_loopfix";
var V = m.vertices; var tw = w.transform; float R = 2.3f * 2.3f; int cut = 0; var cutAt = new List<string>();
for (int s = 0; s < m.subMeshCount; s++)
{
    var T = m.GetTriangles(s); var keep = new List<int>();
    for (int t = 0; t < T.Length; t += 3)
    {
        var a = tw.TransformPoint(V[T[t]]); var b = tw.TransformPoint(V[T[t + 1]]); var c = tw.TransformPoint(V[T[t + 2]]);
        bool hit = false; Vector3 hp = a;
        int N = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max((b - a).magnitude, (c - a).magnitude, (c - b).magnitude) / 0.8f), 2, 200);
        for (int i = 0; i <= N && !hit; i++) for (int j = 0; j <= N - i && !hit; j++)
        {
            var p = a + (b - a) * (i / (float)N) + (c - a) * (j / (float)N);
            foreach (var q in L) if ((q.x - p.x) * (q.x - p.x) + (q.y - p.z) * (q.y - p.z) < R) { hit = true; hp = p; break; }
        }
        if (hit) { cut++; if (cutAt.Count < 40) cutAt.Add(hp.x.ToString("F0") + "," + hp.z.ToString("F0")); } else { keep.Add(T[t]); keep.Add(T[t + 1]); keep.Add(T[t + 2]); }
    }
    m.SetTriangles(keep, s);
}
string path = "Assets/Scenes/Map_TownV6_Mats/Mesh_Walls_0_loopfix.asset";
UnityEditor.AssetDatabase.DeleteAsset(path); UnityEditor.AssetDatabase.CreateAsset(m, path);
mf.sharedMesh = m; mc.sharedMesh = null; mc.sharedMesh = m;
UnityEditor.EditorUtility.SetDirty(mf); UnityEditor.EditorUtility.SetDirty(mc);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(w.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(w.scene);
return "loopPts " + L.Count + " cut tris " + cut + " / " + src.triangles.Length / 3 + " at " + string.Join(" ", cutAt.Distinct());
