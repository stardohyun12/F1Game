// 실제 게임 뷰에서 컴포넌트를 켜고 해상도별 프레임 시간(unscaledDeltaTime 평균, 30프레임 버린 뒤 120프레임). 끝나면 270 상태로 캡처하고 컴포넌트를 끈다.
Application.runInBackground=true;
var t=System.Type.GetType("LowResRenderTest, PitCrew"); var cam=Camera.main; var comp=cam.GetComponent(t) as Behaviour; var fH=t.GetField("targetHeight");
int[] hs={0,1080,540,360,270}; int stage=0, frames=0; double acc=0; int lastFrame=Time.frameCount; var res=new System.Text.StringBuilder(); bool captured=false;
UnityEditor.SessionState.EraseString("qalr");
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(Time.frameCount==lastFrame) return; lastFrame=Time.frameCount;
 if(frames==0){ if(stage>=hs.Length){
    if(!captured){ captured=true; fH.SetValue(comp,270); comp.enabled=true; frames=-20; return; }
    UnityEditor.EditorApplication.update-=cb; comp.enabled=false; UnityEditor.SessionState.SetString("qalr",res.ToString()); return; }
  int h=hs[stage]; if(h==0) comp.enabled=false; else { fH.SetValue(comp,h); comp.enabled=true; } }
 frames++;
 if(frames==-1){ UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/lowres-480-gameview-ui.png"); frames=-1000; return; }
 if(frames==-995){ UnityEditor.EditorApplication.update-=cb; comp.enabled=false; UnityEditor.SessionState.SetString("qalr",res.ToString()); return; }
 if(frames<0) return;
 if(frames>30) acc+=Time.unscaledDeltaTime;
 if(frames>=150){ res.Append((hs[stage]==0?"off(현재 0.5)":hs[stage].ToString())+"="+(acc/120*1000).ToString("F2")+"ms ; "); acc=0; frames=0; stage++; } };
UnityEditor.EditorApplication.update+=cb; return "started";
