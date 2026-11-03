// 0301 검증: 씬 결과 지문. tag = docs/qa/tmp/region/tag.txt. 지형 정점·삼각형은 .bin, 나머지 물체는 경로별 해시 json(월드 정점 mm)
var dir = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/region/");
var tag = System.IO.File.ReadAllText(dir + "tag.txt").Trim();
var top = GameObject.Find("TownV6_Gray").transform;
var terr = top.Find("TownV6_Terrain"); var tm = terr.GetComponent<MeshFilter>().sharedMesh; var ttr = terr.transform;
var tv = tm.vertices; var tt = tm.triangles;
using (var bw = new System.IO.BinaryWriter(System.IO.File.Create(dir + tag + "_terrain.bin")))
{ bw.Write(tv.Length); foreach (var v in tv) { var w = ttr.TransformPoint(v); bw.Write(w.x); bw.Write(w.y); bw.Write(w.z); } bw.Write(tt.Length); foreach (var i in tt) bw.Write(i); }
System.Func<ulong, long, ulong> H = (h, x) => { unchecked { h ^= (ulong)x; h *= 1099511628211UL; } return h; };
System.Func<Mesh, Transform, ulong> MeshH = (m, t) => { ulong h = 14695981039346656037UL; if (m == null) return 0; foreach (var v in m.vertices) { var w = t.TransformPoint(v); h = H(h, (long)Mathf.Round(w.x * 1000)); h = H(h, (long)Mathf.Round(w.y * 1000)); h = H(h, (long)Mathf.Round(w.z * 1000)); } for (int s = 0; s < m.subMeshCount; s++) foreach (var i in m.GetTriangles(s)) h = H(h, i); return h; };
System.Func<Transform, Transform, string> Path = (t, stop) => { var s = t.name; while (t.parent != null && t.parent != stop) { t = t.parent; s = t.name + "/" + s; } return s; };
var sb = new System.Text.StringBuilder(); sb.Append("{\"terrain\":{\"mesh\":\"" + tm.name + "\",\"v\":" + tv.Length + ",\"t\":" + tt.Length / 3 + ",\"h\":\"" + MeshH(tm, ttr).ToString("x16") + "\"},\"objs\":{");
bool first = true; int n = 0;
foreach (var rootName in new[] { "RoadsV2", "TerraceBlock" })
{
    var r = top.Find(rootName); if (r == null) continue;
    foreach (var mf in r.GetComponentsInChildren<MeshFilter>(true)) { if (!first) sb.Append(","); first = false; n++; sb.Append("\"" + rootName + "/" + mf.name + "#" + mf.transform.GetSiblingIndex() + "\":\"" + MeshH(mf.sharedMesh, mf.transform).ToString("x16") + "\""); }
}
foreach (var rootName in new[] { "Roadside", "Parcels_Plan" })
{
    var r = GameObject.Find(rootName); if (r == null) continue;
    foreach (var tr in r.GetComponentsInChildren<Transform>(true))
    {
        var mf = tr.GetComponent<MeshFilter>(); ulong h = mf != null ? MeshH(mf.sharedMesh, tr) : H(H(H(14695981039346656037UL, (long)Mathf.Round(tr.position.x * 1000)), (long)Mathf.Round(tr.position.y * 1000)), (long)Mathf.Round(tr.position.z * 1000));
        if (!first) sb.Append(","); first = false; n++; sb.Append("\"" + rootName + "/" + Path(tr, r.transform).Replace("\"", "'") + "#" + tr.GetSiblingIndex() + "\":\"" + h.ToString("x16") + "\"");
    }
}
sb.Append("}}");
System.IO.File.WriteAllText(dir + tag + ".json", sb.ToString());
return $"{tag}: terrain {tm.name} v{tv.Length} h{MeshH(tm, ttr):x16} objs {n}";
