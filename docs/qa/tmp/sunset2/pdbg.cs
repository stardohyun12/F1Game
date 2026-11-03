// Play: 메인 카메라 RT(실제 화면) 그대로 한 장. _Debug 켜고/끄고. 로그: RT 크기
var dir = "D:/Github/F1Game/docs/qa/tmp/sunset2/";
var cam = Camera.main; var rt = cam.targetTexture; var lr = cam.GetComponent<LowResRenderTest>();
var look = SunsetLook.Find();
string info = "main=" + cam.name + " rt=" + (rt ? rt.width + "x" + rt.height + " filter=" + rt.filterMode : "none") + " lrt=" + (lr ? lr.RenderSize.ToString() : "-") + " screen=" + Screen.width + "x" + Screen.height + " pos=" + cam.transform.position.ToString("F1") + " rot=" + cam.transform.eulerAngles.ToString("F0") + " sunset=" + look.IsOn;
string[] tags = { "play_dbg", "play_on" };
for (int k = 0; k < 2; k++)
{
    look.seaMaterial.SetFloat("_Debug", k == 0 ? 1 : 0);
    cam.Render();
    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(dir + tags[k] + ".png", tex.EncodeToPNG());
}
return info;
