// 360 프레임 1× 캡처 — LowResRenderTest.Present(도현이 보는 화면 그대로). 이름 = SessionState rf.name
string name = UnityEditor.SessionState.GetString("rf.name", "x");
var lrt = UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>();
var sz = lrt.RenderSize;
var dst = UnityEngine.RenderTexture.GetTemporary(sz.x, sz.y, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
lrt.Present(dst);
var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = dst;
var tex = new UnityEngine.Texture2D(sz.x, sz.y, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, sz.x, sz.y), 0, 0); tex.Apply();
UnityEngine.RenderTexture.active = prev; UnityEngine.RenderTexture.ReleaseTemporary(dst);
System.IO.Directory.CreateDirectory("../docs/qa/evidence/art-room-flat");
System.IO.File.WriteAllBytes("../docs/qa/evidence/art-room-flat/" + name + ".png", tex.EncodeToPNG());
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
var cam = lrt.GetComponent<UnityEngine.Camera>();
return name + " " + sz + " cam=" + cam.transform.position.ToString("F2") + " rot=" + cam.transform.eulerAngles.ToString("F1") + " fov=" + cam.fieldOfView.ToString("F1") + " outline=" + (g.Outline != null) + " drew=" + (g.Outline != null && g.Outline.DrewLastFrame) + " inside=" + (g.Outline != null && g.Outline.CameraInside(cam));
