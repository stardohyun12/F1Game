var ps = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>(FindObjectsInactive.Include);
var sb = new System.Text.StringBuilder();
foreach (var t in ps.GetComponentsInChildren<Transform>(true))
{
    string p = t.name; var q = t.parent;
    while (q != null && q != ps.transform) { p = q.name + "/" + p; q = q.parent; }
    if (p.Split('/').Length <= 3) sb.Append(p).Append(t.GetComponent<Renderer>() != null ? " R" : "").Append("\n");
}
sb.Append("bench=" + (ps.Bench != null) + " eye=" + (ps.Eye != null) + " spot=" + (ps.Bench != null && ps.Bench.benchSpot != null));
return sb.ToString();
