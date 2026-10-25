// D-137 하루 빛 4종 × 같은 지점 3곳 → docs/qa/evidence/bayloop-mood/<시간대>_<지점>.png
UnityEditor.SessionState.SetString("bl.outdir", "bayloop-mood");
UnityEditor.SessionState.SetString("bl.moodlog", "");
var moods = new[] { ("1morning", MoodSwitcher.PathMorning), ("2afternoon", MoodSwitcher.PathAfternoon), ("3sunset", MoodSwitcher.PathSunset), ("4dusk", MoodSwitcher.PathDusk) };
var spots = new[] { "main|255|02_shop_climb", "main|712|04_hilltop_downhill", "main|1215|06_coastal" };
var sb = new System.Text.StringBuilder();
foreach (var (tag, path) in moods)
{
    sb.Append("mood|" + path + ";");
    foreach (var s in spots) { var p = s.Split('|'); sb.Append(p[0] + "|" + p[1] + "|" + tag + "_" + p[2] + ";"); }
}
UnityEditor.SessionState.SetString("bl.shot", sb.ToString());
return sb.ToString();
