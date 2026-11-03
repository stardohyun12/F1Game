var sw = System.Diagnostics.Stopwatch.StartNew();
PitCrewSunsetMenu.BakeSeaDepthMap();
var m = SunsetLook.Find().seaMaterial;
return "bake ms=" + sw.ElapsedMilliseconds + " tex2=" + (m.GetTexture("_DepthTex2") ? m.GetTexture("_DepthTex2").width + "" : "-") + " rect2=" + m.GetVector("_DepthRect2") + " matPath=" + UnityEditor.AssetDatabase.GetAssetPath(m);
