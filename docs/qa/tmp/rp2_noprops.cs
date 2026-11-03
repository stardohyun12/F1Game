// 2300 검증: noProps 에 seam2_tomo_camo 넣고 바뀐 길만 → 그 길 소품 0, 나머지 그대로. 그다음 파일 지우고 다시 → 원래대로
var f = new RoadPaintFile(); f.noProps.Add("seam2_tomo_camo"); RoadPaintData.Save(f);
var r1 = RoadsideBuild.Run(true);
var g = GameObject.Find("Roadside").transform.Cast<Transform>().Where(t => t.name.Contains("seam2_tomo_camo")).Select(t => t.name + "=" + (t.childCount - 1)).ToList();
var j1 = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/roadside_last.json"));
System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/roadside_noprops.json"), j1);
System.IO.File.Delete(RoadPaintData.FilePath);
var r2 = RoadsideBuild.Run(true);
return "off: " + r1 + "\n  " + string.Join(", ", g) + "\non: " + r2;
