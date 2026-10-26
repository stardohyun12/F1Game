var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).ToList();
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var log = new System.Text.StringBuilder();
System.Func<System.Func<RoadSurface, bool>, Vector2, (RoadSurface, int)> near = (f, q) => { RoadSurface br = null; int bi = 0; float bd = 1e18f; foreach (var r in rs.Where(f)) for (int i = 4; i < r.centerline.Length - 4; i++) { var p = r.centerline[i]; float d = (new Vector2(p.x, p.z) - q).sqrMagnitude; if (d < bd) { bd = d; br = r; bi = i; } } return (br, bi); };
// 헤어핀: posi/camo 차길에서 10m 앞뒤 꺾임이 가장 큰 점
(RoadSurface, int) hp = (null, 0); float bestAng = 0;
foreach (var r in rs) for (int i = 10; i < r.centerline.Length - 10; i++) { var c = r.centerline; float a = Vector3.Angle(c[i] - c[i - 10], c[i + 10] - c[i]); if (a > bestAng && r.kind != "shortcut") { bestAng = a; hp = (r, i); } }
var shortcut = rs.Where(r => r.kind == "shortcut").OrderByDescending(r => r.centerline.Length).First();
var spots = new (string, (RoadSurface, int))[] {
  ("1-harbor-grid", near(r => r.kind != "shortcut", new Vector2(40, -250))),
  ("2-coast-strip", near(r => r.roadId == "cape_road", new Vector2(-330, -330))),
  ("3-hairpin", hp),
  ("4-shortcut-stone", (shortcut, shortcut.centerline.Length / 2)),
  ("5-coast-main", near(r => r.kind == "main", new Vector2(-330, -100))) };
var go = new GameObject("TmpCarCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 55; cam.farClipPlane = 1500;
var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; var tx = new Texture2D(960, 540, TextureFormat.RGB24, false);
foreach (var (name, (r, i)) in spots)
{
  var c = r.centerline; var p = c[i]; var f = c[Mathf.Min(i + 3, c.Length - 1)] - c[Mathf.Max(i - 3, 0)]; f.y = 0; f.Normalize();
  rb.linearVelocity = Vector3.zero; rb.position = p + Vector3.up * .6f; rb.rotation = Quaternion.LookRotation(f); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
  cam.transform.position = p - f * 7.5f + Vector3.up * 3.6f; cam.transform.LookAt(p + f * 10f + Vector3.up * 1f);
  cam.Render(); RenderTexture.active = rt; tx.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tx.Apply(); RenderTexture.active = null;
  System.IO.File.WriteAllBytes("../docs/qa/evidence/townv6/" + name + ".png", tx.EncodeToPNG());
  log.Append($"{name}: {r.roadId}[{i}] ({p.x:F0},{p.y:F1},{p.z:F0}) ");
}
cam.targetTexture = null; UnityEngine.Object.Destroy(go); rt.Release();
log.Append($"hairpinAng {bestAng:F0}");
return log.ToString();
