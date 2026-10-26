var road = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "P01_switchback");
var s = "";
for (int i = 0; i <= 16; i += 2)
{
    var p = road.centerline[i]; s += i + ":(" + p.x.ToString("F1") + "," + p.y.ToString("F2") + "," + p.z.ToString("F1") + ")";
    foreach (var h in Physics.RaycastAll(p + Vector3.up * 3, Vector3.down, 6).OrderBy(h => h.distance)) s += " " + h.collider.name.Substring(0, Mathf.Min(10, h.collider.name.Length)) + "=" + h.point.y.ToString("F2");
    s += " | ";
}
return s;
