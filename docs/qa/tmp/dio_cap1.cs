// 저해상 RT(=1×) 를 그대로 PNG 로. 인자: 파일 이름은 아래 name 을 바꿔 쓴다
var name = "1-room";
var cam = UnityEngine.Camera.main;
var rt = cam.targetTexture;
if (rt == null) return "no rt";
cam.Render();
var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(rt.width, rt.height, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
UnityEngine.RenderTexture.active = prev;
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../docs/qa/evidence/room-diorama"));
System.IO.Directory.CreateDirectory(dir);
System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
UnityEngine.Object.Destroy(tex);
return dir + "/" + name + ".png " + rt.width + "x" + rt.height + " active=" + RoomDiorama.Active;
