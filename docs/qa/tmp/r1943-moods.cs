var sb = new System.Text.StringBuilder();
foreach (var g in AssetDatabase.FindAssets("t:MoodProfile")) {
  var p = AssetDatabase.GUIDToAssetPath(g); var m = AssetDatabase.LoadAssetAtPath<MoodProfile>(p);
  sb.Append(p + " rot" + m.sunRotationEuler + " sun" + m.sunColor + " I" + m.sunIntensity + " sh" + m.shadowStrength + " ambMode " + m.ambientMode + " amb" + m.ambientColor + " fog" + m.fog + " fc" + m.fogColor + " fd" + m.fogDensity + " sync" + m.syncWorld + " fs" + m.fogStart + " fe" + m.fogEnd + " was" + m.worldAmbientScale + " sea" + m.seaTint + " lit" + m.windowsLit + " pp " + (m.postProfile ? m.postProfile.name : "-") + " sky " + (m.skybox ? m.skybox.name : "-") + "\n"); }
var mc = UnityEngine.Object.FindFirstObjectByType<MoodController>();
sb.Append("scene controller " + (mc ? mc.name + " " + (mc.Profile ? mc.Profile.name : "null") : "none") + "\n");
foreach (var t in System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } }).Where(t => t.Name.Contains("DayCycle") || t.Name.Contains("TimeOfDay") || t.Name.Contains("MoodMenu"))) sb.Append("type " + t.FullName + "\n");
return sb.ToString();
