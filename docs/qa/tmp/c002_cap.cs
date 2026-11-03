// C-002 확인용 한 장 캡처(도트 UI 포함 = 도현 Game 화면): SessionState c002.cap 에 파일 이름
string name = UnityEditor.SessionState.GetString("c002.cap", "c002_shot");
var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
var rt = new RenderTexture(960, 540, 24); var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
lr.Present(rt); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
string p = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/" + name + ".png"));
System.IO.File.WriteAllBytes(p, tex.EncodeToPNG()); rt.Release();
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return p + " prompt=" + d.Prompt + " step=" + Opening.Step + " cam=" + cam.transform.position.ToString("F2") + " fwd=" + cam.transform.forward.ToString("F2");
