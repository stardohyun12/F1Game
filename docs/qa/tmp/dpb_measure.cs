var sb = new System.Text.StringBuilder();
var ps = GameObject.Find("OnFoot_Depot/PackSpot");
sb.Append("PackSpot " + ps.transform.position.ToString("F2") + " rot " + ps.transform.eulerAngles.ToString("F1") + "\n");
foreach (var c in Physics.OverlapBox(new Vector3(274.4f, 31.8f, -29.5f), new Vector3(2.5f, 1.2f, 2.5f), Quaternion.identity))
{
    if (c.transform.IsChildOf(ps.transform)) continue;
    sb.Append(c.name + " " + c.GetType().Name + " b=" + c.bounds.center.ToString("F2") + " ext=" + c.bounds.extents.ToString("F2") + " rotY=" + c.transform.eulerAngles.y.ToString("F0") + "\n");
}
return sb.ToString();
