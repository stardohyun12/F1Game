var s = BuildBayLoop.RoadNetV2Build(150, -130, 330, -20, 15);
var keep = new System.Collections.Generic.List<string>();
foreach (var l in s.Split('\n')) if (l.StartsWith("DBG") || l.Contains("ridge_up_c") || l.StartsWith("검사") || l.StartsWith("그래프") || l.StartsWith("지형") || l.StartsWith("띠에") || l.StartsWith("군더더기") || l.StartsWith("겹친")) keep.Add(l.Length > 260 ? l.Substring(0, 260) : l);
return string.Join("\n", keep);
