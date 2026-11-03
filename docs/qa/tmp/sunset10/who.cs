var cam = Camera.main; var ray = cam.ViewportPointToRay(new Vector3(__X__f, __Y__f, 0));
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy || !r.isVisible) continue;
    float d; if (!r.bounds.IntersectRay(ray, out d)) continue;
    var m = r.sharedMaterial; var sh = m && m.shader ? m.shader.name : "-";
    sb.Append(d.ToString("F0") + "m " + r.name + " / " + (r.transform.parent ? r.transform.parent.name : "") + " | " + sh + " | " + (m ? m.name : "-") + "\n");
}
return sb.ToString();
