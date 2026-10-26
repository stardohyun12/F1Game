// 2306: D_slope 근처 길 경로(10점마다) + ToneSlice 위치
var sb = new System.Text.StringBuilder();
foreach (var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
{
    if (rs.centerline == null) continue;
    var line = new System.Text.StringBuilder(); bool any = false;
    for (int i = 0; i < rs.centerline.Length; i += (rs.roadId == "main" ? 8 : 6))
    {
        var p = rs.centerline[i];
        if (p.x < -110 || p.x > 70 || p.z < 110 || p.z > 270) continue;
        any = true; line.Append(i + ":(" + p.x.ToString("F0") + "," + p.z.ToString("F0") + "," + p.y.ToString("F0") + ") ");
    }
    if (any) sb.AppendLine(rs.roadId + " w=" + rs.width + " closed=" + rs.closed + " :: " + line);
}
var ts = GameObject.Find("BayLoop").transform.Find("ToneSlice_ReturnRoad");
if (ts) foreach (Transform t in ts) if (t.childCount > 0 || t.GetComponent<Renderer>()) { var p = t.position; if (p.x > -110 && p.x < 70 && p.z > 110 && p.z < 270) sb.AppendLine("TS " + t.name + " " + p.ToString("F0")); }
return sb.ToString();
