var sb = new System.Text.StringBuilder();
sb.Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path).Append("\n");
var c = new Vector3(187.1f, 29.5f, -62.7f);
var seen = new System.Collections.Generic.HashSet<Material>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
  if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
  if (!r.bounds.Intersects(new Bounds(c, new Vector3(9,6,9)))) continue;
  var t = r.transform; string p = t.name; while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
  sb.Append("R ").Append(p).Append(" ext=").Append(r.bounds.size.ToString("F1")).Append("\n");
  foreach (var m in r.sharedMaterials) { if (m == null || !seen.Add(m)) continue;
    var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
    sb.Append("  M ").Append(m.name).Append(" | ").Append(m.shader.name).Append(" | ").Append(UnityEditor.AssetDatabase.GetAssetPath(m));
    if (tex != null) sb.Append(" | tex ").Append(tex.name).Append(" ").Append(tex.width).Append("x").Append(tex.height).Append(" f=").Append(tex.filterMode).Append(" ").Append(UnityEditor.AssetDatabase.GetAssetPath(tex));
    if (m.HasProperty("_BaseMap")) sb.Append(" st=").Append(m.GetTextureScale("_BaseMap"));
    sb.Append("\n"); }
}
return sb.ToString();
