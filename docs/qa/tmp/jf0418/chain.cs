// 길 그리기 Enter 와 같은 사슬(RoadPaintMode.Apply 389-394): RoadNetV2BuildAll → TerraceBlock.Build → RoadsideBuild.Run(true) → ParcelBuild.Run. 결과 → chain_out.txt
var outp = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/jf0418/chain_out.txt");
System.IO.File.WriteAllText(outp, "running");
var sw = System.Diagnostics.Stopwatch.StartNew(); string a, b, rs, pc;
try
{
    a = BuildBayLoop.RoadNetV2BuildAll(); var t1 = sw.Elapsed.TotalSeconds;
    b = TerraceBlock.Build(TerraceBlock.HouseCenter, 60); var t2 = sw.Elapsed.TotalSeconds;
    rs = RoadsideBuild.Run(true); var t3 = sw.Elapsed.TotalSeconds;
    pc = ParcelBuild.Run();
    var log = $"done {sw.Elapsed.TotalSeconds:F0}s (road {t1:F0} terrace {t2 - t1:F0} roadside {t3 - t2:F0} parcel {sw.Elapsed.TotalSeconds - t3:F0})\n{a}\n----\n{b}\n----\n{rs}\n----\n{pc}";
    System.IO.File.WriteAllText(outp, log);
    return log.Substring(0, Mathf.Min(400, log.Length));
}
catch (System.Exception e) { System.IO.File.WriteAllText(outp, "EXC " + e); return "EXC " + e.Message; }
