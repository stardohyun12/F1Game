// Play 앉은 상태: 메인 카메라 RT 한 장 + 깜빡임·프레임 8초(__TAG__). _Style = __STYLE__, ① 반사 = __REFL__, ② = __SPEC__
var dir = "D:/Github/F1Game/docs/qa/evidence/sunset6/play/"; System.IO.Directory.CreateDirectory(dir);
Application.runInBackground = true;
var cam = Camera.main; var rt = cam.targetTexture; var lr = cam.GetComponent<LowResRenderTest>();
var look = SunsetLook.Find(); if (!look.IsOn) look.SetOn(true);
var m = look.seaMaterial; m.SetFloat("_Style", __STYLE__f);
var sp = m.GetVector("_Spec"); sp.x = __SPEC__f; m.SetVector("_Spec", sp);
var sr = SeaReflection.Find(); sr.enabled = __REFL__ > 0;
var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
string info = "rt=" + (rt ? rt.width + "x" + rt.height + " " + rt.filterMode : "none") + " renderScale=" + (urp ? urp.renderScale.ToString("F2") : "?")
    + " lrt=" + (lr ? lr.RenderSize.ToString() : "-") + " rot=" + cam.transform.eulerAngles.ToString("F0") + " style=" + m.GetFloat("_Style") + " refl=" + sr.enabled + " spec=" + m.GetVector("_Spec").x;
System.IO.File.AppendAllText(dir + "rt_log.txt", "__TAG__ " + info + "\n");
FrameFlickerProbe.Begin(8f, dir, "__TAG__");
return info;
