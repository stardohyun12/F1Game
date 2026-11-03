// 1935: 블록을 가로지르는 선 위 2 m 마다 지형 단계별 높이(원본·loopfix·d203·ridge·0433·v2·terrace1·parcels) + 지금 길 면(RoadsV2) → profile.tsv
Physics.SyncTransforms();
var top = GameObject.Find("TownV6_Gray").transform; var terr = top.Find("TownV6_Terrain");
string F = "Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain";
var st = new[] { "", "_loopfix", "_d203", "_ridge", "_0433", "_v2", "_terrace1", "_parcels" };
var cols = new System.Collections.Generic.List<MeshCollider>(); var tmp = new System.Collections.Generic.List<GameObject>();
foreach (var s in st)
{
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(F + s + ".asset");
    var go = new GameObject("_pf" + s) { hideFlags = HideFlags.HideAndDontSave }; go.transform.SetPositionAndRotation(terr.position, terr.rotation); go.transform.localScale = terr.lossyScale;
    var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = m; cols.Add(mc); tmp.Add(go);
}
Physics.SyncTransforms();
var rv = new System.Collections.Generic.HashSet<Collider>(top.Find("RoadsV2").GetComponentsInChildren<Collider>(false));
var lines = new (string n, Vector2 a, Vector2 b)[] {
    ("A_WE", new Vector2(80, 35), new Vector2(145, 5)), ("A_NS", new Vector2(115, 65), new Vector2(112, -10)),
    ("B_WE", new Vector2(80, -45), new Vector2(145, -50)), ("B_NS", new Vector2(110, -5), new Vector2(112, -85)) };
var sb = new System.Text.StringBuilder(); sb.Append("line\ts\tx\tz\troad\tbase\tloopfix\td203\tridge\t0433\tv2\tterrace1\tparcels\n");
var hits = new RaycastHit[32];
try
{
    foreach (var (n, a, b) in lines)
    {
        float L = (b - a).magnitude;
        for (float s = 0; s <= L; s += 2f)
        {
            var p = Vector2.Lerp(a, b, s / L); var ray = new Ray(new Vector3(p.x, 300, p.y), Vector3.down);
            float road = float.NaN; int k = Physics.RaycastNonAlloc(ray, hits, 600); for (int q = 0; q < k; q++) if (rv.Contains(hits[q].collider) && (float.IsNaN(road) || hits[q].point.y > road)) road = hits[q].point.y;
            sb.Append($"{n}\t{s:F0}\t{p.x:F1}\t{p.y:F1}\t{road:F2}");
            foreach (var c in cols) sb.Append("\t" + (c.Raycast(ray, out var h, 600) ? h.point.y.ToString("F2") : "nan"));
            sb.Append("\n");
        }
    }
}
finally { foreach (var g in tmp) UnityEngine.Object.DestroyImmediate(g); }
var path = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/sink1935/profile.tsv");
System.IO.File.WriteAllText(path, sb.ToString());
return path;
