var sb = new System.Text.StringBuilder();
var sh = new[] { "PitCrew/LofiPilot/Surface", "PitCrew/CoastalTown/WorldSurface", "PitCrew/BayLoop/TerrainSlope", "PitCrew/BayLoop/WindGrass", "PitCrew/StreetFurniture/Asphalt" };
var mats = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.shader != null).Distinct().ToList();
sb.Append("scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " mats " + mats.Count + "\n");
var texs = new Dictionary<string, List<string>>();
foreach (var m in mats)
{
    if (!m.HasProperty("_MainTex")) { sb.Append("noMain " + m.name + " " + m.shader.name + "\n"); continue; }
    var t = m.GetTexture("_MainTex"); string tp = t ? AssetDatabase.GetAssetPath(t) : "-";
    string k = m.shader.name.Replace("PitCrew/", "") + " | " + tp + (t ? " " + t.width + "x" + t.height : "");
    if (!texs.ContainsKey(k)) texs[k] = new List<string>(); texs[k].Add(m.name);
}
foreach (var kv in texs.OrderBy(k => k.Key)) sb.Append(kv.Key + " <- " + kv.Value.Count + " " + string.Join(",", kv.Value.Take(4)) + "\n");
return sb.ToString();
