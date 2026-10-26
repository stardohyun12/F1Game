// 1548: 씬 렌더러 셰이더 분포(머티리얼 수·렌더러 수) + 차·사람 셰이더
var d = new Dictionary<string, (int r, HashSet<string> m)>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
    foreach (var m in r.sharedMaterials)
    {
        if (m == null) continue; var k = m.shader.name;
        if (!d.ContainsKey(k)) d[k] = (0, new HashSet<string>()); var e = d[k]; e.r++; e.m.Add(m.name); d[k] = e;
    }
var sb = new System.Text.StringBuilder(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\n");
foreach (var kv in d.OrderByDescending(x => x.Value.r)) sb.Append(kv.Key + " rend " + kv.Value.r + " mats " + kv.Value.m.Count + " e.g. " + string.Join(",", kv.Value.m.Take(4)) + "\n");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
if (car) foreach (var r in car.GetComponentsInChildren<Renderer>().Take(6)) sb.Append("CAR " + r.name + " " + string.Join(",", r.sharedMaterials.Where(m => m).Select(m => m.name + "/" + m.shader.name)) + "\n");
sb.Append("sky " + (RenderSettings.skybox ? RenderSettings.skybox.name + "/" + RenderSettings.skybox.shader.name : "-") + " pipeline " + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name : "builtin"));
return sb.ToString();
