var sb = new System.Text.StringBuilder();
var c = Camera.main.transform;
sb.Append("cam " + c.position.ToString("F2") + " fwd " + c.forward.ToString("F2") + "\n");
foreach (var h in Physics.RaycastAll(c.position, c.forward, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
    sb.Append("  hit " + h.collider.name + " d=" + h.distance.ToString("F2") + " par=" + (h.collider.transform.parent ? h.collider.transform.parent.name : "-") + "\n");
var ps = GameObject.Find("OnFoot_Depot/PackSpot");
var rig = ps.transform.Find("PackBench");
var spot = rig.Find("BenchSpot");
sb.Append("spot " + spot.position.ToString("F2") + "\n");
// counter-top props (renderers) in bench area, not ours
var center = rig.TransformPoint(new Vector3(0f, 1.05f, 0.65f));
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    if (r.transform.IsChildOf(ps.transform)) continue;
    var b = r.bounds;
    if (Mathf.Abs(b.center.x - center.x) < 0.6f && Mathf.Abs(b.center.z - center.z) < 0.9f && b.center.y > 32.1f && b.center.y < 32.9f)
        sb.Append("  prop " + r.name + " par=" + (r.transform.parent ? r.transform.parent.name : "-") + " c=" + b.center.ToString("F2") + " ext=" + b.extents.ToString("F2") + " col=" + (r.GetComponent<Collider>() != null) + "\n");
}
foreach (var col in Physics.OverlapBox(center, new Vector3(0.5f, 0.15f, 0.8f), rig.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
    if (!col.transform.IsChildOf(ps.transform)) sb.Append("  col " + col.name + " b=" + col.bounds.center.ToString("F2") + " ext=" + col.bounds.extents.ToString("F2") + "\n");
return sb.ToString();
