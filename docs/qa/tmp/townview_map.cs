var root = GameObject.Find("TownV6_Gray").transform;
var terr = root.Find("TownV6_Terrain").GetComponent<Collider>();
var wallsT = root.Find("Buildings/Walls_0");
var vs = wallsT.GetComponent<MeshFilter>().sharedMesh.vertices; var m = wallsT.localToWorldMatrix;
int N = 36; float x0 = -400f, z0 = -400f, cell = 30f;
var cnt = new int[N, N];
for (int i = 0; i < vs.Length; i++)
{
    var w = m.MultiplyPoint3x4(vs[i]);
    int a = (int)((w.x - x0) / cell), b = (int)((w.z - z0) / cell);
    if (a >= 0 && a < N && b >= 0 && b < N) cnt[a, b]++;
}
var sb = new System.Text.StringBuilder();
sb.Append("rows = z from high(top) to low; cols = x from -400, cell 30. digit=building verts/50 (cap 9) ; ~ = sea(y<1) ; . = land\n");
for (int b = N - 1; b >= 0; b--)
{
    sb.Append(((int)(z0 + b * cell)).ToString().PadLeft(5)).Append(' ');
    for (int a = 0; a < N; a++)
    {
        RaycastHit h; float gx = x0 + a * cell + cell / 2, gz = z0 + b * cell + cell / 2;
        float y = -99f; if (terr.Raycast(new Ray(new Vector3(gx, 500f, gz), Vector3.down), out h, 1000f)) y = h.point.y;
        int c = cnt[a, b] / 50;
        sb.Append(c > 0 ? (c > 9 ? '9' : (char)('0' + c)) : (y < 1f ? '~' : '.'));
    }
    sb.Append('\n');
}
foreach (var n in new[] { "D198_room", "D198_garage", "D198_depot", "D198_postoffice", "house_home" })
{
    Transform t = null; foreach (var tt in root.Find("D198_Buildings").GetComponentsInChildren<Transform>()) if (tt.name == n) { t = tt; break; }
    if (t != null) { var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length > 0) { var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); sb.Append(n + " " + bb.center.ToString("F0") + " size " + bb.size.ToString("F0") + "\n"); } }
}
return sb.ToString();
