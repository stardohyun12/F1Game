var sw = System.Diagnostics.Stopwatch.StartNew();
var r = BuildBayLoop.BuildScene();
var c = BuildBayLoop.Check();
return r + "\n--- check (" + sw.Elapsed.TotalSeconds.ToString("F0") + "s)\n" + c;
