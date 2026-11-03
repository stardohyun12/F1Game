// Play: 바다 원인 찾기 — 반사 __REFL__, 차가운 물 섞음 __COOL__, 시선 yaw 0
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>(); typeof(FishingSpot).GetField("yaw", bf).SetValue(spot, 0f);
SeaReflection.Find().enabled = __REFL__ > 0;
var m = SunsetLook.Find().seaMaterial; var c = m.GetVector("_Cool"); c.x = __COOL__f; m.SetVector("_Cool", c);
return "refl=" + SeaReflection.Find().enabled + " cool=" + m.GetVector("_Cool") + " glint=" + m.GetFloat("_Glint") + " cuts=" + m.GetVector("_GlintCuts") + " sunEl=" + SunsetLook.Find().skySunElevation;
