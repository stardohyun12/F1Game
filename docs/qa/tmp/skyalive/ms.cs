// Play: 5초 프레임 시간(측정기 없음) → SessionState "skyalive_ms___TAG__" (lookg/ms.cs 와 같은 재는 법)
Application.runInBackground = true;
string tag = "__TAG__";
var dts = new System.Collections.Generic.List<float>(); float t0 = Time.realtimeSinceStartup + 0.5f; int lastF = Time.frameCount;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    if (Time.frameCount == lastF) return; lastF = Time.frameCount;
    if (Time.realtimeSinceStartup < t0) return;
    dts.Add(Time.unscaledDeltaTime * 1000f);
    if (Time.realtimeSinceStartup > t0 + 5f)
    {
        dts.Sort(); UnityEditor.SessionState.SetString("skyalive_ms_" + tag, "n=" + dts.Count + " med=" + dts[dts.Count / 2].ToString("F1") + " p95=" + dts[(int)(dts.Count * .95f)].ToString("F1"));
        UnityEditor.EditorApplication.update -= cb;
    }
};
UnityEditor.EditorApplication.update += cb;
return "ms start " + tag;
