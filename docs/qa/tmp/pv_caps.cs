// 과제 0535 캡처 — Play 중, Preview_Candidates. a7b_caps 방식(LowResRenderTest 720 → 1920×1080). → docs/qa/evidence/preview-1028/
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/preview-1028/")); System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var root = GameObject.Find("SceneA4").transform; var gt = root.GetComponent<A6dGrainToggles>(); var lt = root.GetComponent<A7LightToggles>();
var tg = UnityEngine.Object.FindFirstObjectByType<PreviewCandidatesToggle>();
lt.painterly = false; lt.realtimeShadows = true;
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  gt.SendMessage("Update"); lt.Apply();
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; lr.targetHeight = 720; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst); gt.ApplyTo(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; ");
  lr.targetHeight = h0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); };
string only = UnityEditor.SessionState.GetString("pv.only", "");
if (only == "") { tg.show = false; tg.Apply(); }   // only 지정 = 진단용, 지금 켜짐 상태 그대로 찍음
if (only == "") foreach (var v in new[] { "street", "house", "garage" }) Cap("Cam_PV_" + v, v + "_without.png");
if (only == "") { tg.show = true; tg.Apply(); }
foreach (var v in new[] { "street", "house", "garage", "depot15", "depot5", "room_door", "room_desk", "room_window" }) if (only == "" || only.Contains(v)) Cap("Cam_PV_" + v, v + (v == "street" || v == "house" || v == "garage" ? "_with" : "") + ".png");
foreach (var c in cams) if (c && c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
