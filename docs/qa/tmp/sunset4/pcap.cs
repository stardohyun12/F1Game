// Play: 앉은 상태 메인 카메라 RT 한 장 + RT 로그 + 깜빡임 8초(__TAG__). 노을 켬.
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset4/";
var cam = Camera.main; var rt = cam.targetTexture; var lr = cam.GetComponent<LowResRenderTest>();
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
string info = "seated=" + spot.Seated + " sunset=" + look.IsOn + " rt=" + (rt ? rt.width + "x" + rt.height + " filter=" + rt.filterMode : "none")
    + " renderScale=" + (urp ? urp.renderScale.ToString("F2") : "?") + " lrt.RenderSize=" + (lr ? lr.RenderSize.ToString() : "-")
    + " rot=" + cam.transform.eulerAngles.ToString("F0") + " seaMat=" + look.sea.sharedMaterial.name + " shader=" + look.sea.sharedMaterial.shader.name;
if (rt)
{
    cam.Render();
    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(dir + "__TAG___rt.png", tex.EncodeToPNG());
}
System.IO.File.AppendAllText(dir + "rt_log.txt", "__TAG__ " + info + "\n");
FrameFlickerProbe.Begin(8f, dir, "__TAG__");
return info;
