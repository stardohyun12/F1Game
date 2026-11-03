var sb = new System.Text.StringBuilder();
for (float z = -226f; z <= -190f; z += 1f)
{
    sb.Append("z" + z.ToString("0") + ": ");
    for (float x = 262f; x <= 283f; x += 1f)
    {
        var hs = Physics.RaycastAll(new Vector3(x, 6f, z), Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
        float top = -99; string par = "";
        foreach (var h in hs) if (h.point.y > top) { top = h.point.y; par = h.collider.transform.parent != null ? h.collider.transform.parent.name : h.collider.name; }
        sb.Append(top < -50 ? "." : (top > 0.5f ? (par == "Piers" ? "P" : "#") : "~"));
    }
    sb.Append("\n");
}
return sb.ToString();
