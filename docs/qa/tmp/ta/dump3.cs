var sb = new System.Text.StringBuilder();
int n = 0;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  if (t.name.IndexOf("Walk", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("patch", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("fix", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Fill", System.StringComparison.Ordinal) >= 0) {
    if (n++ > 60) break;
    var mf = t.GetComponent<MeshFilter>(); var r = t.GetComponent<Renderer>();
    sb.Append(WorldSnapshot.Path(t)).Append(t.gameObject.activeInHierarchy ? "" : " [off]").Append(" pos=").Append(t.position.ToString("F1"));
    if (mf && mf.sharedMesh) sb.Append(" mesh=").Append(UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh)).Append(" v=").Append(mf.sharedMesh.vertexCount);
    if (r) sb.Append(" b=").Append(r.bounds.center.ToString("F0")).Append("/").Append(r.bounds.size.ToString("F0"));
    sb.Append('\n');
  }
}
var tr = GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<Renderer>(); sb.Append("terrain bounds ").Append(tr.bounds.min.ToString("F0")).Append(" .. ").Append(tr.bounds.max.ToString("F0")).Append('\n');
var sea = GameObject.Find("TownV6_Gray/Sea"); sb.Append("sea ").Append(sea.transform.position.ToString("F1")).Append(" col=").Append(sea.GetComponent<Collider>() != null).Append(" b=").Append(sea.GetComponent<Renderer>().bounds.size.ToString("F0")).Append('\n');
int rs = 0, rsOn = 0; foreach (var s in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { rs++; if (s.gameObject.activeInHierarchy) rsOn++; }
sb.Append("RoadSurface ").Append(rs).Append(" active ").Append(rsOn).Append('\n');
var kinds = new System.Collections.Generic.Dictionary<string,int>();
foreach (var s in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { string k = (s.transform.parent ? s.transform.parent.name : "-") + ":" + s.kind; kinds[k] = kinds.TryGetValue(k, out var q) ? q + 1 : 1; }
foreach (var kv in kinds) sb.Append("  ").Append(kv.Key).Append(" ").Append(kv.Value).Append('\n');
return sb.ToString();
