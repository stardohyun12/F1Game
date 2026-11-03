// (cx,cz) 주변 격자로 수직 RaycastAll → 맞은 물체 이름·높이. 에디트 모드 읽기만.
float cx = 86.16f, cz = -192.19f, R = 3f, st = .5f;
Physics.SyncTransforms();
var sb = new System.Text.StringBuilder();
for (float z = cz + R; z >= cz - R - 1e-3f; z -= st)
{
    for (float x = cx - R; x <= cx + R + 1e-3f; x += st)
    {
        var hs = Physics.RaycastAll(new Vector3(x, 30, z), Vector3.down, 60);
        System.Array.Sort(hs, (a, b) => b.point.y.CompareTo(a.point.y));
        string s = ""; int k = 0;
        foreach (var h in hs) { if (k++ >= 3) break; string nm = h.collider.name.Replace("ROAD_", "").Replace("TownV6_", ""); s += (nm.Length > 14 ? nm.Substring(0, 14) : nm) + ":" + h.point.y.ToString("F3") + " "; }
        sb.AppendLine($"({x:F1},{z:F1}) {s}");
    }
}
return sb.ToString();
