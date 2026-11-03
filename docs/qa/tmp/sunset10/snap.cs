// Play 앉은 상태: v9 = __V9__ 로 바꾸고 시선 yaw __YAW__, 3프레임 뒤 Present 화면(640×360) → raw/__TAG__.png + 해 화면 위치
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset10/raw/"; System.IO.Directory.CreateDirectory(dir);
Application.runInBackground = true;
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
look.dusk = 1f; look.v9 = __V9__; look.Apply();
Shader.SetGlobalVector("_PCSeaClock", new Vector4(__FZ__, 120f, 0, 0));   // 1 = 물 시간 고정(같은 물로 9/10 비교), 0 = 풀림
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(FishingSpot).GetField("pitch", bf).SetValue(spot, 0f);
typeof(FishingSpot).GetField("yaw", bf).SetValue(spot, Mathf.Clamp(__YAW__f, -spot.yawLeft, spot.yawRange));
int fr = Time.frameCount;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.frameCount < fr + 3) return;
    UnityEditor.EditorApplication.update -= cb;
    var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
    int w = lr.RenderSize.x, h = lr.RenderSize.y;
    var rt = new RenderTexture(w, h, 0); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
    lr.Present(rt);
    var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
    System.IO.File.WriteAllBytes(dir + "__TAG__.png", tex.EncodeToPNG()); rt.Release(); UnityEngine.Object.DestroyImmediate(tex);
    var P = look.Now();
    float az = P.sunAzimuth * Mathf.Deg2Rad, el = P.skySunElevation * Mathf.Deg2Rad;
    var sd = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
    var sv = cam.WorldToViewportPoint(cam.transform.position + sd * 2000f);
    var f = cam.transform.forward; f.y = 0; var hz = cam.WorldToViewportPoint(cam.transform.position + f.normalized * 2000f);
    System.IO.File.AppendAllText(dir + "snap.jsonl", "{\"tag\":\"__TAG__\",\"v9\":" + (look.v9 ? 1 : 0) + ",\"sunX\":" + sv.x.ToString("F3") + ",\"sunY\":" + (1 - sv.y).ToString("F3") + ",\"hzY\":" + (1 - hz.y).ToString("F3")
        + ",\"az\":" + P.sunAzimuth.ToString("F1") + ",\"el\":" + P.skySunElevation.ToString("F2") + ",\"discDeg\":" + P.sunHalo.x.ToString("F2") + ",\"discDiamPx\":" + (P.sunHalo.x * 2f / cam.fieldOfView * h).ToString("F1")
        + ",\"camYaw\":" + cam.transform.eulerAngles.y.ToString("F1") + ",\"fov\":" + cam.fieldOfView.ToString("F0") + "}\n");
};
UnityEditor.EditorApplication.update += cb;
return "ok v9=" + look.v9 + " fr=" + fr;
