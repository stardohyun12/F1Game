// 2253: 먼 낮은 칸(diag_far.txt)이 메움에서 얼마나 올랐나 — terrace1(입력) vs 지금 지형. 그리고 ParcelBuild 칸 종류.
var T = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/relief3/");
var top = GameObject.Find("TownV6_Gray").transform; var terr = top.Find("TownV6_Terrain"); var tc = terr.GetComponent<MeshCollider>();
var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_terrace1.asset");
var go = new GameObject("_t1") { hideFlags = HideFlags.HideAndDontSave }; go.transform.SetPositionAndRotation(terr.position, terr.rotation); go.transform.localScale = terr.lossyScale;
var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = m; Physics.SyncTransforms();
var inv = System.Globalization.CultureInfo.InvariantCulture; var sb = new System.Text.StringBuilder(); int n = 0, raised = 0, notRaised = 0;
try
{
    foreach (var ln in System.IO.File.ReadAllLines(T + "diag_far.txt"))
    {
        var a = ln.Split(' '); if (a.Length < 5) continue; float x = float.Parse(a[1], inv), z = float.Parse(a[2], inv); var ray = new Ray(new Vector3(x, 400, z), Vector3.down);
        float y1 = mc.Raycast(ray, out var h1, 800) ? h1.point.y : float.NaN, y = tc.Raycast(ray, out var h, 800) ? h.point.y : float.NaN;
        n++; if (y - y1 > .05f) raised++; else notRaised++;
        if (n % 12 == 0) sb.Append($"{a[0]} ({x},{z}) dd {a[3]} dist {a[4]} t1 {y1:F2} now {y:F2} up {y - y1:F2}\n");
    }
}
finally { UnityEngine.Object.DestroyImmediate(go); }
return $"far {n} raised {raised} notRaised {notRaised}\n" + sb;
