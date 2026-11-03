var b = PitCrew.PackBench.PackBench.I;
var box = b.BenchBox;
var sb = new System.Text.StringBuilder();
var bt = box.transform;
sb.Append("closed=" + box.IsClosed + " RimY=" + box.RimY.ToString("F3") + "\n");
var lid = bt.Find("Shell/LidPivot/Lid");
if (lid != null) { var lc = lid.GetComponent<UnityEngine.Collider>(); sb.Append("lidCol enabled=" + lc.enabled + " minY=" + bt.InverseTransformPoint(lc.bounds.min).y.ToString("F3") + " maxY=" + bt.InverseTransformPoint(lc.bounds.max).y.ToString("F3") + "\n"); }
foreach (var it in box.Contents())
{
    if (it.kind == PitCrew.PackBench.PackItemKind.Eggs) continue;
    var col = it.GetComponent<UnityEngine.Collider>();
    var c = col.bounds.center; float h = col.bounds.extents.y;
    sb.Append(it.name + " cy=" + bt.InverseTransformPoint(c).y.ToString("F3") + " h=" + h.ToString("F3") + " layer=" + it.gameObject.layer + " :");
    foreach (var d in new[] { bt.up, -bt.up })
    {
        sb.Append(d.y > 0 ? " UP[" : " DN[");
        foreach (var hit in UnityEngine.Physics.RaycastAll(c, d, h + 0.012f, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
            sb.Append(hit.collider.name + "@" + hit.distance.ToString("F3") + " ");
        sb.Append("]");
    }
    sb.Append("\n");
}
return sb.ToString();
