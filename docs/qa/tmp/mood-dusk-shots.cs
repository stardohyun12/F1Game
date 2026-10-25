// 초저녁만 다시 3장
UnityEditor.SessionState.SetString("bl.outdir", "bayloop-mood");
UnityEditor.SessionState.SetString("bl.moodlog", "");
var s = "mood|" + MoodSwitcher.PathDusk + ";main|255|4dusk_02_shop_climb;main|712|4dusk_04_hilltop_downhill;main|1215|4dusk_06_coastal;";
UnityEditor.SessionState.SetString("bl.shot", s);
return s;
