// artlive 1722: 결 전/후 근접 3곳 — Play 중, Studio_Building. Cam_ST_in 을 잠시 옮겨 720 Nearest 로 찍음(garage_close_1653 과 같은 방식).
// 인자 SessionState "fb.cap" = 태그 → docs/qa/evidence/studio-<에셋>/c_<태그>_<지점>.png
// 지점은 블렌더 좌표 → 유니티 (−x, z, −y) + 오프셋. garage: LAMP_0 기준. room: 렌더러 전체 경계 중심 ↔ 블렌더 경계 중심(0, −0.08, 1.2).
if (!Application.isPlaying) return "FAIL: Play 중에만";
string tag = UnityEditor.SessionState.GetString("fb.cap", "x"); Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot"); var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) on[t.gameObject] = t.gameObject.activeSelf;
var jobs = new (string id, string asset, Vector3 c, Vector3 l, float fov)[] {
  ("depot-20261028-184108-1yzo", "depot", new Vector3(2.39f, 1.6f, 3.653f), new Vector3(3.943f, 3.106f, -0.854f), 50f),
  ("depot-20261028-184119-ztzt", "depot", new Vector3(4.764f, 1.6f, 3.434f), new Vector3(3.017f, 2.982f, -1.043f), 50f),
  ("depot-20261028-184207-y1nn", "depot", new Vector3(-2.309f, 1.6f, -3.391f), new Vector3(-2.401f, 0.681f, -8.305f), 50f),
  ("depot-20261028-184236-af75", "depot", new Vector3(2.437f, 1.6f, -2.333f), new Vector3(2.304f, 2.185f, -7.297f), 50f),
  ("depot-20261028-184255-kfrs", "depot", new Vector3(2.911f, 1.6f, -4.681f), new Vector3(3.025f, -0.533f, -9.202f), 50f),
  ("garage-20261028-183800-oih2", "garage", new Vector3(-2.294f, 1.535f, -7.639f), new Vector3(-1.065f, 0.337f, -12.335f), 50f),
  ("garage-20261028-183817-yu90", "garage", new Vector3(-2.257f, 1.535f, 1.969f), new Vector3(-7.093f, 0.356f, 1.499f), 50f),
  ("garage-20261028-183826-88vd", "garage", new Vector3(0.612f, 1.535f, -3.371f), new Vector3(4.597f, 0.376f, -6.16f), 50f),
  ("garage-20261028-183839-wwmp", "garage", new Vector3(-1.404f, 1.535f, -6.667f), new Vector3(-5.361f, -0.818f, -8.616f), 50f),
  ("garage-20261028-183904-q85r", "garage", new Vector3(-0.924f, 1.535f, 3.525f), new Vector3(3.083f, 1.445f, 0.536f), 50f),
  ("postoffice-20261028-184329-i97z", "postoffice", new Vector3(-1.941f, 1.6f, 6.74f), new Vector3(-0.617f, 1.55f, 1.919f), 50f),
  ("postoffice-20261028-184350-vpwt", "postoffice", new Vector3(-0.008f, 1.6f, 5.584f), new Vector3(0.151f, 1.101f, 0.611f), 50f),
  ("postoffice-20261028-184404-w6x8", "postoffice", new Vector3(-3.015f, 1.6f, -3.366f), new Vector3(-4.745f, -0.319f, -7.647f), 50f),
  ("postoffice-20261028-184446-lc0d", "postoffice", new Vector3(1.172f, 1.6f, -3.666f), new Vector3(-2.233f, 1.44f, -0.008f), 50f),
  ("room-20261028-183939-tglf", "room", new Vector3(-0.33f, 1.6f, 0.359f), new Vector3(1.444f, -1.888f, -2.754f), 50f),
  ("room-20261028-183953-oxq0", "room", new Vector3(0.261f, 1.6f, -0.503f), new Vector3(-1.967f, -1.313f, 2.895f), 50f),
  ("room-20261028-184017-hjwi", "room", new Vector3(2.449f, 1.6f, 2.694f), new Vector3(-1.244f, 0.992f, -0.621f), 50f) };
tag = UnityEditor.SessionState.GetString("fb.cap", "x");
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
  cam.transform.position = mdl.position + j.c; cam.transform.LookAt(mdl.position + j.l); cam.fieldOfView = j.fov;
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + j.id + "_" + tag + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
  lr.targetHeight = h0; lr.smooth = s0; log.Append(j.id + " "); }
cam.transform.position = p0; cam.transform.rotation = r0; cam.fieldOfView = f0;
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
