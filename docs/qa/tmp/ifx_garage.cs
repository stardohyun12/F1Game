// 정비소 자식: 렌더러·콜라이더·셰이더·상자 좌표(가운데·크기)
var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var it = list.Find(i => i.name.StartsWith(PlayerPrefs.GetString("ifxb", "D198_garage")));
var sb = new System.Text.StringBuilder();
sb.AppendLine(it.name + " bld rot=" + it.building.eulerAngles.ToString("F0") + " scale=" + it.building.lossyScale.ToString("F2"));
foreach (var t in it.building.GetComponentsInChildren<Transform>(true))
{
    var r = t.GetComponent<Renderer>(); var c = t.GetComponent<Collider>();
    if (r == null && c == null) continue;
    var b = r != null ? r.bounds : c.bounds;
    var q = it.box.MultiplyPoint3x4(b.center);
    string sh = r != null && r.sharedMaterial != null ? r.sharedMaterial.shader.name.Replace("PitCrew/", "") + "/" + r.sharedMaterial.name : "-";
    string col = c == null ? "-" : c.GetType().Name + (c is MeshCollider mc ? "(convex=" + mc.convex + ")" : "") + (c.enabled ? "" : "(off)");
    sb.AppendLine(t.name + (t.gameObject.activeInHierarchy ? "" : "[inactive]") + " q=" + q.ToString("F2") + " size=" + b.size.ToString("F1") + " sh=" + sh + " col=" + col);
}
return sb.ToString();
