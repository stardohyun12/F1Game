// PadWalls 메시에서 칼날 자리(279,-25) 4 m 안 삼각형 — 재질(서브메시)·높이·지형과의 차
var pw = GameObject.Find("TownV6_Gray/Parcels/PadWalls") ?? UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r => r.name == "PadWalls").gameObject;
var mf = pw.GetComponent<MeshFilter>(); var m = mf.sharedMesh; var tr = pw.transform; var V = m.vertices;
var terr = GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
var sb = new System.Text.StringBuilder(pw.name + " path " + UnityEditor.AssetDatabase.GetAssetPath(m) + " sub " + m.subMeshCount + " col " + (pw.GetComponent<Collider>() != null) + "\n");
for (int s = 0; s < m.subMeshCount; s++)
{
    var T = m.GetTriangles(s); int n = 0; float lo = 99, hi = -99, dmin = 99, dmax = -99; Vector3 c0 = default;
    for (int i = 0; i < T.Length; i += 3)
    {
        var c = tr.TransformPoint((V[T[i]] + V[T[i + 1]] + V[T[i + 2]]) / 3f);
        if (Mathf.Abs(c.x - 279f) > 4f || Mathf.Abs(c.z + 25f) > 4f) continue;
        n++; if (n == 1) c0 = c; lo = Mathf.Min(lo, c.y); hi = Mathf.Max(hi, c.y);
        if (terr.Raycast(new Ray(new Vector3(c.x, 80, c.z), Vector3.down), out var h, 120)) { dmin = Mathf.Min(dmin, c.y - h.point.y); dmax = Mathf.Max(dmax, c.y - h.point.y); }
    }
    sb.Append($"sub{s} {pw.GetComponent<Renderer>().sharedMaterials[Mathf.Min(s, pw.GetComponent<Renderer>().sharedMaterials.Length - 1)].name}: 삼각형 {n} y {lo:F2}~{hi:F2} 지형보다 {dmin:F2}~{dmax:F2} 첫 {c0}\n");
}
return sb.ToString();
