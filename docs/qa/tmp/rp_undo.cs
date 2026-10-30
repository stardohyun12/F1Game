// roadpaint 검증: 켜기 → 매듭 이동(Undo 기록) → PerformUndo → 원위치 확인, 종류 바꾸기도 같은 방식
RoadPaintMode.SetOn(true);
var root = GameObject.Find("RoadPaint (길 그리기 — 씬에 저장 안 됨)").transform;
var c = root.Cast<Transform>().First(t => t.name.EndsWith("|seam2_tomo_camo")).GetComponent<UnityEngine.Splines.SplineContainer>();
var sp = c.Spline; int i = 2; Vector3 p0 = sp[i].Position; string n0 = c.name;
Undo.IncrementCurrentGroup();
Undo.RecordObject(c, "move knot"); var k = sp[i]; k.Position = p0 + new Vector3(4, 0, 0); sp.SetKnot(i, k);
Undo.IncrementCurrentGroup();
Undo.RecordObject(c.gameObject, "kind"); c.gameObject.name = "main|6|seam2_tomo_camo";
Undo.IncrementCurrentGroup();
Vector3 p1 = sp[i].Position; string n1 = c.name;
Undo.PerformUndo(); string n2 = c.name;
Undo.PerformUndo(); Vector3 p2 = c.Spline[i].Position;
return $"knot {p0} -> {p1} -> undo {p2} (back={Vector3.Distance(p0, p2) < .001f}); name {n0} -> {n1} -> undo {n2} (back={n0 == n2})";
