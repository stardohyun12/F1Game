var sb = new System.Text.StringBuilder();
var ps = GameObject.Find("OnFoot_Depot/PackSpot");
var region = new Bounds(new Vector3(274.38f, 31.9f, -29.5f), new Vector3(0.6f, 1.2f, 1.0f));
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    if (r.transform.IsChildOf(ps.transform)) continue;
    if (!r.bounds.Intersects(region)) continue;
    if (r.bounds.extents.x > 50f) continue;
    var t = r.transform; string path = t.name; while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
    sb.Append(path + " max.y=" + r.bounds.max.y.ToString("F3") + " c=" + r.bounds.center.ToString("F2") + " ext=" + r.bounds.extents.ToString("F2") + "\n");
}
return sb.ToString();
