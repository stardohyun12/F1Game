// Play: 앉은 상태 메인 카메라 RT 그대로 한 장 + RT 크기 로그 + 엔진 소리 상태 + 깜빡임 8초 시작(sunset2)
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset2/";
System.IO.Directory.CreateDirectory(dir);
var cam = Camera.main; var rt = cam.targetTexture; var lr = cam.GetComponent<LowResRenderTest>();
var look = SunsetLook.Find();
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
string info = "state=" + d.State + " seated=" + spot.Seated + " sunset=" + look.IsOn
    + " main=" + cam.name + " rt=" + (rt ? rt.width + "x" + rt.height + " filter=" + rt.filterMode : "none")
    + " renderScale=" + (urp ? urp.renderScale.ToString("F2") : "?")
    + " lrt.RenderSize=" + (lr ? lr.RenderSize.ToString() : "-")
    + " screen=" + Screen.width + "x" + Screen.height
    + " rot=" + cam.transform.eulerAngles.ToString("F0") + " OnFootMute=" + EngineAudio.OnFootMute;
foreach (var ea in UnityEngine.Object.FindObjectsByType<EngineAudio>(FindObjectsSortMode.None))
{ var s = ea.GetComponent<AudioSource>(); info += " | engine " + ea.name + " playing=" + s.isPlaying + " mute=" + s.mute + " vol=" + s.volume.ToString("F2"); }
cam.Render();
var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = null;
System.IO.File.WriteAllBytes(dir + "seated_v2_rt.png", tex.EncodeToPNG());
System.IO.File.WriteAllText(dir + "rt_log.txt", info + "\n");
FrameFlickerProbe.Begin(8f, dir, "seated_v2");
return info;
