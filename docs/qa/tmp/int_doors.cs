var list = A7ShadeGlobals.FindInteriors(null);
var doors = UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
var sb = new System.Text.StringBuilder();
sb.AppendLine("doors=" + doors.Length);
foreach (var it in list)
{
    if (!it.ok || !it.name.StartsWith("house")) continue;
    sb.AppendLine("## " + it.name + " half=" + it.half.ToString("F2"));
    foreach (var d in doors)
    {
        var c = Gaze.CenterOf(d).Value;
        var q = it.box.MultiplyPoint3x4(c);
        Vector3 cc = d.transform.parent != null ? d.transform.parent.TransformPoint(d.centerLocal) : d.centerLocal;
        var qc = it.box.MultiplyPoint3x4(cc);
        if (Mathf.Abs(q.y) > 1.6f || Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.z)) > 1.6f) continue;
        sb.AppendLine("  door " + d.name + " cfg=" + d.configured + " open=" + d.IsOpen + " qRend=" + q.ToString("F2") + " qClosed=" + qc.ToString("F2"));
    }
}
return sb.ToString();
