// Play: 카메라를 POSE(cur 또는 x,y,z,pitch,yaw)에 두고 시계 고정(_PCSeaClock=100)으로 한 장 그려 Present 경로 그대로 __TAG__.png
Application.runInBackground = true;
var dir = "D:/Github/F1Game/docs/qa/evidence/look-global/raw/"; System.IO.Directory.CreateDirectory(dir);
var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
string pose = "__POSE__";
if (pose != "cur")
{
    var p = pose.Split(',');
    System.Func<int, float> F = k => float.Parse(p[k], System.Globalization.CultureInfo.InvariantCulture);
    cam.transform.SetPositionAndRotation(new Vector3(F(0), F(1), F(2)), Quaternion.Euler(F(3), F(4), 0f));
}
Shader.SetGlobalVector("_PCSeaClock", new Vector4(1, 100, 0, 0));
var look = SunsetLook.Find(); if (look != null) look.Apply();
var sr = SeaReflection.Find(); if (sr != null && sr.isActiveAndEnabled) sr.RenderFor(cam);
cam.Render();
int w = lr.RenderSize.x, h = lr.RenderSize.y;
var rt = new RenderTexture(w, h, 0); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
lr.Present(rt);
var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
System.IO.File.WriteAllBytes(dir + "__TAG__.png", tex.EncodeToPNG()); rt.Release();
Shader.SetGlobalVector("_PCSeaClock", Vector4.zero);
return "__TAG__ " + w + "x" + h + " cam=" + cam.transform.position.ToString("F2") + " " + cam.transform.eulerAngles.ToString("F1") + " fov=" + cam.fieldOfView + " look=" + (look != null && look.IsOn);
