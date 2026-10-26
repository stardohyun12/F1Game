// 과제 1514: 해안 구조물 전후 캡처(에디트 모드). SessionState "c1514.tag" = before/after.
// 1_top_straight = 도현 1.png 꼴(해안 직선을 바다 위에서) / 2_top_breakwater = 2.png 꼴(O_breakwater 입구) / 3·4 차 시점 해안 직선 / 5 방파제·항구
string tag = UnityEditor.SessionState.GetString("c1514.tag", "before");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/coast-v1/" + tag));
System.IO.Directory.CreateDirectory(outDir);
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 5).ToList();
var P = rs.Where(r => r.roadId == "main").OrderByDescending(r => r.centerline.Length).First().centerline;
var go = new GameObject("c1514cam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.3f; cam.farClipPlane = 1500f;
var mc = Camera.main; if (mc != null) { cam.clearFlags = mc.clearFlags; cam.backgroundColor = mc.backgroundColor; }
var log = new System.Text.StringBuilder(tag + " ");
System.Action<string, Vector3, Vector3, float> Shot = (name, cp, look, fov) =>
{
    cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = fov;
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); rt.Release();
    log.Append(name + " ");
};
System.Func<int, Vector3> Fw = i => { var f = P[Mathf.Min(i + 3, P.Length - 1)] - P[Mathf.Max(i - 3, 0)]; f.y = 0; return f.normalized; };
System.Func<Vector3, Vector3> SeaDir = p => { var best = Vector3.zero; float lo = 999; for (int a = 0; a < 16; a++) { var d = Quaternion.Euler(0, a * 22.5f, 0) * Vector3.forward; float h = BuildBayLoop.H(p.x + d.x * 30, p.z + d.z * 30); if (h < lo) { lo = h; best = d; } } return best; };
{ int i = 1240; var p = P[i]; var f = Fw(i); var nL = new Vector3(-f.z, 0, f.x); var s = Vector3.Dot(SeaDir(p), nL) > 0 ? nL : -nL;
  Shot("1_top_straight", p + s * 34 + Vector3.up * 26, p + s * 9 - Vector3.up * 4, 55f); }
{ var p = new Vector3(-96, 5, -111); var s = new Vector3(-0.7f, 0, -0.7f).normalized;
  Shot("2_top_breakwater", p + s * 45 + Vector3.up * 30, p, 55f); }
var B = new Vector3(0, 2.8f, -5.6f);
foreach (var (nm, i) in new[] { ("3_car_straight", 1200), ("4_car_straight", 1300) })
{ var p = P[i]; var f = Fw(i); var q = Quaternion.LookRotation(f); Shot(nm, p + q * B, p + Vector3.up * 1.0f + f * 6f, 64f); }
{ var bw = rs.First(r => r.roadId == "O_breakwater").centerline; var m = bw[bw.Length / 3]; var s = new Vector3(0.5f, 0, 0.8f).normalized;
  Shot("5_harbor_breakwater", m + s * 40 + Vector3.up * 24, m - s * 10, 55f); }
UnityEngine.Object.DestroyImmediate(go);
return log.ToString();
