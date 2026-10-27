// 과제 1302 캡처 — Play 중. B·소개·집 근접·정비소 실내 → docs/qa/evidence/scene-a4b/. 끝나면 GameCam 만 켬
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a4b/")); System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + " " + lr.RenderSize + "; "); };
Cap("Cam_B_Game", "b_game.png"); Cap("Cam_Intro", "intro.png"); Cap("Cam_House", "house_close.png"); Cap("Cam_GarageIn", "garage_interior.png"); Cap("Cam_D", "car_close.png");
foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
