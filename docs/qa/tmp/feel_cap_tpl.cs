// 0320 차 시점 4 + 위 평면 (TAG=before/after) — v6c 캡처(에디트 모드): 위 평면 1 + 차 시점 5. 960×540 / 위 1600×1600 정사영(±400 m)
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).ToList();
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var log = new System.Text.StringBuilder(); bool onlyTop = false;
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/townv6-feel/")); System.IO.Directory.CreateDirectory(dir);
System.Func<System.Func<RoadSurface, bool>, Vector2, (RoadSurface, int)> near = (f, q) => { RoadSurface br = null; int bi = 0; float bd = 1e18f; foreach (var r in rs.Where(f)) for (int i = 4; i < r.centerline.Length - 4; i++) { var p = r.centerline[i]; float d = (new Vector2(p.x, p.z) - q).sqrMagnitude; if (d < bd) { bd = d; br = r; bi = i; } } return (br, bi); };
(RoadSurface, int) hp = (null, 0); float bestAng = 0;
foreach (var r in rs) for (int i = 10; i < r.centerline.Length - 10; i++) { var c = r.centerline; float a = Vector3.Angle(c[i] - c[i - 10], c[i + 10] - c[i]); if (a > bestAng && r.kind != "shortcut") { bestAng = a; hp = (r, i); } }
var sc = rs.Where(r => r.roadId.StartsWith("sc_garage_top_house")).First();
// (이름, 길·점, 바라볼 곳: null 이면 진행 방향)
var spots = new (string, (RoadSurface, int), Vector3?)[] {
  ("car1-harbor-grid-TAG", near(r => r.kind != "shortcut", new Vector2(-150, -215)), null),
  ("car2-hairpin-TAG", hp, null),
  ("car3-coast-main-TAG", near(r => r.roadId == "main_west", new Vector2(-445, -262)), null),
  ("car4-stone-TAG", near(r => r.roadId == "sc_garage_top_house_0", new Vector2(-36, 150)), null) };
var go = new GameObject("TmpCapCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 55; cam.farClipPlane = 1500;
var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; var tx = new Texture2D(960, 540, TextureFormat.RGB24, false);
var car0 = car.transform.position; var carR = car.transform.rotation;
foreach (var (name, (r, i), look) in spots.Where(s => !onlyTop || s.Item1.StartsWith("1")))
{
  var c = r.centerline; var p = c[i]; var f = look.HasValue ? look.Value - p : c[Mathf.Min(i + 3, c.Length - 1)] - c[Mathf.Max(i - 3, 0)]; f.y = 0; f.Normalize();
  car.transform.SetPositionAndRotation(p + Vector3.up * .6f, Quaternion.LookRotation(f));
  cam.transform.position = p - f * 4.2f + Vector3.up * 1.9f; cam.transform.LookAt(p + f * 8f + Vector3.up * 1f);   // 카메라 B 와 같은 자리
  cam.Render(); RenderTexture.active = rt; tx.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tx.Apply(); RenderTexture.active = null;
  System.IO.File.WriteAllBytes(dir + name + ".png", tx.EncodeToPNG());
  if (false) foreach (var (w, h) in new[] { (1280, 720), (960, 540) }) { var r2 = new RenderTexture(w, h, 24); var t2 = new Texture2D(w, h, TextureFormat.RGB24, false); cam.targetTexture = r2; cam.Render(); RenderTexture.active = r2; t2.ReadPixels(new Rect(0, 0, w, h), 0, 0); t2.Apply(); RenderTexture.active = null; System.IO.File.WriteAllBytes(dir + name + "-" + h + ".png", t2.EncodeToPNG()); cam.targetTexture = rt; r2.Release(); }
  log.Append($"{name}: {r.roadId}[{i}] ({p.x:F0},{p.y:F1},{p.z:F0}) | ");
}
car.transform.SetPositionAndRotation(car0, carR);
cam.targetTexture = null; rt.Release();
var rt2 = new RenderTexture(1600, 1600, 24); var tx2 = new Texture2D(1600, 1600, TextureFormat.RGB24, false);
cam.orthographic = true; cam.orthographicSize = 400; cam.targetTexture = rt2; cam.transform.position = new Vector3(0, 160, 0); cam.nearClipPlane = .3f; cam.transform.rotation = Quaternion.Euler(90, 0, 0);
cam.Render(); RenderTexture.active = rt2; tx2.ReadPixels(new Rect(0, 0, 1600, 1600), 0, 0); tx2.Apply(); RenderTexture.active = null;
System.IO.File.WriteAllBytes(dir + "top-TAG.png", tx2.EncodeToPNG());
cam.targetTexture = null; rt2.Release(); UnityEngine.Object.DestroyImmediate(go);
log.Append($"hairpinAng {bestAng:F0}");
return log.ToString();
