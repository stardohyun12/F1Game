// Play: 오후(dusk 0)에서 9판/10판 스위치가 화면을 바꾸면 안 된다 — 전역 값만 확인(그림 비교는 pair 로)
var l = SunsetLook.Find(); l.dusk = __D__f; l.v9 = __V9__; l.Apply();
var P = l.Now();
return "dusk=" + l.dusk + " v9=" + l.v9 + " sil=" + Shader.GetGlobalVector("_PCDuskSil") + " pf=" + P.seaPathFocus + " az=" + P.sunAzimuth + " halo=" + P.sunHalo;
