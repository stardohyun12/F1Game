// 과제 0236 캡처 — Play 중. 시점(집·거리·정비소 안)마다 [그림자 켬 / 그림자 끔 / 그림자 켬+붓질] → docs/qa/evidence/scene-a7b/
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a7b/")); System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder(); var dst = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
Camera houseCam = null; foreach (var c in cams) if (c.name == "Cam_House") houseCam = c;
var root = GameObject.Find("SceneA4").transform; var gt = root.GetComponent<A6dGrainToggles>(); var lt = root.GetComponent<A7LightToggles>();
var made = new System.Collections.Generic.List<GameObject>();
System.Func<string, Vector3, Vector3, Camera> Mk = (nm, pos, look) => { var c = UnityEngine.Object.Instantiate(houseCam.gameObject).GetComponent<Camera>(); c.name = nm; c.transform.position = pos; c.transform.LookAt(look); cams.Add(c); made.Add(c.gameObject); return c; };
var cb = root.Find("A4e_Curbs/Curb_house_narrow_a"); var hs = root.Find("A4b_Houses/house_narrow_a");
Mk("Cam_Close_house", cb.TransformPoint(new Vector3(0f, 1.2f, 7f)), hs.position + Vector3.up * 3.5f);
var gb = new Bounds(); bool f0 = false; foreach (var r in root.Find("garage_v5_combo").GetComponentsInChildren<Renderer>()) { if (!f0) { gb = r.bounds; f0 = true; } else gb.Encapsulate(r.bounds); }
var gc = Mk("Cam_GarageIn", gb.center + new Vector3(0f, -1.2f, gb.extents.z * 0.7f), gb.center + new Vector3(0f, -1.7f, -gb.extents.z * 0.6f)); gc.fieldOfView = 70f;
System.Action<string, string> Cap = (nm, file) => {
  Camera cam = null; foreach (var c in cams) if (c.name == nm) cam = c; if (!cam) { log.Append("no " + nm + "; "); return; }
  foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c == cam);
  gt.SendMessage("Update"); lt.Apply();
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; lr.targetHeight = 720; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); cam.Render(); lr.Present(dst); gt.ApplyTo(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + file, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(file + (lr.presentMaterial ? "[" + lr.presentMaterial.name + "]" : "") + "; ");
  lr.targetHeight = h0; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); };
string[][] V = { new[] { "Cam_Close_house", "house" }, new[] { "Cam_B_Game", "street" }, new[] { "Cam_GarageIn", "garage" } };
bool s0 = lt.realtimeShadows, p0 = lt.painterly;
lt.painterly = false; lt.realtimeShadows = true; foreach (var v in V) Cap(v[0], v[1] + "_on.png");
lt.realtimeShadows = false; foreach (var v in V) Cap(v[0], v[1] + "_off.png");
lt.realtimeShadows = true; lt.painterly = true; foreach (var v in V) Cap(v[0], v[1] + "_paint.png");
lt.realtimeShadows = s0; lt.painterly = p0; lt.Apply();
var a = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; log.Append(" rp " + a.name + " shDist " + a.GetType().GetProperty("shadowDistance").GetValue(a) + " res " + a.GetType().GetProperty("mainLightShadowmapResolution").GetValue(a) + " r " + lt.painterlyRadius);
foreach (var go in made) UnityEngine.Object.Destroy(go);
foreach (var c in cams) if (c && c.GetComponent<LowResRenderTest>()) c.gameObject.SetActive(c.name == "GameCam"); dst.Release();
return log.ToString();
