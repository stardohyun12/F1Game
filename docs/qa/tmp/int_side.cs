var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var sb = new System.Text.StringBuilder();
foreach (var it in list)
{
    if (it.room != null || it.name.StartsWith("house")) continue;
    var inv = it.box.inverse;
    Vector3 ax = inv.MultiplyVector(Vector3.right).normalized, az = inv.MultiplyVector(Vector3.forward).normalized;
    Vector3 o = inv.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up;
    sb.AppendLine(it.name + " o=" + o.ToString("F1"));
    for (int s = 0; s < 4; s++)
    {
        Vector3 dir = s == 0 ? ax : s == 1 ? -ax : s == 2 ? az : -az, lat = s < 2 ? az : ax;
        float reach = (s < 2 ? it.half.x : it.half.z) + 1f, span = s < 2 ? it.half.z : it.half.x;
        var line = "  side" + s + " dir=" + dir.ToString("F1") + ":";
        for (int k = -3; k <= 3; k++)
        {
            var hs = Physics.RaycastAll(o + lat * (span * 0.85f * k / 3f), dir, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            line += " [";
            foreach (var h in hs) line += h.transform.name + "@" + h.distance.ToString("F1") + ",";
            line += "]";
        }
        sb.AppendLine(line);
    }
}
return sb.ToString();
