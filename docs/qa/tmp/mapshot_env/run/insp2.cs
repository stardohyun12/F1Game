var sb = new System.Text.StringBuilder();
var man = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("D:/Github/F1Game/art/export/env-first/manifest.json"));
foreach (var b in man["buildings"]) {
  string n = (string)b["name"]; if (n != "env_camo_204696009_v01" && n != "env_camo_204693578_v01") continue;
  var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_TmpEnv/" + n + ".fbx");
  var go = UnityEngine.Object.Instantiate(asset); go.hideFlags = HideFlags.HideAndDontSave;
  float px = (float)b["pivot_xz"][0], pz = (float)b["pivot_xz"][1];
  sb.AppendLine(n + " pivot " + px + "," + pz + " keys: " + string.Join(",", ((Newtonsoft.Json.Linq.JObject)b).Properties().Select(p => p.Name)));
  sb.Append(" poly(rel):"); foreach (var p in b["poly"]) sb.Append(" (" + ((float)p[0] - px).ToString("F1") + "," + ((float)p[1] - pz).ToString("F1") + ")");
  sb.AppendLine();
  foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) { if (!mf.name.EndsWith("_BODY")) continue; var set = new System.Collections.Generic.List<string>(); float mn = 1e9f; foreach (var v in mf.sharedMesh.vertices) mn = Mathf.Min(mn, go.transform.InverseTransformPoint(mf.transform.TransformPoint(v)).y);
    foreach (var v in mf.sharedMesh.vertices) { var p = go.transform.InverseTransformPoint(mf.transform.TransformPoint(v)); if (p.y < mn + .02f) set.Add("(" + p.x.ToString("F1") + "," + p.z.ToString("F1") + ")"); }
    sb.AppendLine(" body bottom y=" + mn.ToString("F3") + " pts: " + string.Join(" ", set)); }
  UnityEngine.Object.DestroyImmediate(go);
}
return sb.ToString();
