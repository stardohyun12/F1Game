// 칩 겹침 확인: SessionState pc.chip = "<place idx>|<파일명>" → 이동 → 40프레임 뒤 Game 뷰 캡처(칩 포함)
Application.runInBackground=true;
var a=UnityEditor.SessionState.GetString("pc.chip","12|chip").Split('|');
var dir=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var outDir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../docs/qa/evidence/pc2305/")); System.IO.Directory.CreateDirectory(outDir);
DebugWindow.GoPlace(int.Parse(a[0]));
int w=0; UnityEditor.SessionState.SetString("pc.chipdone","");
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(!Application.isPlaying){ UnityEditor.EditorApplication.update-=cb; return; }
 if(++w==60){ ScreenCapture.CaptureScreenshot(outDir+a[1]+".png"); }
 if(w==70){ UnityEditor.EditorApplication.update-=cb; UnityEditor.SessionState.SetString("pc.chipdone","prompt='"+dir.Prompt+"' state="+dir.State+" gaze="+dir.GazeTarget+" anchor="+dir.PromptAnchor); } };
UnityEditor.EditorApplication.update+=cb; return "go";
