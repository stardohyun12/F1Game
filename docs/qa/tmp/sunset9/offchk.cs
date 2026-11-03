// Play: 노을 끄기 → 판자·갈매기 사라짐, 갈매기 울음 주도권(leader) 풀림 확인
var l = SunsetLook.Find(); l.SetOn(false);
var amb = UnityEngine.Object.FindFirstObjectByType<CoastAmbience>();
var fg = UnityEngine.Object.FindFirstObjectByType<DuskForeground>();
return "on=" + l.IsOn + " backdrop=" + l.backdrop.activeInHierarchy + " leaderNull=" + (amb.leader == null) + " fgActiveInScene=" + (fg != null);
