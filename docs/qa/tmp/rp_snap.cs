// roadpaint 검증: RoadsV2·TerraceBlock 자식별 정점 해시 + 지형 정점을 파일로 (before/after 비교용). 인자 = SessionState "rp_snap_tag"
var tag = SessionState.GetString("rp_snap_tag", "a");
var top = GameObject.Find("TownV6_Gray").transform;
var sb = new System.Text.StringBuilder();
foreach (var nm in new[] { "RoadsV2", "TerraceBlock" })
{
    var t = top.Find(nm); if (t == null) { sb.AppendLine(nm + "\t-"); continue; }
    foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
    {
        var m = mf.sharedMesh; if (m == null) continue; long h = m.vertexCount; var b = mf.GetComponent<Renderer>().bounds;
        foreach (var v in m.vertices) h = h * 31 + (long)(v.x * 100) * 7 + (long)(v.y * 100) * 13 + (long)(v.z * 100);
        sb.AppendLine($"{nm}/{mf.name}\t{h}\t{b.center.x:0.0}\t{b.center.z:0.0}\t{b.extents.x:0.0}\t{b.extents.z:0.0}");
    }
}
var dir = "D:/Github/F1Game/docs/qa/tmp/";
System.IO.File.WriteAllText(dir + "rp_snap_" + tag + ".tsv", sb.ToString());
var terr = top.Find("TownV6_Terrain"); var tm = terr.GetComponent<MeshFilter>().sharedMesh; var tr = terr.transform;
var vs = tm.vertices; var bw = new System.IO.BinaryWriter(System.IO.File.Create(dir + "rp_terr_" + tag + ".bin"));
foreach (var v in vs) { var w = tr.TransformPoint(v); bw.Write(w.x); bw.Write(w.y); bw.Write(w.z); }
bw.Close();
return $"snap {tag}: objs {sb.ToString().Split('\n').Length - 1}, terrain {tm.name} verts {vs.Length}";
