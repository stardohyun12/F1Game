// artlive 20261030-2003 field_patch 캡처 — Play 중. house_home + field_patch 만 켜고 Cam_FP_{front,kart} 를 720 Nearest(LowResRenderTest) 로.
// 인자 SessionState "fp.cap" = "<단계 번호>|<짧은 설명>" → docs/qa/evidence/iter-field_patch/sNN_<설명>_<캠>.png + art/_review/2026-10-30-field-asset/ 사본
if (!Application.isPlaying) return "FAIL: Play 중에만";
var arg = UnityEditor.SessionState.GetString("fp.cap", "1|grey").Split('|');
string nn = int.Parse(arg[0]).ToString("00"), desc = arg[1];
Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot");
var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) { on[t.gameObject] = t.gameObject.activeSelf; t.gameObject.SetActive(t.name == "house_home" || t.name == "field_patch"); }
string root = System.IO.Path.GetFullPath(Application.dataPath + "/../../");
string ev = root + "docs/qa/evidence/iter-field_patch/", rv = root + "art/_review/2026-10-30-field-asset/"; System.IO.Directory.CreateDirectory(ev); System.IO.Directory.CreateDirectory(rv);
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
foreach (var v in new[] { "front", "kart", "close", "yard" }) {  // 2056: yard = 집 마당 컷(house_home + 밭)
  Camera cam = cams.FirstOrDefault(c => c.name == "Cam_FP_" + v); if (!cam) { log.Append("no Cam_FP_" + v + "; "); continue; }
  foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  string file = "s" + nn + "_" + desc + "_" + v + ".png"; var png = tx.EncodeToPNG(); System.IO.File.WriteAllBytes(ev + file, png); System.IO.File.WriteAllBytes(rv + file, png); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; ");
  lr.targetHeight = h0; lr.smooth = s0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); }
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
