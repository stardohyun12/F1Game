// Play: 땅 구름 그림자 보기용 높은 자리(오후 새 판) — 60m 위에서 마을 내려다봄
var look = SunsetLook.Find(); look.dusk = 0f; SunsetLook.DebugOldSky = __OLD__; look.Apply();
return CamPin.Pin(new Vector3(-10f, 60f, -83f), new Vector3(28f, 60f, 0f)) + " clouds=" + Shader.GetGlobalVector("_PCSkyClouds");
