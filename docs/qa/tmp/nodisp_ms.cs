// 프레임 ms 측정 2단계. SessionState "nd" = "MODE|x" (A 빈카메라 켬 / B 끔). 1회차: 설정+시작 기록, 2회차: 평균 ms 계산.
Application.runInBackground = true; Time.timeScale = 1f;
var dcg = GameObject.Find("LowResRenderTest_DisplayCam"); var dc = dcg != null ? dcg.GetComponent<Camera>() : null;
string st = UnityEditor.SessionState.GetString("msStart", "");
if (st == "")
{
    string mode = UnityEditor.SessionState.GetString("nd", "A|x").Split('|')[0];
    if (dc != null) dc.enabled = (mode == "A");
    UnityEditor.SessionState.SetString("msStart", Time.frameCount + "|" + Time.realtimeSinceStartup.ToString("R") + "|" + mode);
    return "started " + mode;
}
var p = st.Split('|'); int f0 = int.Parse(p[0]); double t0 = double.Parse(p[1]);
UnityEditor.SessionState.SetString("msStart", "");
int n = Time.frameCount - f0; double dt = Time.realtimeSinceStartup - t0;
if (dc != null) dc.enabled = true;
return "mode=" + p[2] + " frames=" + n + " secs=" + dt.ToString("F2") + " avgMs=" + (1000.0 * dt / n).ToString("F3");
