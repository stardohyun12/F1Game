// 1514: 도현 2.png 자리 찾기 — V_pier·O_breakwater 시작점 근처 본선을 바다 쪽 위에서 찍는다(후보 캡처)
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/coast-v1/find"));
System.IO.Directory.CreateDirectory(outDir);
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 5).ToList();
var go = new GameObject("c1514cam"); var cam = go.AddComponent<Camera>(); cam.farClipPlane = 1500f;
var mc = Camera.main; if (mc != null) { cam.clearFlags = mc.clearFlags; cam.backgroundColor = mc.backgroundColor; }
var log = new System.Text.StringBuilder();
System.Action<string, Vector3, Vector3> Shot = (name, cp, look) =>
{
    cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = 55;
    var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); rt.Release();
};
foreach (var id in new[] { "V_pier", "O_breakwater", "O_coast", "A_cliffwalk" })
{
    var r = rs.FirstOrDefault(q => q.roadId == id); if (r == null) continue;
    var p = r.centerline[0]; var seaDir = Vector3.zero;
    // 바다 쪽 = 반경 40m 에서 땅이 가장 낮은 방향
    float lo = 999; for (int a = 0; a < 16; a++) { var d = Quaternion.Euler(0, a * 22.5f, 0) * Vector3.forward; float h = BuildBayLoop.H(p.x + d.x * 40, p.z + d.z * 40); if (h < lo) { lo = h; seaDir = d; } }
    Shot("find_" + id, p + seaDir * 45 + Vector3.up * 30, p);
    log.Append(id + "@" + p.ToString("F0") + " sea" + seaDir.ToString("F1") + " | ");
}
UnityEngine.Object.DestroyImmediate(go);
return log.ToString();
