// 과제 0422: 고리 위 길 판 이음매 단차(>4cm) 찾기 + 높은 판 끝 꼭짓점을 낮은 판 높이로 내린 사본 메시(Mesh_<판>_loopfix.asset). SessionState "tv6.seam" = count | fix
string mode = UnityEditor.SessionState.GetString("tv6.seam", "count");
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var L = System.IO.File.ReadAllLines(dir + "tv6_loop_pts.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
var seams = new List<(Vector3 p, Collider hi, float lowY, float dy)>();
System.Func<List<(Vector3 p, Collider hi, float lowY, float dy)>> Scan = () =>
{
    var S = new List<(Vector3, Collider, float, float)>();
    foreach (var o in new[] { -1.2f, 0f, 1.2f })
    {
        RaycastHit ph = default; bool has = false;
        for (int i = 0; i + 1 < L.Count; i++)
        {
            var a = L[i]; var b = L[i + 1]; var f = b - a; float len = f.magnitude; if (len < 0.01f) continue; f /= len; var sd = Vector3.Cross(Vector3.up, f);
            for (float s = 0; s < len; s += 0.25f)
            {
                var p = a + f * s + sd * o;
                if (!Physics.Raycast(new Vector3(p.x, 200, p.z), Vector3.down, out var h, 400, ~0, QueryTriggerInteraction.Ignore)) { has = false; continue; }
                if (has && h.collider != ph.collider && h.collider.name.StartsWith("ROAD") && ph.collider.name.StartsWith("ROAD"))
                {
                    float dy = h.point.y - ph.point.y;
                    if (Mathf.Abs(dy) > 0.04f && Mathf.Abs(dy) < 0.4f) S.Add(dy > 0 ? (h.point, h.collider, ph.point.y, dy) : (ph.point, ph.collider, h.point.y, -dy));
                }
                ph = h; has = true;
            }
        }
    }
    return S;
};
seams = Scan();
var sb = new System.Text.StringBuilder("seams " + seams.Count + ": ");
foreach (var g in seams.GroupBy(x => x.hi.name)) sb.Append(g.Key + " n" + g.Count() + " max" + g.Max(x => x.dy).ToString("F2") + " @" + g.First().p.ToString("F0") + "; ");
if (mode == "fix")
{
    int moved = 0;
    foreach (var g in seams.GroupBy(x => x.hi))
    {
        var col = g.Key as MeshCollider; var mf = col ? col.GetComponent<MeshFilter>() : null; if (!col || !mf) { sb.Append("skip " + g.Key.name + "; "); continue; }
        var src = mf.sharedMesh; if (src.name.EndsWith("_loopfix")) { var op = UnityEditor.AssetDatabase.GetAssetPath(src).Replace("_loopfix", ""); var o2 = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(op); if (o2) src = o2; }
        var m = UnityEngine.Object.Instantiate(src); m.name = src.name.Replace("_loopfix", "") + "_loopfix"; var V = m.vertices; var tw = col.transform;
        for (int k = 0; k < V.Length; k++)
        {
            var w = tw.TransformPoint(V[k]); float best = 1e9f; float ly = 0;
            foreach (var s in g) { float d = (s.p.x - w.x) * (s.p.x - w.x) + (s.p.z - w.z) * (s.p.z - w.z); if (d < best) { best = d; ly = s.lowY; } }
            if (best > 1.2f * 1.2f || w.y <= ly + 0.005f || w.y > ly + 0.45f) continue;
            w.y = ly + 0.005f; V[k] = tw.InverseTransformPoint(w); moved++;
        }
        m.vertices = V; m.RecalculateNormals(); m.RecalculateBounds();
        string path = "Assets/Scenes/Map_TownV6_Mats/Mesh_" + m.name + ".asset";
        UnityEditor.AssetDatabase.DeleteAsset(path); UnityEditor.AssetDatabase.CreateAsset(m, path);
        mf.sharedMesh = m; col.sharedMesh = null; col.sharedMesh = m; UnityEditor.EditorUtility.SetDirty(mf); UnityEditor.EditorUtility.SetDirty(col);
    }
    Physics.SyncTransforms();
    sb.Append("moved verts " + moved + " → after " + Scan().Count);
    var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
}
return sb.ToString();
