var sb = new System.Text.StringBuilder();
float topY = 32.215f;
var cmin = new Vector3(273.85f, 0f, -30.85f); var cmax = new Vector3(274.9f, 0f, -28.1f);
foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
{
    var r = mf.GetComponent<Renderer>(); if (r == null || mf.sharedMesh == null) continue;
    var b = r.bounds;
    if (b.max.x < cmin.x || b.min.x > cmax.x || b.max.z < cmin.z || b.min.z > cmax.z || b.max.y < topY || b.min.y > 33.5f) continue;
    if (mf.transform.root.name == "OnFoot_HomeGarage") continue;
    if (!mf.sharedMesh.isReadable) { sb.Append(mf.name + " (not readable) b=" + b.center.ToString("F2") + " ext=" + b.extents.ToString("F2") + "\n"); continue; }
    var v = mf.sharedMesh.vertices; var m = mf.transform.localToWorldMatrix;
    // bucket by 0.1 m along z: max height above counter top, x range
    var hi = new System.Collections.Generic.SortedDictionary<int, Vector3>();
    foreach (var lv in v)
    {
        var w = m.MultiplyPoint3x4(lv);
        if (w.y < topY + 0.01f || w.y > 33.5f || w.x < cmin.x || w.x > cmax.x || w.z < cmin.z || w.z > cmax.z) continue;
        int k = Mathf.FloorToInt(w.z * 10f);
        Vector3 e; if (!hi.TryGetValue(k, out e)) e = new Vector3(99f, -1f, -99f);
        hi[k] = new Vector3(Mathf.Min(e.x, w.x), Mathf.Max(e.y, w.y - topY), Mathf.Max(e.z, w.x));
    }
    if (hi.Count == 0) continue;
    sb.Append(mf.name + " par=" + (mf.transform.parent ? mf.transform.parent.name : "-") + "\n");
    foreach (var kv in hi) sb.Append("  z " + (kv.Key / 10f).ToString("F1") + " x " + kv.Value.x.ToString("F2") + "~" + kv.Value.z.ToString("F2") + " h " + kv.Value.y.ToString("F2") + "\n");
}
return sb.ToString();
