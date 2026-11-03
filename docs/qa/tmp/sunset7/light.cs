// 실루엣 세기 시험: 노을 빛 세기·높이(실루엣 = 정면이 빛을 덜 받게)
var look = SunsetLook.Find(); look.sunIntensity = __SI__f; look.lightElevation = __LE__f;
UnityEditor.EditorUtility.SetDirty(look);
return "si=" + look.sunIntensity + " le=" + look.lightElevation;
