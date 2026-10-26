// 2111 (Play, h0404-lapprep 직후 + bl-drive 시작 직후): 8초 연속 프레임. SessionState "l5c.tag" = before|after|after360.
// before = 5b 설정(스냅 켬·밉맵 끔·Nearest·MSAA 1), after = 5c 기본, after360 = 5c + 640×360. 30fps 고정(captureFramerate), 1초 가속 뒤 240장, 화면과 같은 확대(LowResRenderTest.Present)로 960×540.
var tag = UnityEditor.SessionState.GetString("l5c.tag", "after");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/look5c/frames_" + tag));
System.IO.Directory.CreateDirectory(outDir);
var c = UnityEngine.Object.FindFirstObjectByType<PCLookScene>();
bool before = tag == "before";
c.vertexSnap = before; c.mipmaps = !before; c.smoothUpscale = !before; c.msaa = before ? 1 : 4; c.lowResHeight = tag == "after360" ? 360 : 270;
c.Tick();
var lr = Camera.main.GetComponent<LowResRenderTest>();
Time.captureFramerate = 30;
var rt = new RenderTexture(960, 540, 0); var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
int lastF = Time.frameCount, n = 0, skip = 0, dup = 0, gaps = 0;
UnityEditor.SessionState.SetString("l5c.done", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    int f = Time.frameCount;
    if (f == lastF) { dup++; return; }
    if (f - lastF > 1) gaps += f - lastF - 1;
    lastF = f;
    if (skip < 30) { skip++; return; }
    lr.Present(rt);
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "f" + n.ToString("000") + ".png"), tex.EncodeToPNG());
    n++;
    if (n >= 240)
    {
        UnityEditor.EditorApplication.update -= cb; Time.captureFramerate = 0; rt.Release();
        UnityEditor.SessionState.SetString("l5c.done", tag + " frames=" + n + " gaps=" + gaps + " lr=" + lr.RenderSize + " smooth=" + lr.smooth + " msaa=" + lr.msaa);
    }
};
UnityEditor.EditorApplication.update += cb;
return "capturing " + tag + " lr=" + lr.RenderSize;
