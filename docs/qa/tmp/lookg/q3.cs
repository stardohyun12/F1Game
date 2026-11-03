var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
{
    var n = r.name.ToLower(); if (!(n.Contains("road") || n.Contains("street") || n.Contains("asphalt"))) continue;
    var s = r.bounds.size; if (Mathf.Max(s.x, s.z) < 120) continue;
    sb.Append(r.name + " par=" + (r.transform.parent ? r.transform.parent.name : "") + " " + r.bounds.min.ToString("F0") + "~" + r.bounds.max.ToString("F0") + " mat=" + r.sharedMaterial.name + "\n");
}
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); sb.Append("follow=" + (fc ? fc.name : "-"));
return sb.ToString();
