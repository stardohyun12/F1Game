// artlive 1857: 코멘트 카메라 전/후(fb_cap_1846 과 같은 방식) + 보관소 시계 근접 두 시각. Play 중, Studio_Building.
// SessionState "fb.cap" = 태그 → docs/qa/evidence/feedback-1028/<id>_<태그>.png · 시계 = clock_<시각>_<태그>.png
if (!Application.isPlaying) return "FAIL: Play 중에만";
string tag = UnityEditor.SessionState.GetString("fb.cap", "x"); Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot"); var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) on[t.gameObject] = t.gameObject.activeSelf;
var jobs = new (string id, string asset, Vector3 c, Vector3 l, float fov, float min)[] {
  ("depot-20261028-184207-y1nn", "depot", new Vector3(-2.309f, 1.6f, -3.391f), new Vector3(-2.401f, 0.681f, -8.305f), 50f, -1f),
  ("depot-20261028-184236-af75", "depot", new Vector3(2.437f, 1.6f, -2.333f), new Vector3(2.304f, 2.185f, -7.297f), 50f, -1f),
  ("garage-20261028-183800-oih2", "garage", new Vector3(-2.294f, 1.535f, -7.639f), new Vector3(-1.065f, 0.337f, -12.335f), 50f, -1f),
  ("garage-20261028-183817-yu90", "garage", new Vector3(-2.257f, 1.535f, 1.969f), new Vector3(-7.093f, 0.356f, 1.499f), 50f, -1f),
  ("garage-20261028-183826-88vd", "garage", new Vector3(0.612f, 1.535f, -3.371f), new Vector3(4.597f, 0.376f, -6.16f), 50f, -1f),
  ("garage-20261028-183839-wwmp", "garage", new Vector3(-1.404f, 1.535f, -6.667f), new Vector3(-5.361f, -0.818f, -8.616f), 50f, -1f),
  ("garage-20261028-183904-q85r", "garage", new Vector3(-0.924f, 1.535f, 3.525f), new Vector3(3.083f, 1.445f, 0.536f), 50f, -1f),
  ("clock_0310", "depot", new Vector3(0f, -0.25f, 1.3f), Vector3.zero, 40f, 190f),   // 3:10
  ("clock_1045", "depot", new Vector3(0f, -0.25f, 1.3f), Vector3.zero, 40f, 645f) }; // 10:45
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
Camera cam = null; foreach (var c in cams) if (c.name == "Cam_ST_in") cam = c;
foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/feedback-1028/")); System.IO.Directory.CreateDirectory(outDir);
foreach (var j in jobs) {
  foreach (Transform t in slot.transform) t.gameObject.SetActive(t.name == j.asset);
  var mdl = slot.transform.Find(j.asset).GetChild(0);
  if (j.min >= 0) { var ck = slot.transform.Find(j.asset).GetComponentsInChildren<Transform>(true).First(t => t.name == "CLOCK");
    var wc = ck.GetComponent("WallClock") as MonoBehaviour; wc.enabled = false; wc.GetType().GetMethod("Show").Invoke(wc, new object[] { j.min });
    var hc = ck.Find("CLOCK_hand_hour").position; cam.transform.position = hc + j.c; cam.transform.LookAt(hc); }
  else { cam.transform.position = mdl.position + j.c; cam.transform.LookAt(mdl.position + j.l); }
  cam.fieldOfView = j.fov;
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + j.id + "_" + tag + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
  lr.targetHeight = h0; lr.smooth = s0; log.Append(j.id + " "); }
cam.transform.position = p0; cam.transform.rotation = r0; cam.fieldOfView = f0;
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
