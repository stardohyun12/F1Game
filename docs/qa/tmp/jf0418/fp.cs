// 씬 지문: 모든 MeshFilter/MeshCollider/Collider 물체의 경로·켜짐·월드 변환·메시 정점 해시(1 mm). 정렬된 줄 → 파일. 읽기만.
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
string tag = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/jf0418/fp_tag.txt")).Trim();
var lines = new System.Collections.Generic.List<string>();
string Path(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
long MH(Mesh m)
{
    if (m == null) return 0; unchecked
    {
        long h = 1469598103934665603; var vs = m.vertices;
        foreach (var v in vs) { h = (h ^ Mathf.RoundToInt(v.x * 1000)) * 1099511628211; h = (h ^ Mathf.RoundToInt(v.y * 1000)) * 1099511628211; h = (h ^ Mathf.RoundToInt(v.z * 1000)) * 1099511628211; }
        foreach (var i in m.triangles) h = (h ^ i) * 1099511628211; return h;
    }
}
foreach (var r in sc.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(true))
{
    var mf = t.GetComponent<MeshFilter>(); var col = t.GetComponent<Collider>(); if (mf == null && col == null) continue;
    var mc = col as MeshCollider;
    string pos = $"{t.position.x:F3},{t.position.y:F3},{t.position.z:F3} r{t.eulerAngles.y:F1},{t.eulerAngles.x:F1},{t.eulerAngles.z:F1} s{t.lossyScale.x:F3}";
    lines.Add($"{Path(t)}\t{(t.gameObject.activeInHierarchy ? 1 : 0)}\t{pos}\tmf:{(mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount + ":" + MH(mf.sharedMesh) : "-")}\tcol:{(col == null ? "-" : col.GetType().Name + (col.enabled ? "" : "(off)"))}{(mc != null && mc.sharedMesh != null ? ":" + mc.sharedMesh.vertexCount + ":" + MH(mc.sharedMesh) : "")}");
}
lines.Sort(System.StringComparer.Ordinal);
System.IO.File.WriteAllLines(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/jf0418/fp_" + tag + ".tsv"), lines);
return tag + " objects " + lines.Count;
