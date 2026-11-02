// 전표(Tab) 장면 — 도트 UI 는 가로 640 기준 배치라 360 폭에선 전표가 잘린다. 16:9 의 가로 640 으로 그린 뒤 왼쪽 360 열(=1080 px)만 쓴다(전표가 왼쪽 아래).
string nm = UnityEditor.SessionState.GetString("insta.name", "x");
var ui = UnityEngine.Object.FindFirstObjectByType<DotScreenUI>(); ui.SimSlip = true;
Texture2D Left(Texture2D t, int keep) { var px = t.GetPixels(0, 0, keep, t.height); var o = new Texture2D(keep, t.height, TextureFormat.RGB24, false); o.SetPixels(px); o.Apply(); UnityEngine.Object.DestroyImmediate(t); return o; }
Save(Left(Grab(640, 450, 3, null, null, 0f), 1080), "stills/" + nm + "_1080x1350.png");
Save(Left(Grab(640, 360, 3, null, null, 0f), 1080), "stills/" + nm + "_1080x1080.png");
ui.SimSlip = false;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return nm + " state=" + d.State + " cam=" + cam.transform.position.ToString("F1") + " fwd=" + cam.transform.forward.ToString("F2");
