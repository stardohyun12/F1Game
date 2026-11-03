var b = PitCrew.PackBench.PackBench.I;
var box = b.BenchBox;
var tray = b.Tray();
var sb = new System.Text.StringBuilder();
var bc = tray.GetComponent<UnityEngine.BoxCollider>();
var t = tray.transform;
float trayBottom = box.transform.InverseTransformPoint(t.TransformPoint(bc.center) - t.up * 0.0325f).y;
sb.Append("trayBottomLocal=" + trayBottom.ToString("F4") + " innerBottom=" + box.InnerBottomY.ToString("F4") + "\n");
foreach (var it in box.Contents())
{
    var col = it.GetComponent<UnityEngine.Collider>();
    sb.Append(it.name + " " + it.kind + " pos=" + box.transform.InverseTransformPoint(it.transform.position).ToString("F3") + " minY=" + box.transform.InverseTransformPoint(col.bounds.min).y.ToString("F4") + " maxY=" + box.transform.InverseTransformPoint(col.bounds.max).y.ToString("F4") + " sleep=" + it.Body.IsSleeping() + "\n");
}
var o = t.TransformPoint(bc.center) - t.up * (0.0325f - 0.003f);
foreach (var h in UnityEngine.Physics.RaycastAll(o, -t.up, 0.05f, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
    sb.Append("ray -Y hit " + h.collider.name + " d=" + h.distance.ToString("F4") + "\n");
float bf; var fr = box.MeasureSupport(tray, out bf);
sb.Append("bare=" + bf + " faces=" + string.Join(",", System.Array.ConvertAll(fr, f => f.ToString("F2"))));
return sb.ToString();
