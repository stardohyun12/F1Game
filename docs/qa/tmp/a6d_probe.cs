// 과제 1726 탐색(저장 안 함): A6c 의 재질별 사용처 — 이름·셰이더·텍스처·_Sat·_Tint·토글 포함 여부·대표 오브젝트
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6c_Town.unity");
var tg = GameObject.Find("SceneA4").GetComponent<SceneA3Toggles>();
var use = new System.Collections.Generic.Dictionary<Material, System.Collections.Generic.List<string>>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
  foreach (var m in r.sharedMaterials) { if (!m) continue; if (!use.ContainsKey(m)) use[m] = new System.Collections.Generic.List<string>(); use[m].Add(r.name); }
var sb = new System.Text.StringBuilder("S " + tg.S1_highlightRolloff + tg.S2_oneLight + tg.S3_softBorders + " T " + tg.T1_sunEdge + tg.T2_shadeAmbient + " toggles " + tg.materials.Length + "\n");
foreach (var kv in use) { var m = kv.Key; var t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
  sb.Append(m.name + " | " + m.shader.name.Replace("PitCrew/", "") + " | " + AssetDatabase.GetAssetPath(m).Replace("Assets/Art/", "") + " | tex " + (t ? t.name + " " + t.width + "x" + t.height : "-")
    + (m.HasProperty("_Sat") ? " sat " + m.GetFloat("_Sat").ToString("F2") : "") + (m.HasProperty("_Tint") ? " tint " + ColorUtility.ToHtmlStringRGB(m.GetColor("_Tint")) : "") + (m.HasProperty("_BaseColor") ? " bc " + ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor")) : "")
    + " tg " + System.Array.IndexOf(tg.materials, m) + " n " + kv.Value.Count + " [" + string.Join(",", kv.Value.GetRange(0, Mathf.Min(4, kv.Value.Count))) + "]\n"); }
return sb.ToString();
