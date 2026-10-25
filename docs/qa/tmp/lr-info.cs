var rp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
var q=QualitySettings.renderPipeline; var d=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
var cam=Camera.main; string ci="-";
if(cam!=null){ var ad=cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ci=cam.name+" pp="+(ad!=null?ad.renderPostProcessing.ToString():"?")+" aa="+(ad!=null?ad.antialiasing.ToString():"?")+" target="+(cam.targetTexture!=null); }
var canv=""; foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if(c.isRootCanvas) canv+=c.name+":"+c.renderMode+" ";
return "current="+(rp!=null?rp.name:"null")+" quality="+(q!=null?q.name:"null")+" default="+(d!=null?d.name:"null")+" qlevel="+QualitySettings.names[QualitySettings.GetQualityLevel()]+" renderScale="+(rp!=null?rp.renderScale:0)+" upscale="+(rp!=null?rp.upscalingFilter.ToString():"-")+" screen="+Screen.width+"x"+Screen.height+" cam="+ci+" canvases="+canv;
