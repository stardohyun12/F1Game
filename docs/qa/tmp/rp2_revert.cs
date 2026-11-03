// 2300 검증 되돌리기: 길 그리기 끄기 → roads-hand.json 지움(시험 전엔 없었음) → 1판 Apply 와 같은 순서로 재빌드
RoadPaintMode.SetOn(false);
var f = RoadPaintData.FilePath; bool had = System.IO.File.Exists(f); if (had) System.IO.File.Delete(f);
var sw = System.Diagnostics.Stopwatch.StartNew();
BuildBayLoop.RoadNetV2BuildAll(); var t1 = sw.Elapsed.TotalSeconds;
TerraceBlock.Build(TerraceBlock.HouseCenter, 60); var t2 = sw.Elapsed.TotalSeconds;
var r = RoadsideBuild.Run(true);
return $"hand json deleted={had} 길 {t1:F0}s 집블록 {t2 - t1:F0}s\n" + r;
