var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var sb = new System.Text.StringBuilder();
var seen = new System.Collections.Generic.HashSet<Texture>();
foreach (var it in list)
{
    if (!(it.name.StartsWith("D198_depot") || it.name.StartsWith("D198_postoffice/1F") || it.name.StartsWith("house_home/ROOM_2F"))) continue;
    sb.Append("# " + it.name + " bld=" + it.building.name + "\n");
    foreach (var r in it.building.GetComponentsInChildren<Renderer>())
    {
        var q = it.box.MultiplyPoint3x4(r.bounds.center);
        if (Mathf.Max(Mathf.Abs(q.x), Mathf.Max(Mathf.Abs(q.y), Mathf.Abs(q.z))) > 1.2f) continue;
        foreach (var m in r.sharedMaterials)
        {
            if (m == null || !m.HasProperty("_BaseMap")) continue;
            var t = m.GetTexture("_BaseMap");
            if (t == null || !seen.Add(t)) continue;
            sb.Append(r.name + " mat=" + m.name + " sh=" + m.shader.name + " tex=" + t.name + " " + t.width + "x" + t.height + " filt=" + t.filterMode + " mips=" + t.mipmapCount + " bias=" + t.mipMapBias + " ST=" + m.GetVector("_BaseMap_ST") + " SN=" + (m.HasProperty("_SN") ? m.GetFloat("_SN") : -1) + " blotch=" + (m.HasProperty("_Blotch") ? m.GetFloat("_Blotch") : -1) + "\n");
        }
    }
}
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
sb.Append("levels=" + (g != null ? g.indoorLevels.ToString() : "-") + " ditherW=" + (g != null ? g.indoorDitherWidth.ToString() : "-"));
return sb.ToString();
