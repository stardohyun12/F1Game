// artlive 1653: 정비소 근접 4지점(천장·공구판·원통·게시판) — Play 중, Studio_Building. Cam_ST_in 을 잠시 옮겨 720 Nearest 로 찍음.
// 인자 SessionState "studio.close" = 태그(before/after) → docs/qa/evidence/studio-garage/c_<태그>_<지점>.png
// 지점은 블렌더 좌표 → 유니티: (−x, z, −y) + (LAMP_0 실제 위치 − LAMP_0 블렌더 위치(0.09, 5.0, 3.43)). LAMP_0 이 없으면(옛 fbx) 프리팹 원점 규칙으로 계산.
if (!Application.isPlaying) return "FAIL: Play 중에만";
string tag = UnityEditor.SessionState.GetString("studio.close", "x"); Application.runInBackground = true;
var slot = GameObject.Find("STUDIO/STUDIO_Slot"); var on = new System.Collections.Generic.Dictionary<GameObject, bool>();
foreach (Transform t in slot.transform) { on[t.gameObject] = t.gameObject.activeSelf; t.gameObject.SetActive(t.name == "garage"); }
var g = slot.transform.Find("garage"); Transform l0 = null; foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (t.name == "LAMP_0") l0 = t;
Vector3 off;
if (l0) off = l0.position - new Vector3(-0.09f, 3.43f, -5.0f);
else { var mdl = g.GetChild(0); off = mdl.position; }  // 옛 fbx: 메시 원점 = 블렌더 원점
System.Func<float, float, float, Vector3> U = (x, y, z) => new Vector3(-x, z, -y) + off;
var spots = new (string k, Vector3 c, Vector3 l)[] { ("ceil", U(1.0f, 2.6f, 1.5f), U(1.3f, 7.0f, 3.4f)), ("peg", U(2.4f, 4.8f, 1.75f), U(4.98f, 4.8f, 1.85f)), ("cyl", U(-1.8f, 3.2f, 1.3f), U(-4.2f, 4.2f, 0.5f)), ("board", U(-2.4f, 4.65f, 1.9f), U(-4.83f, 4.65f, 2.1f)), ("floor", U(0.6f, 1.6f, 2.4f), U(0.6f, 6.0f, 0f)) };
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/studio-garage/"));
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder("off " + off.ToString("F2") + " lamp " + (l0 != null) + " lights " + g.GetComponentsInChildren<Light>(true).Length + "; ");
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
Camera cam = null; foreach (var c in cams) if (c.name == "Cam_ST_in") cam = c;
foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView; cam.fieldOfView = 60;
foreach (var s in spots) {
  cam.transform.position = s.c; cam.transform.LookAt(s.l);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + "c_" + tag + "_" + s.k + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(s.k + " ");
  lr.targetHeight = h0; lr.smooth = s0; }
cam.transform.position = p0; cam.transform.rotation = r0; cam.fieldOfView = f0;
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
