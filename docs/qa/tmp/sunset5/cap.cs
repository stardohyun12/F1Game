// 편집 모드: 앉은 자리 카메라(PITCH,YAW) 640x360. 판 = A(_Style 0) / B(_Style 1) / day(노을 끔) / nogrid(격자 끈 B) — 같은 카메라·같은 순간
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset5/"; System.IO.Directory.CreateDirectory(dir);
string[] vers = { __VERS__ };
float yaw = __YAW__f, pitch = __PITCH__f; string pre = "__PRE__";
var look = SunsetLook.Find(); var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var go = new GameObject("_tmpC"); var c = go.AddComponent<Camera>(); c.CopyFrom(Camera.main); c.fieldOfView = 60; c.farClipPlane = 1800; c.nearClipPlane = 0.05f;
c.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(pitch, yaw, 0));
var rt = new RenderTexture(640, 360, 24); rt.filterMode = FilterMode.Point; c.targetTexture = rt;
var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
var m = look.seaMaterial; float style0 = m.GetFloat("_Style"); var disp0 = m.GetVector("_Disp");
string log = "";
foreach (var ver in vers)
{
    look.SetOn(ver != "day");
    if (ver == "A") m.SetFloat("_Style", 0); else m.SetFloat("_Style", 1);
    if (ver == "nogrid") { m.SetVector("_Disp", new Vector4(0, disp0.y, disp0.z, disp0.w)); look.seaNear.enabled = false; }
    c.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes(dir + pre + "_" + ver + ".png", tex.EncodeToPNG());
    m.SetVector("_Disp", disp0); m.SetFloat("_Style", style0);
    log += ver + "(near=" + look.seaNear.enabled + ") ";
}
look.SetOn(false);
RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
return "ok " + log + " on=" + look.IsOn + " seaMat=" + look.sea.sharedMaterial.name + " near=" + look.seaNear.enabled;
