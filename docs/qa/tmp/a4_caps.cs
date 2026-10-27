// 과제 1232 캡처 — Play 중. 토글(S1·S2·S3·T1·T2·건물 새/옛) 상태별 B 컷 + 기본 상태 B·소개·차 근접(Cam_D). 끝나면 기본(S 켬·T 끔·새 건물)
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a4/")); System.IO.Directory.CreateDirectory(outDir + "toggles/");
var root = GameObject.Find("SceneA4").transform; var tg = root.GetComponent<SceneA3Toggles>(); var log = new System.Text.StringBuilder();
var bNew = root.Find("A4_BldgNew").gameObject; var bOld = root.Find("A4_BldgOld").gameObject;
System.Action<string> Set = bits => {   // S1 S2 S3 T1 T2 新
  tg.S1_highlightRolloff = bits[0] == '1'; tg.S2_oneLight = bits[1] == '1'; tg.S3_softBorders = bits[2] == '1'; tg.T1_sunEdge = bits[3] == '1'; tg.T2_shadeAmbient = bits[4] == '1'; tg.Apply();
  bNew.SetActive(bits[5] == '1'); bOld.SetActive(bits[5] != '1'); };
var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + " " + lr.RenderSize + "; "); };
string[] st = { "111001", "000001", "011001", "101001", "110001", "111101", "111011", "111000" };
string[] nmz = { "all_on", "all_off", "no_S1", "no_S2", "no_S3", "plus_T1", "plus_T2", "bldg_old" };
for (int i = 0; i < st.Length; i++) { Set(st[i]); Cap("Cam_B_Game", "toggles/" + nmz[i] + ".png"); }
Set("111001"); Cap("Cam_B_Game", "b_game.png"); Cap("Cam_Intro", "intro.png"); Cap("Cam_D", "car_close.png");
Set("111000"); Cap("Cam_Intro", "toggles/bldg_old_intro.png"); Set("111001");
foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
