// 2306: D_slope 구역 조사 — 높이 격자, 지나는 길, 안에 있는 오브젝트(상위 부모별 개수)
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (s.name != "Map_BayLoop") UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_BayLoop.unity");
var poly = new Vector2[] { new Vector2(-92,158), new Vector2(-70,124), new Vector2(-30,124), new Vector2(-24,160), new Vector2(42,196), new Vector2(52,232), new Vector2(0,252), new Vector2(-62,252), new Vector2(-96,236) };
System.Func<float,float,bool> In = (x,z) => { bool c=false; for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){ if(((poly[i].y>z)!=(poly[j].y>z)) && (x < (poly[j].x-poly[i].x)*(z-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x)) c=!c;} return c; };
var sb = new System.Text.StringBuilder();
var bl = GameObject.Find("BayLoop").transform;
int ground = 0; foreach (var r in bl.GetComponentsInChildren<MeshCollider>()) { }
sb.AppendLine("Heights (row z desc, col x -100..60 step 10), '.'=outside:");
for (int z = 260; z >= 120; z -= 10)
{
    var line = new System.Text.StringBuilder(z.ToString().PadLeft(4) + ": ");
    for (int x = -100; x <= 60; x += 10)
    {
        float h = float.NaN;
        var hits = Physics.RaycastAll(new Vector3(x, 400, z), Vector3.down, 800);
        foreach (var hh in hits) if (hh.collider.name.StartsWith("OG_") || hh.collider.transform.name.Contains("Terrain")) { if (float.IsNaN(h) || hh.point.y > h) h = hh.point.y; }
        line.Append((In(x,z) ? "" : "(") + (float.IsNaN(h) ? "  ?" : h.ToString("F0").PadLeft(3)) + (In(x,z) ? "  " : ") "));
    }
    sb.AppendLine(line.ToString());
}
foreach (var rs in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
{
    if (rs.centerline == null) continue; int n = 0; float lo = 1e9f, hi = -1e9f;
    foreach (var p in rs.centerline) if (In(p.x, p.z)) { n++; lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y); }
    if (n > 0) sb.AppendLine("ROAD " + rs.roadId + " " + rs.name + " pts=" + n + "/" + rs.centerline.Length + " y=" + lo.ToString("F0") + ".." + hi.ToString("F0") + " w=" + rs.width);
}
var cnt = new System.Collections.Generic.Dictionary<string,int>();
foreach (Transform top in bl) foreach (Transform t in top)
{
    var p = t.position; if (!In(p.x, p.z)) continue;
    string k = top.name; cnt[k] = cnt.TryGetValue(k, out var v) ? v + 1 : 1;
}
foreach (var kv in cnt) sb.AppendLine("OBJ " + kv.Key + " x" + kv.Value);
sb.AppendLine("colliders sample at (-30,200): " + string.Join(",", Physics.RaycastAll(new Vector3(-30,400,200), Vector3.down, 800).Select(h => h.collider.name + "@" + h.point.y.ToString("F0"))));
return sb.ToString();
