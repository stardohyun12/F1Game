// 과제 2050 탐색 — A6d 씬 구조
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6d_Town.unity");
var log = new System.Text.StringBuilder();
var other = new System.Collections.Generic.Dictionary<string, int>(); var uses = new System.Collections.Generic.Dictionary<string, string>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) foreach (var m in r.sharedMaterials) { if (!m) continue; string p = AssetDatabase.GetAssetPath(m);
  if (!p.Contains("SceneA6d/Mats")) { string k = m.name + " [" + m.shader.name + "] " + p; other[k] = other.ContainsKey(k) ? other[k] + 1 : 1; if (!uses.ContainsKey(k)) uses[k] = r.transform.parent ? r.transform.parent.name + "/" + r.name : r.name; }
  else if (m.name.Contains("garage") || m.name.Contains("stonewall") || m.name.Contains("wall_tile")) { string k = "A6D " + m.name; other[k] = other.ContainsKey(k) ? other[k] + 1 : 1; if (!uses.ContainsKey(k)) uses[k] = r.transform.parent ? r.transform.parent.name + "/" + r.name : r.name; } }
foreach (var kv in other) log.Append(kv.Value + "x " + kv.Key + " e.g. " + uses[kv.Key] + "\n");
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) log.Append("LIGHT " + l.name + " " + l.type + " en " + l.enabled + " act " + l.gameObject.activeInHierarchy + " sh " + l.shadows + " rot " + l.transform.eulerAngles + " col " + l.color + " int " + l.intensity + "\n");
log.Append("ambientMode " + RenderSettings.ambientMode + " amb " + RenderSettings.ambientLight + " fog " + RenderSettings.fog + " sun " + RenderSettings.sun + "\n");
var a = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
log.Append("URP " + AssetDatabase.GetAssetPath(a) + " shDist " + a.shadowDistance + " res " + a.mainLightShadowmapResolution + " casc " + a.shadowCascadeCount + " mainSh " + a.supportsMainLightShadows + " soft " + a.supportsSoftShadows + "\n");
var root = GameObject.Find("SceneA4").transform; foreach (Transform t in root) log.Append("G " + t.name + " (" + t.childCount + ")\n");
var tg = root.GetComponent<SceneA3Toggles>(); log.Append("toggles mats " + tg.materials.Length + " R1 " + tg.R1_stepShade + "\n");
var m0 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA6d/Mats/SA6d_house_shop.mat"); foreach (var pn in new[] { "_Ramp", "_R1Ok", "_LightAmt", "_RampMid", "_RampDark", "_Sat", "_Lift", "_S2", "_FogStart", "_FogEnd", "_FogMax" }) log.Append(pn + "=" + m0.GetFloat(pn) + " "); log.Append("fogC " + m0.GetColor("_FogColor") + " rampSun " + m0.GetVector("_RampSun") + " WB " + m0.GetColor("_WB") + "\n");
foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art/SceneA6d/Mats" })) { var mm = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)); if (mm.HasProperty("_R1Ok") && mm.GetFloat("_R1Ok") < 0.5f) log.Append("R1Ok0 " + mm.name + "; "); }
return log.ToString();
