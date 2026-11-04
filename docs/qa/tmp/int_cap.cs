// PlayerPrefs "intcap" = 저장 파일 이름(확장자 없이). 카메라 각도·방·E 대상 같이 남김
Time.timeScale = 1f;
var cam = Camera.main;
var dio = cam.GetComponent<RoomDiorama>();
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var g = typeof(OnFootDirector).GetField("gaze", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(d) as Gaze;
string n = PlayerPrefs.GetString("intcap");
var hs = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 30f);
System.Array.Sort(hs, (x, y) => x.distance.CompareTo(y.distance));
string hits = "";
for (int i = 0; i < Mathf.Min(4, hs.Length); i++) { var rr = hs[i].collider.GetComponent<Renderer>(); hits += hs[i].collider.name + "@" + hs[i].distance.ToString("F1") + "(" + (rr != null && rr.sharedMaterial != null ? rr.sharedMaterial.shader.name : "-") + ") "; }
ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/interior-all/" + n + ".png");
return n + " fixed=" + RoomDiorama.Fixed + " room=" + dio.GetType().GetProperty("RoomNow")?.GetValue(dio)
  + " camRot=" + cam.transform.eulerAngles.ToString("F1") + " cam=" + cam.transform.position.ToString("F2")
  + " player=" + cam.GetComponent<OnFootCamera>().target.position.ToString("F2") + " ts=" + Time.timeScale + " target=" + (g != null ? g.Target : "-") + " hits=" + hits;
