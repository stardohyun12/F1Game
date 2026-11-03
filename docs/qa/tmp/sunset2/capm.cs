// 앉은 눈 노을 켬 640x360 여러 방향(1판 자세 151, 2판 180, 왼끝 172, 오른끝 215, 위끝 pitch -12). 끝나면 노을 끔.
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset2/"; System.IO.Directory.CreateDirectory(dir);
string[] tags = { "cam_v1pose_v2", "cam_v2", "edge_left172", "edge_right215" };
float[] yaws = { 151f, 180f, 172f, 215f };
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
look.SetOn(true);
for (int k = 0; k < tags.Length; k++)
{
    c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(3f, yaws[k], 0));
    c.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes(dir + tags[k] + ".png", tex.EncodeToPNG());
}
look.SetOn(false);
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok on=" + look.IsOn + " seatRot=" + seat.eulerAngles;
