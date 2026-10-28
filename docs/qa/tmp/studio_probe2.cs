// artlive 1534: A7 하늘·길·마당 메시/재질 값(읽기만)
var log = new System.Text.StringBuilder();
foreach (var nm in new[] { "Sky", "yard_ground", "Road" }) {
  var r = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(x => x.name == nm); var mf = r.GetComponent<MeshFilter>(); var m = r.sharedMaterial;
  log.Append(nm + " path " + AnimationUtility.CalculateTransformPath(r.transform, null) + " pos " + r.transform.position.ToString("F1") + " rot " + r.transform.eulerAngles.ToString("F0") + " scl " + r.transform.lossyScale.ToString("F2") + " b " + r.bounds.size.ToString("F1") + " mesh " + (mf ? mf.sharedMesh.name + " " + AssetDatabase.GetAssetPath(mf.sharedMesh) + " v" + mf.sharedMesh.vertexCount : "-") + "\n");
  var uv = mf.sharedMesh.uv; if (uv.Length > 0) log.Append("  uv range " + new Vector2(uv.Min(q => q.x), uv.Min(q => q.y)) + ".." + new Vector2(uv.Max(q => q.x), uv.Max(q => q.y)) + "\n");
  foreach (var p in new[] { "_Tint", "_FogColor" }) if (m.HasProperty(p)) log.Append("  " + p + " " + m.GetColor(p) + "\n");
  foreach (var p in new[] { "_A7Lit", "_Sat", "_FogStart", "_FogEnd", "_FogMax", "_HasR2" }) if (m.HasProperty(p)) log.Append("  " + p + " " + m.GetFloat(p) + "\n");
  var t = m.GetTexture("_BaseMap"); log.Append("  tex " + AssetDatabase.GetAssetPath(t) + " " + t.width + "x" + t.height + " " + t.filterMode + " wrap " + t.wrapMode + "\n");
}
var cam = GameObject.Find("GameCam").GetComponent<Camera>(); log.Append("GameCam pos " + cam.transform.position.ToString("F1") + " rot " + cam.transform.eulerAngles.ToString("F0") + " near " + cam.nearClipPlane + "\n");
return log.ToString();
