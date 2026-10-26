// 1548: 커스텀 셰이더 머티리얼의 텍스처 크기·밉·필터 (렌더러 수 많은 순)
var d = new Dictionary<Texture, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
    foreach (var m in r.sharedMaterials)
    {
        if (m == null || !m.shader.name.StartsWith("PitCrew")) continue;
        foreach (var pn in m.GetTexturePropertyNames()) { var t = m.GetTexture(pn); if (t == null) continue; d[t] = d.TryGetValue(t, out var c) ? c + 1 : 1; }
    }
var sb = new System.Text.StringBuilder();
foreach (var kv in d.OrderByDescending(x => x.Value).Take(25)) { var t = kv.Key; sb.Append(t.name + " " + t.width + "x" + t.height + " mips " + t.mipmapCount + " " + t.filterMode + " uses " + kv.Value + " " + UnityEditor.AssetDatabase.GetAssetPath(t) + "\n"); }
return sb.ToString();
