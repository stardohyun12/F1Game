// 무드 조사: 열린 씬, 렌더러 머티리얼을 셰이더별로, 동기화 대상 여부, 라이트, MoodController·Volume
var sb = new System.Text.StringBuilder();
sb.Append("scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " playing " + Application.isPlaying + "\n");
var byShader = new Dictionary<string, HashSet<string>>(); var cnt = new Dictionary<string, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    foreach (var m in r.sharedMaterials)
    {
        if (m == null) continue;
        bool sync = m.HasProperty("_FogColor") && m.HasProperty("_Ambient") && m.HasProperty("_FogStart") && m.HasProperty("_FogEnd");
        string k = m.shader.name + (sync ? " [sync]" : " [NO SYNC]");
        if (!byShader.ContainsKey(k)) { byShader[k] = new HashSet<string>(); cnt[k] = 0; }
        byShader[k].Add(m.name); cnt[k]++;
    }
foreach (var kv in byShader.OrderBy(k => k.Key)) sb.Append(kv.Key + " x" + cnt[kv.Key] + ": " + string.Join(", ", kv.Value.Take(40)) + "\n");
foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("light " + l.name + " " + l.type + " " + l.color + " " + l.intensity + " rot " + l.transform.eulerAngles + "\n");
foreach (var mc in UnityEngine.Object.FindObjectsByType<MoodController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("MoodController on " + mc.name + "\n");
foreach (var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("volume " + v.name + " global " + v.isGlobal + " prof " + (v.sharedProfile ? v.sharedProfile.name : "-") + "\n");
sb.Append("skybox " + (RenderSettings.skybox ? RenderSettings.skybox.name + " / " + RenderSettings.skybox.shader.name : "-") + " fog " + RenderSettings.fog + " " + RenderSettings.fogColor + " amb " + RenderSettings.ambientLight + "\n");
return sb.ToString();
