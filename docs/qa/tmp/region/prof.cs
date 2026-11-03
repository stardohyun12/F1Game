// 0301 프로파일: 편집 없이 Enter 와 같은 4단 실행, 단계별 시간
var dir = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/region/");
var sw = System.Diagnostics.Stopwatch.StartNew();
var a = BuildBayLoop.RoadNetV2BuildAll(); var t1 = sw.Elapsed.TotalSeconds;
var b = TerraceBlock.Build(TerraceBlock.HouseCenter, 60); var t2 = sw.Elapsed.TotalSeconds;
var rs = RoadsideBuild.Run(true); var t3 = sw.Elapsed.TotalSeconds;
var pc = ParcelBuild.OnEnter ? ParcelBuild.Run() : "(off)"; var t4 = sw.Elapsed.TotalSeconds;
var s = $"road {t1:F1} terrace {t2 - t1:F1} roadside {t3 - t2:F1} parcel {t4 - t3:F1} total {t4:F1}\n--road--\n{a}\n--terrace--\n{b}\n--roadside--\n{rs}\n--parcel--\n{pc}";
System.IO.File.WriteAllText(dir + "prof.txt", s);
return s.Substring(0, System.Math.Min(400, s.Length));
