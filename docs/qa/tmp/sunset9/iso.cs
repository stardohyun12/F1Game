// Play: 흰 곡선 분리 — 줄 끄기(__LINE__) · 반사 끄기(__REFL__) 후 다음 프레임에 snap 으로 찍는다
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var l = spot.transform.Find("FishingRig/Line").GetComponent<LineRenderer>(); l.enabled = __LINE__ > 0;
var sr = SeaReflection.Find(); sr.enabled = __REFL__ > 0;
return "line=" + l.enabled + " refl=" + sr.enabled + " srMask=" + sr.cullMask.value;
