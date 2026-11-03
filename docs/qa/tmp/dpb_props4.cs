var ctr = GameObject.Find("D198_depot/q3_depot/COUNTER_01");
var mf = ctr.GetComponent<MeshFilter>();
var tmp = new GameObject("tmp_probe");
tmp.transform.SetPositionAndRotation(ctr.transform.position, ctr.transform.rotation); tmp.transform.localScale = ctr.transform.lossyScale;
tmp.layer = 31;
var mc = tmp.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
Physics.SyncTransforms();
var sb = new System.Text.StringBuilder("rows z (-28.10..-30.90 step .05), cols x 273.90..274.85 step .05; '.'=31.97 top, digit=cm*2 above(0-9 => 5cm units), ' '=none\n");
for (float z = -28.10f; z >= -30.91f; z -= 0.05f)
{
    sb.Append(z.ToString("F2") + " ");
    for (float x = 273.90f; x <= 274.851f; x += 0.05f)
    {
        RaycastHit h;
        if (Physics.Raycast(new Vector3(x, 33.0f, z), Vector3.down, out h, 3f, 1 << 31))
        { float d = h.point.y - 31.97f; sb.Append(d < 0.01f ? "." : Mathf.Min(9, Mathf.CeilToInt(d / 0.05f)).ToString()); }
        else sb.Append(" ");
    }
    sb.Append("\n");
}
UnityEngine.Object.Destroy(tmp);
return sb.ToString();
