// 점프 순간 — SimJump 호출 뒤 N 프레임 지나 현재 게임 카메라로 찍는다(EditorApplication.update 로 기다림). 결과는 SessionState insta.jumpres.
string nm = UnityEditor.SessionState.GetString("insta.name", "x");
int wait = int.Parse(UnityEditor.SessionState.GetString("insta.wait", "14"));
var pl = UnityEngine.Object.FindFirstObjectByType<OnFootPlayer>();
float y0 = pl.transform.position.y;
pl.SimJump();
int f0 = Time.frameCount; bool done = false;
UnityEditor.SessionState.SetString("insta.jumpres", "pending");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
  if (done || Time.frameCount - f0 < wait) return;
  done = true; UnityEditor.EditorApplication.update -= cb;
  var s = Still(nm, null, null, 0f);
  UnityEditor.SessionState.SetString("insta.jumpres", "y0=" + y0.ToString("F2") + " y=" + pl.transform.position.y.ToString("F2") + " cam=" + cam.transform.position.ToString("F1") + " " + s);
};
UnityEditor.EditorApplication.update += cb;
return "scheduled y0=" + y0.ToString("F2");
