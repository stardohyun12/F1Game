var sw = System.Diagnostics.Stopwatch.StartNew();
var list = A7ShadeGlobals.FindInteriors(null);
var sb = new System.Text.StringBuilder();
sb.AppendLine("ms=" + sw.ElapsedMilliseconds + " n=" + list.Count);
foreach (var it in list)
{
    var c = it.ok ? it.box.inverse.MultiplyPoint3x4(Vector3.zero) : it.seed;
    sb.AppendLine(it.name + " ok=" + it.ok + " why=" + it.why + " center=" + c.ToString("F1") + " half=" + it.half.ToString("F2") + " pts=" + it.samples + " seed=" + it.seed.ToString("F1") + " cut=" + RoomDiorama.CanCut(it.building));
}
return sb.ToString();
