// artlive 라이브 아트 캡처 — Play 중, Iter_<에셋> 씬. 고정 카메라 Cam_IT_{road,front,34} 를 720 Nearest 게임 화면(LowResRenderTest 720 → 1280×720 1:1)으로.
// 인자: SessionState "iter.cap" = "<단계 번호>|<짧은 설명>|<에셋>"  (iter_cap.sh 가 넣는다)  → docs/qa/evidence/iter-<에셋>/sNN_<설명>_<캠>.png
if (!Application.isPlaying) return "FAIL: Play 중에만";
var arg = UnityEditor.SessionState.GetString("iter.cap", "").Split('|');
if (arg.Length < 3) return "FAIL: iter.cap 인자 없음 (번호|설명|에셋)";
string nn = int.Parse(arg[0]).ToString("00"), desc = arg[1], asset = arg[2];
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/iter-" + asset + "/")); System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
foreach (var v in new[] { "road", "front", "34" }) {
  Camera cam = null; foreach (var c in cams) if (c.name == "Cam_IT_" + v) cam = c; if (!cam) { log.Append("no Cam_IT_" + v + "; "); continue; }
  foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  string file = "s" + nn + "_" + desc + "_" + v + ".png"; System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; ");
  lr.targetHeight = h0; lr.smooth = s0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); }
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
return "OK " + outDir + " " + log;
