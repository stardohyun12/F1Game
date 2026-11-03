// Play 앉은 상태: v9 = __V9__, 물 시간 풀고 게임 시각 0.5 s 마다 21장(0~10 s, Present 640×360) → raw/__TAG___00..20.png + __TAG__.jsonl
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset10/raw/";
Application.runInBackground = true;
var look = SunsetLook.Find(); look.dusk = 1f; look.v9 = __V9__; look.Apply();
Shader.SetGlobalVector("_PCSeaClock", Vector4.zero);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(FishingSpot).GetField("pitch", bf).SetValue(spot, 0f);
typeof(FishingSpot).GetField("yaw", bf).SetValue(spot, 0f);
float t0 = Time.time + 0.3f; int k = 0; var log = new System.Text.StringBuilder();
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.time < t0 + k * 0.5f) return;
    var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
    int w = lr.RenderSize.x, h = lr.RenderSize.y;
    var rt = new RenderTexture(w, h, 0); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
    lr.Present(rt);
    var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
    System.IO.File.WriteAllBytes(dir + "__TAG___" + k.ToString("00") + ".png", tex.EncodeToPNG()); rt.Release(); UnityEngine.Object.DestroyImmediate(tex);
    log.Append("{\"k\":" + k + ",\"t\":" + (Time.time - t0).ToString("F2") + ",\"frame\":" + Time.frameCount + "}\n");
    k++;
    if (k >= 21) { UnityEditor.EditorApplication.update -= cb; System.IO.File.WriteAllText(dir + "__TAG__.jsonl", log.ToString()); }
};
UnityEditor.EditorApplication.update += cb;
return "started __TAG__ v9=" + look.v9;
