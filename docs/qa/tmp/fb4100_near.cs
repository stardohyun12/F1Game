var c = new Vector3(258f, 0, -58f); var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
{
    if (!r.gameObject.activeInHierarchy) continue; var n = r.name; if (!n.StartsWith("ROAD")) continue;
    var b = r.bounds; var cp = b.ClosestPoint(new Vector3(c.x, b.center.y, c.z)); float d = new Vector2(cp.x - c.x, cp.z - c.z).magnitude; if (d > 25) continue;
    sb.AppendLine(n + " | " + r.sharedMaterial.name + " | d=" + d.ToString("F1") + " | " + b.min.ToString("F1") + " " + b.max.ToString("F1") + " | " + r.transform.parent.name);
}
return sb.ToString();
