var sb = new System.Text.StringBuilder();
var man = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("D:/Github/F1Game/art/export/env-first/manifest.json"));
foreach (var b in man["buildings"]) {
  string n = (string)b["name"];
  var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_TmpEnv/" + n + ".fbx");
  var go = UnityEngine.Object.Instantiate(asset); go.hideFlags = HideFlags.HideAndDontSave;
  Bounds bb = default; bool first = true; int zero = 0;
  foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds); }
  foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) { if (!mf.name.EndsWith("_BODY")) continue; foreach (var v in mf.sharedMesh.vertices) { var p = go.transform.InverseTransformPoint(mf.transform.TransformPoint(v)); if (Mathf.Abs(p.y) < .005f) zero++; } }
  sb.AppendLine(n + " rootScale=" + asset.transform.localScale + " bounds y " + (bb.min.y).ToString("F3") + ".." + (bb.max.y).ToString("F3") + " size " + bb.size.ToString("F2") + " bodyBottomPts=" + zero + " y=" + (float)b["y"]);
  UnityEngine.Object.DestroyImmediate(go);
}
return sb.ToString();
