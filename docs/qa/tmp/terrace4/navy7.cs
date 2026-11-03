// 남색 판 7: Renderer 아닌 그리는 것 — Terrain·DecalProjector·VisualEffect·LensFlare·Volume, 그리고 Default 레이어 GameObject 중 렌더러 없는 컴포넌트 종류
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.None))sb.AppendLine("Terrain "+Path(t.transform)+" active="+t.gameObject.activeInHierarchy+" en="+t.enabled+" pos="+t.transform.position.ToString("F0")+" size="+(t.terrainData?t.terrainData.size.ToString("F0"):"-")+" layer="+t.gameObject.layer);
var kinds=new System.Collections.Generic.Dictionary<string,int>();
foreach(var c in UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None)){if(!c||!c.isActiveAndEnabled)continue;var n=c.GetType().FullName;if(n.StartsWith("UnityEngine.Rendering")||n.Contains("Decal")||n.Contains("Flare")||n.Contains("VFX")||n.Contains("Sky")||n.Contains("Cloud")||n.Contains("Water")||n.Contains("Volume")){sb.AppendLine("beh "+n+" @ "+Path(c.transform));}}
var rp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;sb.AppendLine("RP="+(rp?rp.name+" "+UnityEditor.AssetDatabase.GetAssetPath(rp):"builtin"));
sb.AppendLine("skybox="+(RenderSettings.skybox?RenderSettings.skybox.name+" "+RenderSettings.skybox.shader.name:"null"));
return sb.ToString();
