// artlive 20261031-0142 나무 후보 캡처 — Play 중. house_home + field_patch + trees 만 켜고, 세트 A(T1·3·5·7) → B(T2·4·6·8) 를 바꿔 끼우며
// Cam_TR_<종>_{eye,kart} · Cam_TR_all 을 720 Nearest(LowResRenderTest) + 실시간 그림자(A7) 로. 같은 빛.
// 인자 SessionState "tr.cap" = 단계 번호 → docs/qa/evidence/iter-trees/sNN_T<n>_<캠>.png + art/_review/2026-10-31-trees/ 사본
if (!Application.isPlaying) return "FAIL: Play 중에만";
string nn = int.Parse(UnityEditor.SessionState.GetString("tr.cap", "1")).ToString("00");
Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot");
var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) { on[t.gameObject] = t.gameObject.activeSelf; t.gameObject.SetActive(t.name == "house_home" || t.name == "field_patch" || t.name == "trees"); }
var trees = slot.transform.Find("trees");
string root = System.IO.Path.GetFullPath(Application.dataPath + "/../../");
string ev = root + "docs/qa/evidence/iter-trees/", rv = root + "art/_review/2026-10-31-trees/"; System.IO.Directory.CreateDirectory(ev); System.IO.Directory.CreateDirectory(rv);
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
var SP = new[] { "pine", "celtis", "camellia", "cypress" };
void Shot(string camName, string file) {
  Camera cam = cams.FirstOrDefault(c => c.name == camName); if (!cam) { log.Append("no " + camName + "; "); return; }
  foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  var png = tx.EncodeToPNG(); System.IO.File.WriteAllBytes(ev + file, png); System.IO.File.WriteAllBytes(rv + file, png); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; ");
  lr.targetHeight = h0; lr.smooth = s0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); }
for (int k = 0; k < 2; k++) {
  for (int i = 0; i < 4; i++) { var spot = trees.Find("TREE_" + SP[i]); spot.Find("T" + (i * 2 + 1)).gameObject.SetActive(k == 0); spot.Find("T" + (i * 2 + 2)).gameObject.SetActive(k == 1); }
  for (int i = 0; i < 4; i++) { int n = i * 2 + k + 1; Shot("Cam_TR_" + SP[i] + "_eye", "s" + nn + "_T" + n + "_eye.png"); Shot("Cam_TR_" + SP[i] + "_kart", "s" + nn + "_T" + n + "_kart.png"); }
  Shot("Cam_TR_all", "s" + nn + "_set" + (k == 0 ? "A" : "B") + "_all.png");
}
for (int i = 0; i < 4; i++) { var spot = trees.Find("TREE_" + SP[i]); spot.Find("T" + (i * 2 + 1)).gameObject.SetActive(true); spot.Find("T" + (i * 2 + 2)).gameObject.SetActive(false); }
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
