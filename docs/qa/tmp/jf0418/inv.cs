var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder(); sb.AppendLine(sc.path + " dirty=" + sc.isDirty + " play=" + UnityEngine.Application.isPlaying);
var top = GameObject.Find("TownV6_Gray").transform; var v2 = top.Find("RoadsV2");
var terr = top.Find("TownV6_Terrain"); sb.AppendLine("terrain mesh: " + UnityEditor.AssetDatabase.GetAssetPath(terr.GetComponent<MeshFilter>().sharedMesh) + " / " + terr.GetComponent<MeshFilter>().sharedMesh.name + " col=" + UnityEditor.AssetDatabase.GetAssetPath(terr.GetComponent<MeshCollider>().sharedMesh) + "/" + terr.GetComponent<MeshCollider>().sharedMesh.name);
var byAsset = new System.Collections.Generic.Dictionary<string, int>(); int n = 0;
foreach (Transform c in v2)
{
    n++; var mf = c.GetComponent<MeshFilter>();
    string p = mf == null ? "(nomf)" : (mf.sharedMesh == null ? "(null)" : UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh)); if (p == "") p = "(scene-embedded)";
    string k = c.name.Substring(0, Mathf.Min(9, c.name.Length)) + " | " + p; byAsset[k] = byAsset.GetValueOrDefault(k) + 1;
    var mc = c.GetComponent<MeshCollider>();
    if (mc != null && mc.sharedMesh != null) { string q = UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh); if (q == "") q = "(scene-embedded)"; string k2 = "  col " + c.name.Substring(0, Mathf.Min(9, c.name.Length)) + " | " + q; byAsset[k2] = byAsset.GetValueOrDefault(k2) + 1; }
}
sb.AppendLine("RoadsV2 children " + n); foreach (var kv in byAsset) sb.AppendLine(kv.Key + " = " + kv.Value);
foreach (Transform c in top) sb.AppendLine("top: " + c.name + " ch=" + c.childCount + " active=" + c.gameObject.activeSelf);
return sb.ToString();
