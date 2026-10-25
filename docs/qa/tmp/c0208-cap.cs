// 과제 0208: 차 Visual 기준 Blender 와 같은 정사영 카메라로 캡처 + 차만 그린 마스크. SessionState "c.tag" = 파일 접두.
string tag = UnityEditor.SessionState.GetString("c.tag", "x");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/car-look-v1"));
System.IO.Directory.CreateDirectory(outDir);
var race = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
Transform vis = null;
foreach (Transform c in race.transform) if (c.gameObject.activeInHierarchy && (c.name.StartsWith("CarVisual_") || c.name == "FormulaGameplayVisual")) vis = c;
var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional);
bool shaded = Physics.Raycast(vis.position + Vector3.up * 0.8f, -sun.transform.forward, 200f);
var hits = Physics.RaycastAll(vis.position + Vector3.up * 0.8f, -sun.transform.forward, 200f).Where(h => !h.collider.transform.IsChildOf(race.transform)).Select(h => h.collider.name).ToList();
shaded = hits.Count > 0;
var go = new GameObject("CapCam"); var cam = go.AddComponent<Camera>();
var mainCam = Camera.main;
bool post = UnityEditor.SessionState.GetBool("c.post", true);
var ad = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
bool postOn = post && mainCam != null && UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(mainCam).renderPostProcessing;
cam.orthographic = true; cam.orthographicSize = 5.1f * 0.5f * 540f / 960f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 300f;
var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 1; cam.targetTexture = rt;
var rends = vis.GetComponentsInChildren<Renderer>().ToList();
var layers = rends.Select(r => r.gameObject.layer).ToList();
var res = new List<string>();
foreach (var view in new[] { ("threequarter", new Vector3(-5f, 3.9f, 7f)), ("overlay", new Vector3(-5f, 3.5f, -7f)) })
{
  var target = vis.TransformPoint(new Vector3(0f, 0.47f, 0.12f) / vis.localScale.x);
  go.transform.position = vis.position + vis.rotation * view.Item2;
  go.transform.rotation = Quaternion.LookRotation(target - go.transform.position, Vector3.up);
  foreach (var pass in new[] { "", "_mask" })
  {
    ad.renderPostProcessing = pass == "" && postOn;
    if (pass == "_mask") { foreach (var r in rends) r.gameObject.layer = 31; cam.cullingMask = 1 << 31; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.magenta; }
    else { cam.cullingMask = ~(1 << 31); cam.clearFlags = CameraClearFlags.Skybox; }
    cam.Render();
    RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, tag + "_" + view.Item1 + pass + ".png"), tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex);
  }
  for (int i = 0; i < rends.Count; i++) rends[i].gameObject.layer = layers[i];
  res.Add(view.Item1);
}
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "cap " + tag + " vis=" + vis.name + " pos=" + vis.position.ToString("F1") + " sunShadowed=" + shaded + "[" + string.Join("/", hits) + "] post=" + postOn + " " + string.Join(",", res);
