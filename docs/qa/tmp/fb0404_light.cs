var sb = new System.Text.StringBuilder();
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
  sb.Append("Light " + l.name + " " + l.type + " en " + l.enabled + "/" + l.gameObject.activeInHierarchy + " int " + l.intensity + " col " + l.color + " rot " + l.transform.eulerAngles + " shadows " + l.shadows + " bias " + l.shadowBias + " nbias " + l.shadowNormalBias + " str " + l.shadowStrength + "\n");
sb.Append("ambient " + RenderSettings.ambientMode + " " + RenderSettings.ambientLight + " sky " + RenderSettings.ambientSkyColor + " fog " + RenderSettings.fog + " " + RenderSettings.fogMode + " " + RenderSettings.fogColor + " skybox " + (RenderSettings.skybox ? RenderSettings.skybox.name : "null") + " sun " + (RenderSettings.sun ? RenderSettings.sun.name : "null") + "\n");
foreach (var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None)) {
  sb.Append("Volume " + v.name + " global " + v.isGlobal + " w " + v.weight + " prof " + (v.sharedProfile ? UnityEditor.AssetDatabase.GetAssetPath(v.sharedProfile) : "null") + ":");
  if (v.sharedProfile) foreach (var c in v.sharedProfile.components) sb.Append(" " + c.GetType().Name + (c.active ? "" : "(off)"));
  sb.Append("\n");
}
var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; sb.Append("RP " + (rp ? rp.name : "null"));
var u = rp as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset; if (u) sb.Append(" shadowDist " + u.shadowDistance + " cascades " + u.shadowCascadeCount + " depthBias " + u.shadowDepthBias + " normalBias " + u.shadowNormalBias + " soft " + u.supportsSoftShadows + " res " + u.mainLightShadowmapResolution);
sb.Append("\n");
foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) sb.Append("Cam " + cam.name + " en " + cam.enabled + " far " + cam.farClipPlane + "\n");
var mats = new System.Collections.Generic.Dictionary<string,int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) foreach (var m in r.sharedMaterials) if (m) { var k = m.shader.name; mats[k] = (mats.ContainsKey(k) ? mats[k] : 0) + 1; }
foreach (var kv in mats) sb.Append("shader " + kv.Key + " x" + kv.Value + "\n");
return sb.ToString();
