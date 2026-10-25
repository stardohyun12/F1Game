var o = new System.Text.StringBuilder();
foreach (var n in new[] { "BLD_P194_detached_house", "Plinth_P194" })
{
    var g = GameObject.Find(n); if (g == null) { o.Append(n + " none | "); continue; }
    foreach (var col in g.GetComponentsInChildren<Collider>()) { var b = col.bounds; o.Append(n + " " + col.GetType().Name + " min" + b.min.ToString("F1") + " max" + b.max.ToString("F1") + " | "); }
}
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(r => r.roadId == "SC_oldroad" && r.centerline != null && r.centerline.Length > 0);
foreach (var P in rs.centerline) if (P.x > -88 && P.x < -72 && P.z > 92 && P.z < 106) o.Append(P.ToString("F1") + " ");
return o.ToString();
