// 0301 검증 1: 옛 길(OldPath0301=true)로 시험 편집 — seam2_tomo_camo 가운데 매듭 3 m 옆 → Enter 와 같은 Apply
ParcelBuild.OldPath0301 = true;
try
{
    RoadPaintMode.SetOn(true);
    var root = GameObject.Find("RoadPaint (길 그리기 — 씬에 저장 안 됨)").transform;
    var c = root.Cast<Transform>().First(t => t.name.EndsWith("|seam2_tomo_camo")).GetComponent<UnityEngine.Splines.SplineContainer>();
    var sp = c.Spline; int i = sp.Count / 2;
    Vector3 a = sp[i - 1].Position, b = sp[i + 1].Position, p = sp[i].Position;
    var side = Vector3.Cross(Vector3.up, b - a).normalized * 3f;
    Undo.RecordObject(c, "move knot");
    var k = sp[i]; k.Position = p + side; sp.SetKnot(i, k);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var log = RoadPaintMode.Apply();
    return $"OLD moved knot {i}/{sp.Count} ({p.x:0.0},{p.z:0.0}) +3m · {sw.Elapsed.TotalSeconds:F1}s\n" + log.Substring(0, System.Math.Min(300, log.Length));
}
finally { ParcelBuild.OldPath0301 = false; }
