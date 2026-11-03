// Play: 모드(__MODE__) 적용 후 5초 동안 프레임 시간 모음 → SessionState "lookg_ms_<mode>" (중앙값·p95 ms)
Application.runInBackground = true;
var s = SunsetLook.Find(); string mode = "__MODE__";
if (mode == "day") s.SetOn(false); else { s.dusk = mode == "dusk" ? 1f : 0f; if (!s.IsOn) s.SetOn(true); else s.Apply(); }
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
        dts.Sort(); UnityEditor.SessionState.SetString("lookg_ms_" + mode, "n=" + dts.Count + " med=" + dts[dts.Count / 2].ToString("F1") + " p95=" + dts[(int)(dts.Count * .95f)].ToString("F1"));
        UnityEditor.EditorApplication.update -= cb;
    }
};
UnityEditor.EditorApplication.update += cb;
return "ms start " + mode;
