var sb = new System.Text.StringBuilder();
var seen = new System.Collections.Generic.HashSet<Material>();
foreach (var r in GameObject.Find("Sunset_D227").GetComponentsInChildren<Renderer>(true))
{
    var m = r.sharedMaterial; if (m == null || !seen.Add(m)) continue;
    sb.Append(m.name + " path=" + UnityEditor.AssetDatabase.GetAssetPath(m) + " shader=" + m.shader.name);
    foreach (var p in new[] { "_A7Lit", "_LightAmt", "_Ramp", "_R1Ok", "_Lift", "_FogStart", "_FogEnd", "_FogMax", "_S1", "_S2", "_S3", "_Sat" }) if (m.HasProperty(p)) sb.Append(" " + p + "=" + m.GetFloat(p));
    foreach (var p in new[] { "_Ambient", "_Sun", "_FogColor", "_LiftTint", "_WB" }) if (m.HasProperty(p)) sb.Append(" " + p + "=" + m.GetColor(p));
    sb.Append("\n");
}
return sb.ToString();
