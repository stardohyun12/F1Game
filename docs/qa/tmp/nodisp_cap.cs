// 사용: 첫 줄 라벨을 바꿔서 실행. Game 뷰 경고 플래그 + 화면 캡처 저장.
Application.runInBackground=true;
string label="LABEL";
var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
var f=t.GetField("m_NoCameraWarning",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
var sb=new System.Text.StringBuilder();
foreach(var o in Resources.FindObjectsOfTypeAll(t)) { ((UnityEditor.EditorWindow)o).Repaint(); sb.Append("noCamWarn="+f.GetValue(o)+" "); }
var tex=ScreenCapture.CaptureScreenshotAsTexture();
System.IO.Directory.CreateDirectory("../docs/qa/evidence/no-display-fix");
System.IO.File.WriteAllBytes("../docs/qa/evidence/no-display-fix/"+label+".png", tex.EncodeToPNG());
UnityEngine.Object.Destroy(tex);
return sb.ToString()+" frame="+Time.frameCount+" t="+Time.realtimeSinceStartup.ToString("F3")+" cams="+Camera.allCamerasCount;
