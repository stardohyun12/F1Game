var o = "";
var house = GameObject.Find("house_home");
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{
    var n = t.name.ToLower();
    if (n.Contains("bed") || n.Contains("calendar") || n.Contains("desk") || n == "house_home" || n.Contains("pillow") || n.Contains("room"))
        o += t.name + "@" + t.position.ToString("F2") + " par=" + (t.parent ? t.parent.name : "-") + "\n";
}
if (house != null)
{
    o += "HOUSE pos=" + house.transform.position.ToString("F2") + " yaw=" + house.transform.eulerAngles.y + "\n";
    foreach (Transform c in house.transform)
    {
        var r = c.GetComponent<Renderer>();
        o += "  " + c.name + " " + (r != null ? r.bounds.ToString() : "") + " kids=" + c.childCount + "\n";
    }
}
return o;
