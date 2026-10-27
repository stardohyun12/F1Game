// 과제 1525 탐색(저장 안 함): Scene_A4e_Town 렌더러 재질 중 kit-style 대상(소품 png 이름 / bush·mound·clump·rock) — 재질 경로·셰이더·_BaseMap·tiling·렌더러 수
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4e_Town.unity");
string[] P = { "bench_01", "bicycle_01", "cone_01", "cone_02", "drum_01", "drum_02", "drum_03", "fence_01", "fence_02", "pole_01", "pot_01", "pot_02", "tires_01", "tires_02" };
var use = new System.Collections.Generic.Dictionary<Material, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) foreach (var m in r.sharedMaterials) if (m) { use.TryGetValue(m, out int n); use[m] = n + 1; }
var sb = new System.Text.StringBuilder(); var tg = GameObject.Find("SceneA4").GetComponent<SceneA3Toggles>();
foreach (var kv in use) { var m = kv.Key; var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null; string tn = tex ? tex.name : "-"; string ln = m.name.ToLower();
  bool hit = System.Array.IndexOf(P, tn) >= 0 || ln.Contains("bush") || ln.Contains("mound") || ln.Contains("clump") || ln.Contains("rock") || System.Array.Exists(P, p => ln.Contains(p));
  if (!hit) continue;
  sb.Append(m.name + " [" + AssetDatabase.GetAssetPath(m) + "] sh " + m.shader.name + " tex " + tn + " [" + (tex ? AssetDatabase.GetAssetPath(tex) : "") + "] st " + (m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap").ToString("F3") : "") + " n" + kv.Value + " inToggles " + System.Array.IndexOf(tg.materials, m) + "\n"); }
var m0 = new System.Collections.Generic.List<Material>(use.Keys).Find(x => x.name.ToLower().Contains("bush")); if (m0) { sb.Append("props of " + m0.name + ": "); var s = m0.shader; for (int i = 0; i < s.GetPropertyCount(); i++) sb.Append(s.GetPropertyName(i) + ","); }
return sb.ToString();
