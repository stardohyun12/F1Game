var sb = new System.Text.StringBuilder();
var pts = new[]{ new Vector3(204.9f,25.6f,-52.4f), new Vector3(217.4f,22.9f,-87.7f) };
var seen = new System.Collections.Generic.HashSet<string>();
foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) {
  foreach (var p in pts) {
    if ((r.bounds.ClosestPoint(p) - p).magnitude > 12f) continue;
    var root = r.transform; while (root.parent != null && root.parent.parent != null && root.parent.name != "TownV6_Gray") root = root.parent;
    foreach (var m in r.sharedMaterials) if (m) { var k = root.name + " | " + m.shader.name + " | " + m.name; if (seen.Add(k)) sb.Append(k + " | shadowCast " + r.shadowCastingMode + "\n"); }
  }
}
var sh = RenderSettings.ambientProbe; sb.Append("ambientProbe L0 r " + sh[0,0] + " g " + sh[1,0] + " b " + sh[2,0] + "\n");
sb.Append("lightingData " + (UnityEditor.Lightmapping.lightingDataAsset ? "yes" : "no") + "\n");
return sb.ToString();
