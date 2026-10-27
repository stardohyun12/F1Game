// 과제 1302 에디트 모드 빠른 캡처(확인용, 증거 아님) → docs/qa/tmp/a4b_q_<cam>.png
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
var sb = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) sb[c] = c.gameObject.activeSelf;
foreach (var nm in new[] { "Cam_B_Game", "Cam_Intro", "Cam_House", "Cam_GarageIn" }) {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); continue; }
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + "a4b_q_" + nm + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(nm + " ok; "); }
foreach (var kv in sb) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
return log.ToString();
