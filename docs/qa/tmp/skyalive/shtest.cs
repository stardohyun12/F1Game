// Play: 땅 구름 그림자 확인 — 오후 팔레트 그림자 세기 __W__ 덮는 양 __C__ 로 잠시 바꿔 적용(복구는 W=0.30 C=0.42)
var look = SunsetLook.Find();
look.afternoon.skyClouds = new Vector4(__C__f, 0.30f, 0.02f, __W__f);
look.dusk = 0f; SunsetLook.DebugOldSky = false; look.Apply();
var L = RenderSettings.sun;
return "clouds=" + Shader.GetGlobalVector("_PCSkyClouds") + " mainLight=" + (L ? L.name + " dirToLight=" + (-L.transform.forward).ToString("F2") + " int=" + L.intensity : "null");
