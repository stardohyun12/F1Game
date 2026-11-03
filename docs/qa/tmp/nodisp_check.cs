// Play: SessionState "nd" = "MODE|LABEL" — A(빈 카메라 켬)/B(끔)/R(복구). timeScale 0 에서 화면 캡처(LABEL-MODE.png) + Unity 의 Display1 참조 판정.
Application.runInBackground = true;
var parts = UnityEditor.SessionState.GetString("nd", "A|x").Split('|'); string mode = parts[0], label = parts[1];
var m = typeof(UnityEditor.EditorGUIUtility).GetMethod("IsDisplayReferencedByCameras", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
var dcg = GameObject.Find("LowResRenderTest_DisplayCam");
var dc = dcg != null ? dcg.GetComponent<Camera>() : null;
if (mode == "A") { if (dc != null) dc.enabled = true; Time.timeScale = 0f; }
else if (mode == "B") { if (dc != null) dc.enabled = false; Time.timeScale = 0f; }
else { if (dc != null) dc.enabled = true; Time.timeScale = 1f; return "restored"; }
bool referenced = (bool)m.Invoke(null, new object[] { 0 });
var tex = ScreenCapture.CaptureScreenshotAsTexture();
System.IO.Directory.CreateDirectory("../docs/qa/evidence/no-display-fix");
System.IO.File.WriteAllBytes("../docs/qa/evidence/no-display-fix/" + label + "-" + mode + ".png", tex.EncodeToPNG());
UnityEngine.Object.Destroy(tex);
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return label + " mode=" + mode + " display0Referenced=" + referenced + " warningShown=" + (!referenced) + " dispcam=" + (dc != null && dc.enabled) + " state=" + (d != null ? d.State.ToString() : "-") + " sunset=" + SunsetLook.Find().IsOn + " frame=" + Time.frameCount;
