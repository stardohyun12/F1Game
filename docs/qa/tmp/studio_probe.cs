// artlive 1534: Scene_A7_Light 설정 조사(읽기만, 저장 안 함)
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A7_Light.unity");
var log = new System.Text.StringBuilder();
foreach (var g in sc.GetRootGameObjects()) log.Append("ROOT " + g.name + " [" + string.Join(",", g.GetComponents<Component>().Select(c => c.GetType().Name)) + "] kids " + g.transform.childCount + "\n");
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); log.Append("A7LT on " + lt.name + " " + EditorJsonUtility.ToJson(lt) + "\n");
var gt = UnityEngine.Object.FindFirstObjectByType<A6dGrainToggles>(); if (gt) log.Append("GRAIN " + EditorJsonUtility.ToJson(gt) + "\n");
log.Append("fog " + RenderSettings.fog + " " + RenderSettings.fogMode + " " + RenderSettings.fogColor + " " + RenderSettings.fogStartDistance + "-" + RenderSettings.fogEndDistance + " ambient " + RenderSettings.ambientMode + " " + RenderSettings.ambientLight + " skybox " + (RenderSettings.skybox ? AssetDatabase.GetAssetPath(RenderSettings.skybox) : "null") + "\n");
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var lr = c.GetComponent<LowResRenderTest>(); log.Append("CAM " + c.name + " act " + c.gameObject.activeSelf + " clear " + c.clearFlags + " bg " + c.backgroundColor + " fov " + c.fieldOfView + " far " + c.farClipPlane + (lr ? " LR " + EditorJsonUtility.ToJson(lr) : "") + "\n"); }
var mats = new System.Collections.Generic.HashSet<string>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) { var n = r.name.ToLower(); if (n.Contains("road") || n.Contains("ground") || n.Contains("grass") || n.Contains("dirt") || n.Contains("sky") || n.Contains("street") || n.Contains("yard")) foreach (var m in r.sharedMaterials) if (m) mats.Add(r.name + " -> " + AssetDatabase.GetAssetPath(m) + " sh " + m.shader.name + " tex " + (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") ? m.GetTexture("_BaseMap").name : "-") + " st " + (m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap").ToString() : "")); }
foreach (var s in mats.Take(25)) log.Append("MAT " + s + "\n");
var v = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>(); if (v) log.Append("VOLUME " + v.name + " profile " + AssetDatabase.GetAssetPath(v.sharedProfile) + "\n");
return log.ToString();
