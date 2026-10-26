// 1514: O_breakwater 입구 가까이(톱니 확인) + 그 자리 레이 단면
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/coast-v1/find"));
var go = new GameObject("c1514cam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.3f; cam.farClipPlane = 1500f;
var mc = Camera.main; if (mc != null) { cam.clearFlags = mc.clearFlags; cam.backgroundColor = mc.backgroundColor; }
var s = new Vector3(-0.7f, 0, -0.7f).normalized; var p = new Vector3(-96, 3, -111) + s * 7 + new Vector3(s.z, 0, -s.x) * 14; var cp = p + s * 16 + Vector3.up * 9;
cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(p - cp)); cam.fieldOfView = 55;
var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "zoom_bw.png"), tex.EncodeToPNG());
foreach (var (tagN, pre) in new[] { ("noFar", "FarTerrain"), ("noBay", "BayTerrain") })
{
    var hid = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith(pre)).ToList();
    foreach (var r in hid) r.enabled = false;
    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "zoom_bw_" + tagN + ".png"), tex.EncodeToPNG());
    foreach (var r in hid) r.enabled = true;
}
var sb = new System.Text.StringBuilder();
for (int k = 0; k < 9; k++)
{
    var q = p + s * (k * 1.5f);
    var rends = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.bounds.Contains(new Vector3(q.x, r.bounds.center.y, q.z))).Select(r => r.name).Take(12);
    var hits = Physics.RaycastAll(q + Vector3.up * 30, Vector3.down, 60f).OrderBy(h => h.distance);
    sb.Append((k * 1.5f) + "m H" + BuildBayLoop.H(q.x, q.z).ToString("F2") + " hits " + string.Join(",", hits.Select(h => h.collider.name + "@" + h.point.y.ToString("F2"))) + " | " + string.Join(",", rends) + "\n");
}
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();
