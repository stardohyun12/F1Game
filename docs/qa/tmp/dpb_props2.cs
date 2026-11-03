var bld = GameObject.Find("Q3_building");
var mf = bld.GetComponent<MeshFilter>();
var tmp = new GameObject("tmp_probe");
tmp.transform.SetPositionAndRotation(bld.transform.position, bld.transform.rotation); tmp.transform.localScale = bld.transform.lossyScale;
tmp.layer = 31;
var mc = tmp.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
Physics.SyncTransforms();
var sb = new System.Text.StringBuilder("grid: z rows, x cols 273.90..274.85 step .05 ; '.'=flat counter top, digit=height above top in 5cm units\n");
float top = 32.205f;
for (float z = -28.15f; z >= -30.85f; z -= 0.05f)
{
    sb.Append(z.ToString("F2") + " ");
    for (float x = 273.90f; x <= 274.851f; x += 0.05f)
    {
        RaycastHit h;
        if (Physics.Raycast(new Vector3(x, 34f, z), Vector3.down, out h, 3f, 1 << 31))
        {
            float d = h.point.y - top;
            sb.Append(d < 0.01f ? (d < -0.05f ? "_" : ".") : Mathf.Min(9, Mathf.CeilToInt(d / 0.05f)).ToString());
        }
        else sb.Append(" ");
    }
    sb.Append("\n");
}
UnityEngine.Object.Destroy(tmp);
return sb.ToString();
