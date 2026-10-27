// 과제 1815 캡처(a6d_v6c_caps 바탕) — Play 중. 접미사는 docs/qa/tmp/fix1_suffix.txt (before/after)
// B 컷 + 집 근접(narrow_a 정면, 길 건너에서) + 카트 근접 + 돌담(narrow_a 연석 줄, 길에서 비스듬히)
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a6d/")); System.IO.Directory.CreateDirectory(outDir);
string suf = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/fix1_suffix.txt")).Trim();
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
Camera houseCam = null; foreach (var c in cams) if (c.name == "Cam_House") houseCam = c;
var gt = GameObject.Find("SceneA4").GetComponent<A6dGrainToggles>();
var race = GameObject.Find("Race").transform;
System.Func<string, Vector3, Vector3, Camera> MakeCam = (nm, pos, look) => { var c2 = UnityEngine.Object.Instantiate(houseCam.gameObject).GetComponent<Camera>(); c2.name = nm; c2.transform.position = pos; c2.transform.LookAt(look); cams.Add(c2); return c2; };
var cb = GameObject.Find("SceneA4").transform.Find("A4e_Curbs/Curb_house_narrow_a"); var hs = GameObject.Find("SceneA4").transform.Find("A4b_Houses/house_narrow_a");
MakeCam("Cam_Close_house", cb.TransformPoint(new Vector3(0f, 1.2f, 7f)), hs.position + Vector3.up * 3.5f);
MakeCam("Cam_Close_wall", cb.TransformPoint(new Vector3(-6.5f, 1.0f, 3.2f)), cb.TransformPoint(new Vector3(0f, 0f, 0f)));
MakeCam("Cam_Close_kart", race.position + race.forward * 2.6f + race.right * 1.8f + Vector3.up * 1.3f, race.position + Vector3.up * 0.45f);
MakeCam("Cam_Close_kartback", race.position - race.forward * 2.8f + race.right * 1.2f + Vector3.up * 1.5f, race.position + Vector3.up * 0.45f);
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  gt.SendMessage("Update");
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; lr.targetHeight = 720; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst); gt.ApplyTo(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + "; ");
  lr.targetHeight = h0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); };
Cap("Cam_B_Game", "fix1_b_game_" + suf + ".png"); Cap("Cam_Close_house", "fix1_house_" + suf + ".png"); Cap("Cam_Close_wall", "fix1_wall_" + suf + ".png");
Cap("Cam_Close_kart", "fix1_kart_" + suf + ".png"); Cap("Cam_Close_kartback", "fix1_kartback_" + suf + ".png");
foreach (var c in cams) if (c.name.StartsWith("Cam_Close_")) UnityEngine.Object.Destroy(c.gameObject);
foreach (var c in cams) if (c && c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
