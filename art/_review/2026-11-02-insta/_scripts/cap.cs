// SessionState insta.name = 파일 이름(번호_설명). 지금 게임 카메라 그대로 4:5 + 1:1.
string nm = UnityEditor.SessionState.GetString("insta.name", "x");
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var r = Still(nm, null, null, 0f);
return r + " state=" + d.State + " prompt='" + d.Prompt + "' cam=" + cam.transform.position.ToString("F1") + " fwd=" + cam.transform.forward.ToString("F2") + " fov=" + cam.fieldOfView;
