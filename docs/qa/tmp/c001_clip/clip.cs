// tools/evidence/clip.cs.tmpl 사본(C-001 자체 확인용, 출력만 docs/qa/tmp/c001_clip/frames, 10초·12fps)
Application.runInBackground = true;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/c001_clip/frames"));
System.IO.Directory.CreateDirectory(outDir);
float secs = 10f, fps = 12f;
int W = 960, H = 540;
var cam = Camera.main;
var lr = cam != null ? cam.GetComponent<LowResRenderTest>() : null;
var rt = new RenderTexture(W, H, 24); var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
int f0 = Time.frameCount, lastF = f0, uniq = 0, n = 0, warn = 0, err = 0;
float t0 = Time.realtimeSinceStartup, next = t0;
var samples = new System.Collections.Generic.List<string>();
Application.LogCallback onLog = (msg, st, type) => {
    if (type == LogType.Warning) warn++;
    else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { err++; }
    else return;
    if (samples.Count < 12) samples.Add(type + ": " + (msg.Length > 160 ? msg.Substring(0, 160) : msg));
};
Application.logMessageReceived += onLog;
UnityEditor.SessionState.SetString("ev.done", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
    bool stop = !Application.isPlaying;
    float now = Time.realtimeSinceStartup;
    if (!stop) {
        int f = Time.frameCount; if (f != lastF) { uniq++; lastF = f; }
        if (now >= next) {
            next += 1f / fps;
            try {
                if (lr != null) lr.Present(rt);
                else if (cam != null) { var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old; }
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "f" + n.ToString("000") + ".png"), tex.EncodeToPNG());
                n++;
            } catch (System.Exception e) { if (samples.Count < 12) samples.Add("CAPTURE: " + e.Message); }
        }
    }
    if (stop || now - t0 >= secs) {
        UnityEditor.EditorApplication.update -= cb;
        Application.logMessageReceived -= onLog;
        rt.Release();
        var sb = new System.Text.StringBuilder("{");
        sb.Append("\"frame_start\":" + f0 + ",\"frame_end\":" + Time.frameCount + ",\"unique_frames\":" + uniq);
        sb.Append(",\"seconds\":" + (now - t0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ",\"captured\":" + n);
        sb.Append(",\"warnings\":" + warn + ",\"errors\":" + err + "}");
        UnityEditor.SessionState.SetString("ev.done", sb.ToString());
    }
};
UnityEditor.EditorApplication.update += cb;
return "clip start frame=" + f0 + " src=" + (lr != null ? "LowRes" : (cam != null ? "Camera" : "NONE"));
