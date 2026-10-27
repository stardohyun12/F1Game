// 과제 0311: 고리 길 위로 지형(TownV6_Terrain)이 뚫고 올라온 자리 고침. SessionState "tv6.terr" = count(재기만) | fix(사본 메시 만들어 끼움)
// fix: 고리 중심선 5m 안 지형 꼭짓점 중 바로 아래·위에 길 판이 있고 길보다 높거나 8cm 안인 것 → 길 높이 -8cm. 사본 Mesh_Terrain_loopfix.asset, 원본 그대로.
string mode = UnityEditor.SessionState.GetString("tv6.terr", "count");
foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) if (g.name == "TV6_LoopFix") UnityEngine.Object.DestroyImmediate(g);
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var L = System.IO.File.ReadAllLines(dir + "tv6_loop_pts.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector3(float.Parse(a[0]), 0, float.Parse(a[1])); }).ToList();
System.Func<float, float, float> RoadY = (x, z) => { float y = float.NaN; foreach (var h in Physics.RaycastAll(new Vector3(x, 200, z), Vector3.down, 400, ~0, QueryTriggerInteraction.Ignore)) if (h.collider.name.StartsWith("ROAD") && (float.IsNaN(y) || h.point.y > y)) y = h.point.y; return y; };
System.Func<int> Count = () => { int n = 0; for (int i = 0; i < L.Count; i++) { var f = L[Mathf.Min(L.Count - 1, i + 1)] - L[Mathf.Max(0, i - 1)]; f.y = 0; var sd = Vector3.Cross(Vector3.up, f.normalized); for (float o = -2.5f; o <= 2.5f; o += 0.5f) { var p = L[i] + sd * o; if (Physics.Raycast(new Vector3(p.x, 200, p.z), Vector3.down, out var h, 400, ~0, QueryTriggerInteraction.Ignore) && h.collider.name.Contains("Terrain") && !float.IsNaN(RoadY(p.x, p.z))) n++; } } return n; };
var sb = new System.Text.StringBuilder("terrain-over-road samples(±2.5m) before " + Count() + "; ");
if (mode == "fix")
{
    var tg = GameObject.Find("TownV6_Terrain"); var mf = tg.GetComponent<MeshFilter>(); var mc = tg.GetComponent<MeshCollider>(); var tw = tg.transform;
    var src = mf.sharedMesh; if (src.name.EndsWith("_loopfix")) src = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(UnityEditor.AssetDatabase.GetAssetPath(src).Replace("_loopfix", ""));
    sb.Append("src " + UnityEditor.AssetDatabase.GetAssetPath(src) + " verts " + src.vertexCount + "; ");
    var m = UnityEngine.Object.Instantiate(src); m.name = src.name + "_loopfix"; var V = m.vertices; int moved = 0; float maxDrop = 0; var LY = Enumerable.Repeat(float.NaN, L.Count).ToArray();
    for (int k = 0; k < V.Length; k++)
    {
        var w = tw.TransformPoint(V[k]); bool near = false; foreach (var q in L) if ((q.x - w.x) * (q.x - w.x) + (q.z - w.z) * (q.z - w.z) < 25f) { near = true; break; }
        if (!near) continue; float ry = RoadY(w.x, w.z);
        // 중심선 4.5m 안: 가장 가까운 중심선 점의 길 높이를 상한으로(길 가장자리 밖 높은 꼭짓점이 삼각형으로 길 위를 덮는 것 방지)
        { int bi = -1; float bd = 4.5f * 4.5f; for (int qi = 0; qi < L.Count; qi++) { float d = (L[qi].x - w.x) * (L[qi].x - w.x) + (L[qi].z - w.z) * (L[qi].z - w.z); if (d < bd) { bd = d; bi = qi; } }
          if (bi >= 0) { if (float.IsNaN(LY[bi])) LY[bi] = RoadY(L[bi].x, L[bi].z); if (!float.IsNaN(LY[bi])) ry = float.IsNaN(ry) ? LY[bi] : Mathf.Min(ry, LY[bi]); } }
        if (float.IsNaN(ry) || w.y < ry - 0.08f) continue;
        float drop = w.y - (ry - 0.08f); maxDrop = Mathf.Max(maxDrop, drop); w.y = ry - 0.08f; V[k] = tw.InverseTransformPoint(w); moved++;
    }
    m.vertices = V; m.RecalculateNormals(); m.RecalculateBounds();
    string path = UnityEditor.AssetDatabase.GetAssetPath(src).Replace(".asset", "_loopfix.asset"); if (!path.EndsWith("_loopfix.asset")) path = "Assets/Scenes/Map_TownV6_Mats/" + m.name + ".asset";
    UnityEditor.AssetDatabase.DeleteAsset(path); UnityEditor.AssetDatabase.CreateAsset(m, path);
    mf.sharedMesh = m; mc.sharedMesh = null; mc.sharedMesh = m; UnityEditor.EditorUtility.SetDirty(mf); UnityEditor.EditorUtility.SetDirty(mc);
    Physics.SyncTransforms();
    sb.Append("moved verts " + moved + " maxDrop " + maxDrop.ToString("F2") + " → " + path + "; after " + Count());
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tg.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(tg.scene);
}
return sb.ToString();
