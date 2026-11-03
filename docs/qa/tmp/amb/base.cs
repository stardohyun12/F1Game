// 비교용 '예전 소리': AmbienceZones 를 끄고 예전 설정으로 되돌린 뒤 같은 자리 3곳을 잰다(Play 중에만, 씬엔 저장 안 됨)
Application.runInBackground = true;
var z = AmbienceZones.Active; if (z == null) return "no zones";
z.enabled = false;
z.wind.zoneGain = -1f;
var w = z.wind.GetComponent<AudioSource>(); w.spatialBlend = 0.3f; var wl = w.GetComponent<AudioLowPassFilter>(); if (wl) wl.enabled = false;
var v = z.coast.waves; v.rolloffMode = AudioRolloffMode.Logarithmic; v.minDistance = 12f; v.maxDistance = 90f; v.volume = 0.55f; v.dopplerLevel = 1f; v.transform.position = new Vector3(7f, 0.2f, -311.5f);
var vl = v.GetComponent<AudioLowPassFilter>(); if (vl) vl.enabled = false;
var g = z.coast.gulls; g.volume = 1f;
// Places 는 AmbienceZones.Active 를 찾는다 — 꺼도 Active 는 OnDisable 에서 비므로 잠깐 되살린다
typeof(AmbienceZones).GetProperty("Active").SetValue(null, z);
var spots = new[] {
  new AmbienceProbe.Spot("예전_방", 0, Vector3.zero, 0),
  new AmbienceProbe.Spot("예전_집 앞 골목", 1, Vector3.zero, 0),
  new AmbienceProbe.Spot("예전_부두 끝", 5, Vector3.zero, 0),
};
return AmbienceProbe.Places("D:/Github/F1Game/docs/qa/evidence/ambience-zones/before", spots);
