var sb = new System.Text.StringBuilder();
foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
{
    var rs = g.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { sb.Append(g.name + " (no r) " + g.transform.position + "\n"); continue; }
    Bounds b = new Bounds(); bool f = false; foreach (var r in rs) { if (r.name == "Sea") continue; if (!f) { b = r.bounds; f = true; } else b.Encapsulate(r.bounds); }
    sb.Append(g.name + " n=" + rs.Length + " " + b.min.ToString("F0") + "~" + b.max.ToString("F0") + " act=" + g.activeSelf + "\n");
}
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{ var n = t.name.ToLower(); if (n.Contains("harbor") || n.Contains("harbour") || n.Contains("port") && !n.Contains("support") || n.Contains("pier") || n.Contains("quay") || n.Contains("dock")) sb.Append("H " + t.name + " " + t.position.ToString("F0") + " par=" + (t.parent ? t.parent.name : "") + "\n"); }
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); sb.Append("car=" + (car ? car.transform.position.ToString("F1") + " " + car.transform.eulerAngles.ToString("F0") : "-"));
return sb.ToString();
