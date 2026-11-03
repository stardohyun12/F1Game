// 시험용 사본 — artlive 20261031-1926 짙은 숲 후보 캡처 — Play 중. house_home + field_patch + forest 만 켜고, F1~F4 를 하나씩:
//   무리(GROVE_F<n>) → Cam_FO_far(멀리) · Cam_FO_kart(카트 2.8m) / 단독(SINGLE_F<n>) → Cam_FO_single. 720 Nearest(LowResRenderTest) + 실시간 그림자(A7), 같은 빛.
// SessionState "fo.cap" = 단계 번호 → docs/qa/evidence/iter-forest/sNN_F<n>_<캠>.png + art/_review/2026-10-31-forest/ 사본
if (!Application.isPlaying) return "FAIL: Play 중에만";
string nn = int.Parse(UnityEditor.SessionState.GetString("fo.cap", "1")).ToString("00");
Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot");
var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) { on[t.gameObject] = t.gameObject.activeSelf; t.gameObject.SetActive(t.name == "house_home" || t.name == "field_patch" || t.name == "forest"); }
var fo = slot.transform.Find("forest");
string root = System.IO.Path.GetFullPath(Application.dataPath + "/../../");
string ev = root + "docs/qa/evidence/iter-forest/", rv = root + "art/_review/2026-10-31-forest/"; System.IO.Directory.CreateDirectory(ev); System.IO.Directory.CreateDirectory(rv);
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
void Shot(string camName, string file) {
  Camera cam = cams.FirstOrDefault(c => c.name == camName); if (!cam) { log.Append("no " + camName + "; "); return; }
  foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  var png = tx.EncodeToPNG(); System.IO.File.WriteAllBytes(ev + file, png); System.IO.File.WriteAllBytes(rv + file, png); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; ");
  lr.targetHeight = h0; lr.smooth = s0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); }
void Only(string nm) { foreach (Transform ch in fo) if (ch.name != "FOREST_slope") ch.gameObject.SetActive(ch.name == nm); }
// 시험: 비탈 그림자 받기 끔 / 켬, 단독 캠
var slr = fo.Find("FOREST_slope").GetComponent<MeshRenderer>();
Only("SINGLE_F1"); slr.receiveShadows = false; Shot("Cam_FO_single", "t_noshadow.png"); slr.receiveShadows = true;
var hh = slot.transform.Find("house_home").gameObject; hh.SetActive(false); Shot("Cam_FO_single", "t_nohouse.png"); hh.SetActive(true);
Only("GROVE_F1");
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
