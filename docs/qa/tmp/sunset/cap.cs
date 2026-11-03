// Cam_Sunset 를 360 줄(640x360, 점 확대 전 원본)로 노을 끔/켬 두 장. 끝나면 끔으로 되돌림.
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset/"; System.IO.Directory.CreateDirectory(dir);
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
c.transform.SetPositionAndRotation(seat.position, seat.rotation);
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
var big = new Texture2D(1280, 720 * 2, TextureFormat.RGB24, false);
string[] names = { "cam_sunset_off", "cam_sunset_on" };
for (int i = 0; i < 2; i++)
{
    look.SetOn(i == 1);
    c.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes(dir + names[i] + ".png", tex.EncodeToPNG());
    var px = tex.GetPixels32();
    for (int y = 0; y < 720; y++) for (int x = 0; x < 1280; x++) big.SetPixel(x, 720 * (1 - i) + y, px[(y / 2) * 640 + x / 2]);
}
big.Apply(); System.IO.File.WriteAllBytes(dir + "cam_sunset_off_on_2x.png", big.EncodeToPNG());
look.SetOn(false);
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok on=" + look.IsOn;
