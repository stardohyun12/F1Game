// 2253: farchk 에 나온 점들을 FillSunk 탐침으로 — 빌더가 본 S·y·del. (ParcelBuild.Run 을 한 번 더 돌린다 — 결과는 같은 규칙)
var pts = new System.Collections.Generic.List<Vector2> { new Vector2(-114, 199), new Vector2(-119, 199), new Vector2(-278, 241), new Vector2(-50, 145), new Vector2(-89, 35), new Vector2(-146, 28), new Vector2(-49, 59), new Vector2(54, -31), new Vector2(-60, 44) };
ParcelBuild.ProbePts = pts; ParcelBuild.OldSink2147 = false;
var r = ParcelBuild.Run(); ParcelBuild.ProbePts = null;
return ParcelBuild.ProbeOut.ToString();
