// 과제 1124 캡처 — 토글 상태별 B 컷 + 전부 켬 B·소개·D. Play 중 LowResRenderTest RT(720) 를 Present(Nearest)로 1920×1080. 끝나면 전부 켬 상태로 둠
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a3/")); System.IO.Directory.CreateDirectory(outDir + "toggles/");
var root = GameObject.Find("SceneA3").transform; var log = new System.Text.StringBuilder();
var mats = new System.Collections.Generic.List<Material>(); foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art/SceneA3/Mats" })) mats.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));
System.Action<string> Set = bits => {   // 순서 R1 R2 R3 M1 M2 M3
  bool r1 = bits[0] == '1', r2 = bits[1] == '1', r3 = bits[2] == '1', m1 = bits[3] == '1', m2 = bits[4] == '1', m3 = bits[5] == '1';
  foreach (var m in mats) { if (!m.HasProperty("_Ramp")) continue; m.SetFloat("_Ramp", r1 ? 1 : 0); m.SetFloat("_R2", r2 ? 1 : 0); m.SetFloat("_Outline", r3 ? 1 : 0); }
  root.Find("A3_M1").gameObject.SetActive(m1); root.Find("A3_M2").gameObject.SetActive(m2); root.Find("A3_M3").gameObject.SetActive(m3); root.Find("A3_M3off").gameObject.SetActive(!m3);
  root.Find("A3_M2/edge5").gameObject.SetActive(!m3); root.Find("A3_M2/edge4").gameObject.SetActive(m3); };
var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; foreach (var c in cams) c.gameObject.SetActive(c == cam);
  var lr = cam.GetComponent<LowResRenderTest>(); lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + " " + lr.RenderSize + "; "); };
if (!Application.isPlaying) return "Play 중에만";
string[] st = { "111111", "000000", "011111", "101111", "110111", "111011", "111101", "111110" };
string[] nmz = { "all_on", "all_off", "no_R1", "no_R2", "no_R3", "no_M1", "no_M2", "no_M3" };
for (int i = 0; i < st.Length; i++) { Set(st[i]); Cap("Cam_B_Game", "toggles/" + nmz[i] + ".png"); }
Set("111111"); Cap("Cam_B_Game", "b_game.png"); Cap("Cam_Intro", "intro.png"); Cap("Cam_D", "d.png");
foreach (var c in cams) c.gameObject.SetActive(c.name == "Cam_B_Game"); dst.Release();
return log.ToString();
