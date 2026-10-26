// 과제 0601 — Cam_B 하늘 비율: Sky 렌더러만 마젠타로 바꿔 1280×720 렌더, 마젠타 픽셀 수 / 전체. 끝나면 원래 재질로.
var sky = GameObject.Find("Sky").GetComponent<Renderer>(); var old = sky.sharedMaterial;
var m = new Material(old); m.SetTexture("_BaseMap", Texture2D.whiteTexture); m.SetColor("_Tint", Color.magenta); m.SetFloat("_FogMax", 0); sky.sharedMaterial = m;
Camera cam = null; foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c.name == "Cam_B_Game") cam = c;
var bg = cam.backgroundColor; cam.backgroundColor = Color.magenta;
var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; cam.backgroundColor = bg; sky.sharedMaterial = old;
RenderTexture.active = rt; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); RenderTexture.active = null; rt.Release();
int n = 0; foreach (var p in tx.GetPixels32()) if (p.r > 200 && p.g < 60 && p.b > 200) n++;
return $"sky {(100f * n / (1280 * 720)):F1}% pitch {cam.transform.eulerAngles.x:F1}";
