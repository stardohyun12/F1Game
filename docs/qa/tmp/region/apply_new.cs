// 0301 검증 2: 새 길로 같은 편집 다시 적용(매듭은 안 움직임 — roads-hand.json 그대로) → Enter 시간 측정
ParcelBuild.OldPath0301 = false;
var sw = System.Diagnostics.Stopwatch.StartNew();
var log = RoadPaintMode.Apply();
return $"NEW apply {sw.Elapsed.TotalSeconds:F1}s · on={System.IO.File.Exists(RoadPaintData.FilePath)}\n" + log.Substring(0, System.Math.Min(300, log.Length));
