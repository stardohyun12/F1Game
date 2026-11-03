var sb = new System.Text.StringBuilder();
foreach (var n in new[] { "BenchTop", "COUNTER_01" })
{
    foreach (var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Collider>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    {
        if (c.name != n) continue;
        var path = c.name; var tr = c.transform.parent; while (tr != null) { path = tr.name + "/" + path; tr = tr.parent; }
        sb.Append(path + " act=" + c.gameObject.activeInHierarchy + " en=" + c.enabled + " b=" + c.bounds.min.ToString("F2") + "~" + c.bounds.max.ToString("F2") + "\n");
    }
}
var spot = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>();
sb.Append("bench=" + (spot.Bench != null ? spot.Bench.name + " act=" + spot.Bench.gameObject.activeInHierarchy : "null"));
return sb.ToString();
