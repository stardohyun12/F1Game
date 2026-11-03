var r = BuildBayLoop.RoadNetV2BuildAll();
System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/p1443_build.log"), r);
var sb = new System.Text.StringBuilder();
foreach (var line in r.Split('\n')) if (line.Contains("문 앞") || line.Contains("⏱") || line.Contains("건물 판") || line.Contains("길 밖") || line.Contains("틈") || line.Contains("겹침")) sb.AppendLine(line);
return sb.ToString();
