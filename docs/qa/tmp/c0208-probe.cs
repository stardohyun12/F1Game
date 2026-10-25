var sb = new System.Text.StringBuilder();
sb.Append("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " playing=" + Application.isPlaying + "\n");
var race = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
foreach (Transform c in race.transform) sb.Append(" child " + c.name + " active=" + c.gameObject.activeSelf + " lp=" + c.localPosition.ToString("F3") + " ls=" + c.localScale.ToString("F3") + "\n");
var seen = new HashSet<Texture>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
  foreach (var m in r.sharedMaterials) { if (m == null || !m.HasProperty("_MainTex")) continue; var t = m.mainTexture as Texture2D; if (t == null || !seen.Add(t) || seen.Count > 14) continue;
    sb.Append(" tex " + t.name + " " + t.width + " " + t.format + " " + t.filterMode + " mips=" + t.mipmapCount + " mat=" + m.name + " sh=" + m.shader.name + "\n"); }
var cm = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BayLoop/Car/Candidate_A.mat");
sb.Append("CandA amb=" + cm.GetColor("_Ambient") + " face=" + cm.GetFloat("_FaceShade") + " tint=" + cm.GetColor("_Tint") + "\n");
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) sb.Append("sun col=" + l.color + " int=" + l.intensity + " rot=" + l.transform.eulerAngles + " shadows=" + l.shadows + "\n");
var mood = UnityEngine.Object.FindFirstObjectByType<MoodController>();
sb.Append("mood=" + (mood != null) + " colorSpace=" + QualitySettings.activeColorSpace);
return sb.ToString();
