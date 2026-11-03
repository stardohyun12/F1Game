// artlive 20261031-0205 집 유형 캡처 — Play 중. 마을 = house_types 만(house_home·밭·나무 끔). 세트 A(H1A~H4A) → B 바꿔 끼우며
// Cam_HT_village_{eye,kart} + 단독 Cam_HT_H<n>_solo(그 집 + 비탈 단만) 을 720 Nearest(LowResRenderTest) + 실시간 그림자(A7) 로.
// → docs/qa/evidence/iter-house-types/sNN_*.png + art/_review/2026-10-31-house-types/ 사본
if (!Application.isPlaying) return "FAIL: Play 중에만";
string nn = "06";
Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot");
var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) on[t.gameObject] = t.gameObject.activeSelf;
// house_home 자리는 바닥이 house_home 의 땅판으로 뚫려 있다 → 집은 끄고 땅판(높이 0.3m 아래 렌더러)만 켬. 끝나면 되돌림
var hh = slot.transform.Find("house_home"); var ren = new System.Collections.Generic.Dictionary<Renderer, bool>();
foreach (var r in hh.GetComponentsInChildren<Renderer>(true)) ren[r] = r.enabled;
void Scene(bool village) { foreach (Transform t in slot.transform) t.gameObject.SetActive(t.name == "house_types");
  foreach (var r in ren.Keys) r.enabled = ren[r] && r.bounds.max.y < 0.3f; }
var ht = slot.transform.Find("house_types");
string root = System.IO.Path.GetFullPath(Application.dataPath + "/../../");
string ev = root + "docs/qa/evidence/iter-house-types/", rv = root + "art/_review/2026-10-31-house-types/"; System.IO.Directory.CreateDirectory(ev); System.IO.Directory.CreateDirectory(rv);
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
for (int k = 0; k < 2; k++) {
  string v = k == 0 ? "A" : "B";
  for (int i = 1; i <= 4; i++) { var sp = ht.Find("H" + i); sp.gameObject.SetActive(true); sp.Find("H" + i + "A").gameObject.SetActive(k == 0); sp.Find("H" + i + "B").gameObject.SetActive(k == 1); }
  Scene(true); Shot("Cam_HT_village_eye", "s" + nn + "_set" + v + "_village_eye.png"); Shot("Cam_HT_village_kart", "s" + nn + "_set" + v + "_village_kart.png");
  Scene(false);
  for (int i = 1; i <= 4; i++) { for (int j = 1; j <= 4; j++) ht.Find("H" + j).gameObject.SetActive(j == i); Shot("Cam_HT_H" + i + "_solo", "s" + nn + "_H" + i + v + "_solo.png"); }
}
for (int i = 1; i <= 4; i++) { var sp = ht.Find("H" + i); sp.gameObject.SetActive(true); sp.Find("H" + i + "A").gameObject.SetActive(true); sp.Find("H" + i + "B").gameObject.SetActive(false); }
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in ren) if (kv.Key) kv.Key.enabled = kv.Value;
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
