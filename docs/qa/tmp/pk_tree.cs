var ps = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>(FindObjectsInactive.Include);
var sb = new System.Text.StringBuilder();
System.Action<Transform, int> walk = null;
walk = (t, d) =>
{
    if (d > 4) return;
    var r = t.GetComponent<Renderer>(); var c = t.GetComponent<Collider>();
    sb.Append(new string(' ', d * 2)).Append(t.name).Append(" ").Append(t.position.ToString("F2")).Append(" s").Append(t.lossyScale.ToString("F2"));
    if (r != null) sb.Append(" R:").Append(r.bounds.size.ToString("F2"));
    if (c != null) sb.Append(" C");
    foreach (var m in t.GetComponents<MonoBehaviour>()) sb.Append(" [").Append(m.GetType().Name).Append("]");
    sb.Append(t.gameObject.activeSelf ? "" : " OFF").Append("\n");
    foreach (Transform ch in t) walk(ch, d + 1);
};
walk(ps.transform, 0);
return sb.ToString();
