// 과제 1916 캡처(a6d_fix1_caps 바탕) — Play 중. 접미사는 docs/qa/tmp/fix1_suffix.txt (before/after)
// 도현 페인트오버 1 과 같은 두 화면: house = Cam_Close_house(ROOMS 집 앞 낮은 시점, fix1_house 와 같은 자리), street = Cam_B_Game
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a6d/")); System.IO.Directory.CreateDirectory(outDir);
string suf = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/fix1_suffix.txt")).Trim();
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
Camera houseCam = null; foreach (var c in cams) if (c.name == "Cam_House") houseCam = c;
var gt = GameObject.Find("SceneA4").GetComponent<A6dGrainToggles>();
var cb = GameObject.Find("SceneA4").transform.Find("A4e_Curbs/Curb_house_narrow_a"); var hs = GameObject.Find("SceneA4").transform.Find("A4b_Houses/house_narrow_a");
var hc = UnityEngine.Object.Instantiate(houseCam.gameObject).GetComponent<Camera>(); hc.name = "Cam_Close_house"; hc.transform.position = cb.TransformPoint(new Vector3(0f, 1.2f, 7f)); hc.transform.LookAt(hs.position + Vector3.up * 3.5f); cams.Add(hc);
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  gt.SendMessage("Update");
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; lr.targetHeight = 720; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst); gt.ApplyTo(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; ");
  lr.targetHeight = h0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); };
Cap("Cam_Close_house", "po1_house_" + suf + ".png"); Cap("Cam_B_Game", "po1_street_" + suf + ".png");
UnityEngine.Object.Destroy(hc.gameObject);
foreach (var c in cams) if (c && c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
