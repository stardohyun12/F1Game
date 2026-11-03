// C-001 측정용: 3초 동안 초당 프레임(클립 없이). 결과는 SessionState "fps".
int f0 = Time.frameCount; float t0 = Time.realtimeSinceStartup;
UnityEditor.SessionState.SetString("fps", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => { float t = Time.realtimeSinceStartup - t0; if (t >= 3f) { UnityEditor.EditorApplication.update -= cb; UnityEditor.SessionState.SetString("fps", ((Time.frameCount - f0) / t).ToString("F1") + " fps, vsync=" + QualitySettings.vSyncCount + " target=" + Application.targetFrameRate); } };
UnityEditor.EditorApplication.update += cb;
return "measuring";
