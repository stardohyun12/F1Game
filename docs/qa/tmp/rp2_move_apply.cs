// 2300 검증: 길 그리기 켜기 → seam2_tomo_camo 가운데 매듭 3 m 옆으로 → Enter 와 같은 Apply(길 재빌드 + 길가 소품 바뀐 길만)
RoadPaintMode.SetOn(true);
var root = GameObject.Find("RoadPaint (길 그리기 — 씬에 저장 안 됨)").transform;
var c = root.Cast<Transform>().First(t => t.name.EndsWith("|seam2_tomo_camo")).GetComponent<UnityEngine.Splines.SplineContainer>();
var sp = c.Spline; int i = sp.Count / 2;
Vector3 a = sp[i - 1].Position, b = sp[i + 1].Position, p = sp[i].Position;
var side = Vector3.Cross(Vector3.up, b - a).normalized * 3f;
Undo.RecordObject(c, "move knot");
var k = sp[i]; k.Position = p + side; sp.SetKnot(i, k);
var log = RoadPaintMode.Apply();
return $"moved {c.name} knot {i}/{sp.Count} ({p.x:0.0},{p.z:0.0}) +3m\n" + log;
