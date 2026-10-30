// roadpaint 검증 2: 기존 길(seam2_tomo_camo) 가운데 매듭 하나를 옆으로 3 m 옮긴다 (Undo 기록 = 손 편집과 같은 경로)
var root = GameObject.Find("RoadPaint (길 그리기 — 씬에 저장 안 됨)").transform;
var c = root.Cast<Transform>().First(t => t.name.EndsWith("|seam2_tomo_camo")).GetComponent<UnityEngine.Splines.SplineContainer>();
var sp = c.Spline; int i = sp.Count / 2;
Vector3 a = sp[i - 1].Position, b = sp[i + 1].Position, p = sp[i].Position;
var side = Vector3.Cross(Vector3.up, b - a).normalized * 3f;
Undo.RecordObject(c, "move knot");
var k = sp[i]; k.Position = p + side; sp.SetKnot(i, k);
SessionState.SetString("rp_move_at", $"{p.x + side.x:0.0} {p.z + side.z:0.0}");
return $"moved {c.name} knot {i}/{sp.Count} ({p.x:0.0},{p.z:0.0}) -> ({p.x + side.x:0.0},{p.z + side.z:0.0})\n" + RoadPaintMode.Violations();
