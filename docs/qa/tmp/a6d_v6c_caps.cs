// 과제 1736 캡처(a6d_caps 축약: 카트 v6c B 컷·근접만)(a6c_caps 바탕) — Play 중. B 컷 4종(기본=(a)만 / (b)만 / 둘 다 / 둘 다 끔) + 가로등 근접 + 카트 근접
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a6d/")); System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
Camera houseCam = null; foreach (var c in cams) if (c.name == "Cam_House") houseCam = c;
var gt = GameObject.Find("SceneA4").GetComponent<A6dGrainToggles>();
var race = GameObject.Find("Race").transform;
System.Func<string, Vector3, Vector3, Camera> MakeCam = (nm, pos, look) => { var c2 = UnityEngine.Object.Instantiate(houseCam.gameObject).GetComponent<Camera>(); c2.name = nm; c2.transform.position = pos; c2.transform.LookAt(look); cams.Add(c2); return c2; };
{ // 가로등 근접: 길(Race) 쪽에서 3.5m, 기둥 중간 높이
  Renderer sl = null; float bd = 1e9f; foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("streetlight_01")) { float d0 = Vector3.Distance(r.bounds.center, race.position); if (d0 < bd) { bd = d0; sl = r; } }
  var b = sl.bounds; var o = race.position - b.center; o.y = 0; o.Normalize(); var side = Vector3.Cross(Vector3.up, o);
  MakeCam("Cam_Close_light", b.center + o * 4.5f + side * 1.2f + Vector3.up * 0.3f, b.center + Vector3.up * 0.6f); log.Append("light " + sl.name + " h " + b.size.y.ToString("F1") + " d " + bd.ToString("F0") + "; "); }
MakeCam("Cam_Close_kart", race.position + race.forward * 2.6f + race.right * 1.8f + Vector3.up * 1.3f, race.position + Vector3.up * 0.45f);
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  gt.SendMessage("Update");
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; lr.targetHeight = 720; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst); gt.ApplyTo(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + " " + lr.RenderSize + "; ");
  lr.targetHeight = h0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); };
gt.surfaceGrain = true; gt.screenGrain = false; Cap("Cam_B_Game", "b_game_v6c.png"); Cap("Cam_Close_kart", "kart_v6c.png");
gt.SendMessage("Update");
foreach (var c in cams) if (c.name.StartsWith("Cam_Close_")) UnityEngine.Object.Destroy(c.gameObject);
foreach (var c in cams) if (c && c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
