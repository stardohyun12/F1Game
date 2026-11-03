var sb = new System.Text.StringBuilder();
sb.Append("compiling=" + UnityEditor.EditorApplication.isCompiling);
foreach (var tn in new[] { "SunsetLook", "FishingSpot", "CoastAmbience", "PitCrewSunsetMenu" })
{
    System.Type t = null;
    foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { t = a.GetType(tn); if (t != null) break; }
    sb.Append(" " + tn + "=" + (t != null));
}
foreach (var sn in new[] { "PitCrew/Look/DuskSky", "PitCrew/Look/DuskSea", "PitCrew/LofiPilot/Surface" })
{
    var sh = Shader.Find(sn);
    sb.Append(" | " + sn + "=" + (sh != null) + (sh != null ? " err=" + UnityEditor.ShaderUtil.ShaderHasError(sh) : ""));
}
return sb.ToString();
