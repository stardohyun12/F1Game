// artlive 1722: 결 전/후 근접 3곳 — Play 중, Studio_Building. Cam_ST_in 을 잠시 옮겨 720 Nearest 로 찍음(garage_close_1653 과 같은 방식).
// 인자 SessionState "grain.cap" = 태그 → docs/qa/evidence/studio-<에셋>/c_<태그>_<지점>.png
// 지점은 블렌더 좌표 → 유니티 (−x, z, −y) + 오프셋. garage: LAMP_0 기준. room: 렌더러 전체 경계 중심 ↔ 블렌더 경계 중심(0, −0.08, 1.2).
if (!Application.isPlaying) return "FAIL: Play 중에만";
string tag = UnityEditor.SessionState.GetString("grain.cap", "x"); Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot"); var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) on[t.gameObject] = t.gameObject.activeSelf;
var jobs = new (string asset, string k, Vector3 c, Vector3 l)[] {
  ("room", "desk", new Vector3(-0.8f, -1.35f, 1.45f), new Vector3(0.75f, -0.35f, 0.65f)),
  ("garage", "drum", new Vector3(3.4f, -4.2f, 1.5f), new Vector3(6.3f, -1.7f, 0.6f)),
  ("garage", "tire", new Vector3(-1.8f, 3.2f, 1.3f), new Vector3(-4.2f, 4.2f, 0.5f)) };
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
Camera cam = null; foreach (var c in cams) if (c.name == "Cam_ST_in") cam = c;
foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView; cam.fieldOfView = 60;
foreach (var j in jobs) {
  foreach (Transform t in slot.transform) t.gameObject.SetActive(t.name == j.asset);
  var g = slot.transform.Find(j.asset); Vector3 off;
  if (j.asset == "garage") { Transform l0 = null; foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (t.name == "LAMP_0") l0 = t; off = l0.position - new Vector3(-0.09f, 3.43f, -5.0f); }
  else { var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); off = b.center - new Vector3(0f, 1.2f, 0.08f); }
  System.Func<Vector3, Vector3> U = v => new Vector3(-v.x, v.z, -v.y) + off;
  cam.transform.position = U(j.c); cam.transform.LookAt(U(j.l));
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/studio-" + j.asset + "/"));
  System.IO.File.WriteAllBytes(outDir + "c_" + tag + "_" + j.k + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(j.asset + ":" + j.k + " off " + off.ToString("F2") + "; ");
  lr.targetHeight = h0; lr.smooth = s0; }
cam.transform.position = p0; cam.transform.rotation = r0; cam.fieldOfView = f0;
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
