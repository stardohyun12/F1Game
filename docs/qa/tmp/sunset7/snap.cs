// Play: 지난 프레임 화면(LowResRenderTest.Present 거침)을 raw/__TAG__.png 로. 설정 바꾼 exec 다음에 따로 부른다.
var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
var rt = new RenderTexture(lr.RenderSize.x, lr.RenderSize.y, 0); lr.Present(rt);
var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = a;
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/sunset7/raw/__TAG__.png", tex.EncodeToPNG());
rt.Release(); UnityEngine.Object.DestroyImmediate(tex);
return "ok __TAG__";
