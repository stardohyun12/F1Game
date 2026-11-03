var garage = GameObject.Find("GARAGE_shell").GetComponent<Renderer>();
string outS = "";
var spots = new Vector3[] { new Vector3(184.1f, 0, -66.5f), new Vector3(184.5f, 0, -67.5f), new Vector3(185f, 0, -68.5f), new Vector3(185.5f, 0, -70f), new Vector3(187f, 0, -68f) };
foreach (var pz in spots)
{
    RaycastHit g; Physics.Raycast(new Vector3(pz.x, 60, pz.z), Vector3.down, out g, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    Vector3 eye = g.point + Vector3.up * 1.6f; Bounds b = garage.bounds; string m = "";
    for (int i = 0; i < 9; i++)
    {
        Vector3 p = new Vector3(Mathf.Lerp(b.min.x + 1f, b.max.x - 1f, i / 8f), b.max.y - 1f, b.center.z); Vector3 v = p - eye; RaycastHit h;
        bool vis = !(Physics.Raycast(eye, v.normalized, out h, v.magnitude + 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && h.collider.transform != garage.transform && h.distance < v.magnitude - 1.5f);
        m += vis ? "o" : "x";
    }
    outS += pz.x + "," + pz.z + " " + m + " | ";
}
return outS;
