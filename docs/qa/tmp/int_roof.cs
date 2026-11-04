var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var it = list.Find(i => i.name == "D198_garage/1F#0");
var sb = new System.Text.StringBuilder();
sb.AppendLine("building=" + it.building.name);
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    var q = it.box.MultiplyPoint3x4(r.bounds.center);
    if (Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.z)) > 1.3f || q.y < 0.3f || q.y > 4f) continue;
    var p = r.transform.parent;
    sb.AppendLine("  " + r.name + " parent=" + (p != null ? p.name : "-") + " inB=" + r.transform.IsChildOf(it.building) + " q=" + q.ToString("F2") + " sh=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-"));
}
return sb.ToString();
