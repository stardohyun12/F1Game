// 과제 1704 캡처(a4e_caps 복사, 출력 scene-a6c, + 정비소·가까운 집 한 화면 컷) — Play 중. B·소개 + 집 근접 2(FISH=house_shop, ROOMS=house_narrow_a): Cam_House 를 런타임 복제해 집 정면 길 위에 둠(씬 저장 안 함)
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a6c/")); System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
Camera houseCam = null; foreach (var c in cams) if (c.name == "Cam_House") houseCam = c;
var g = GameObject.Find("SceneA4").transform.Find("A4b_Houses");
foreach (var hn in new[] { "house_shop", "house_narrow_a" }) {
  var t = g.Find(hn); var q = t.rotation; t.rotation = Quaternion.identity; var b = t.GetComponentInChildren<Renderer>().bounds; t.rotation = q; float fz = b.max.z - t.position.z;
  var o = t.forward; o.y = 0; o.Normalize(); var c2 = UnityEngine.Object.Instantiate(houseCam.gameObject).GetComponent<Camera>(); c2.name = "Cam_Close_" + hn;
  c2.transform.position = t.position + o * (fz + 7.5f) + Vector3.up * 1.6f + Vector3.Cross(Vector3.up, o) * 1.5f; c2.transform.LookAt(t.position + o * (fz * 0.5f) + Vector3.up * 3.2f); cams.Add(c2); }
{ var gar = GameObject.Find("SceneA4").transform.Find("garage_v5_combo"); var gb = new Bounds(gar.position, Vector3.zero); foreach (var r in gar.GetComponentsInChildren<Renderer>()) gb.Encapsulate(r.bounds);
  Transform nh = null; float bd = 1e9f; foreach (Transform h in g) { float d0 = Vector3.Distance(h.position, gb.center); if (d0 < bd) { bd = d0; nh = h; } }
  var mid = (gb.center + nh.position) * 0.5f; var ax = nh.position - gb.center; ax.y = 0; var side = Vector3.Cross(Vector3.up, ax.normalized); float dist = ax.magnitude * 0.9f + 10f;
  var road = GameObject.Find("Race").transform.position; if (Vector3.Dot(road - mid, side) < 0) side = -side;
  var cg = UnityEngine.Object.Instantiate(houseCam.gameObject).GetComponent<Camera>(); cg.name = "Cam_Close_garage"; cg.transform.position = mid + side * dist + Vector3.up * (dist * 0.3f + 2f); cg.transform.LookAt(mid + Vector3.up * 2.5f); cams.Add(cg);
  log.Append("garage+" + nh.name + " d " + bd.ToString("F1") + "; "); }
System.Action<string, string, int> Cap = (nm, file, h) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; lr.targetHeight = h; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + " " + lr.RenderSize + "; ");
  lr.targetHeight = h0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); };
Cap("Cam_B_Game", "b_game.png", 720); Cap("Cam_Intro", "intro.png", 720); Cap("Cam_Close_house_shop", "house_fish.png", 720); Cap("Cam_Close_house_narrow_a", "house_rooms.png", 720); Cap("Cam_Close_garage", "garage_house.png", 720);
foreach (var c in cams) if (c.name.StartsWith("Cam_Close_")) UnityEngine.Object.Destroy(c.gameObject);
foreach (var c in cams) if (c && c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
