// 0301 되돌리기: 길 그리기 끄기 → roads-hand.json 지움(시험 전엔 없었음) → Enter 와 같은 4단 재빌드(필지 포함)
ParcelBuild.OldPath0301 = false;
RoadPaintMode.SetOn(false);
var f = RoadPaintData.FilePath; bool had = System.IO.File.Exists(f); if (had) System.IO.File.Delete(f);
var sw = System.Diagnostics.Stopwatch.StartNew();
BuildBayLoop.RoadNetV2BuildAll(); var t1 = sw.Elapsed.TotalSeconds;
TerraceBlock.Build(TerraceBlock.HouseCenter, 60); var t2 = sw.Elapsed.TotalSeconds;
var r = RoadsideBuild.Run(true); var t3 = sw.Elapsed.TotalSeconds;
var pc = ParcelBuild.OnEnter ? ParcelBuild.Run() : "(off)";
return $"hand json deleted={had} 길 {t1:F1}s 집블록 {t2 - t1:F1}s 소품 {t3 - t2:F1}s 필지 {sw.Elapsed.TotalSeconds - t3:F1}s\n" + r + "\n" + pc.Substring(0, System.Math.Min(200, pc.Length));
