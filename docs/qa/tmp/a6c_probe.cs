// 과제 1704 탐색(저장 안 함): A6 의 정비소 재질 vs h2 집 재질 — 셰이더·키워드·float/color 값 차이
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6_Town.unity");
var sb = new System.Text.StringBuilder(); var garage = new System.Collections.Generic.Dictionary<Material, string>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  string path = r.name; var p = r.transform.parent; while (p) { path = p.name + "/" + path; p = p.parent; }
  if (!path.ToLower().Contains("garage") && !path.ToLower().Contains("v5")) continue;
  foreach (var m in r.sharedMaterials) if (m && !garage.ContainsKey(m)) garage[m] = path; }
foreach (var kv in garage) sb.Append("G " + kv.Key.name + " [" + AssetDatabase.GetAssetPath(kv.Key) + "] sh " + kv.Key.shader.name + " @ " + kv.Value + "\n");
var house = GameObject.Find("SceneA4").transform.Find("A4b_Houses/house_shop").GetComponentInChildren<MeshRenderer>().sharedMaterial;
sb.Append("H " + house.name + " sh " + house.shader.name + "\n");
Material gm = null; foreach (var k in garage.Keys) if (k.shader == house.shader) { gm = k; break; }
if (!gm) { foreach (var k in garage.Keys) { gm = k; break; } }
if (gm) { sb.Append("cmp garage " + gm.name + " vs house " + house.name + " (same shader " + (gm.shader == house.shader) + ")\n kw G: " + string.Join(",", gm.shaderKeywords) + " | H: " + string.Join(",", house.shaderKeywords) + "\n");
  var s = gm.shader; for (int i = 0; i < s.GetPropertyCount(); i++) { var n = s.GetPropertyName(i); var t = s.GetPropertyType(i); string a = "", b = "";
    if (t == UnityEngine.Rendering.ShaderPropertyType.Float || t == UnityEngine.Rendering.ShaderPropertyType.Range) { a = gm.GetFloat(n).ToString("F3"); b = house.HasProperty(n) ? house.GetFloat(n).ToString("F3") : "-"; }
    else if (t == UnityEngine.Rendering.ShaderPropertyType.Color || t == UnityEngine.Rendering.ShaderPropertyType.Vector) { a = gm.GetVector(n).ToString("F3"); b = house.HasProperty(n) ? house.GetVector(n).ToString("F3") : "-"; }
    else continue; sb.Append(" " + n + " G " + a + (a != b ? " H " + b + " <<" : "") + "\n"); } }
var tg = GameObject.Find("SceneA4").GetComponent<SceneA3Toggles>(); sb.Append("house in toggles " + System.Array.IndexOf(tg.materials, house) + " garage in toggles " + (gm ? System.Array.IndexOf(tg.materials, gm) : -9));
return sb.ToString();
