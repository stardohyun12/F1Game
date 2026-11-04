var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var sb = new System.Text.StringBuilder();
foreach (var it in list)
{
    if (!it.name.StartsWith("D198_depot")) continue;
    sb.AppendLine(it.name + " half=" + it.half.ToString("F2"));
    foreach (var r in it.building.GetComponentsInChildren<Renderer>())
        if (r.name.IndexOf("door", System.StringComparison.OrdinalIgnoreCase) >= 0 || r.name.IndexOf("glass", System.StringComparison.OrdinalIgnoreCase) >= 0)
            sb.AppendLine("  " + r.name + " w=" + r.bounds.center.ToString("F1") + " box=" + it.box.MultiplyPoint3x4(r.bounds.center).ToString("F2"));
}
return sb.ToString();
