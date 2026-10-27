// 과제 0236-1: 정비소 floor·roof 의 아틀라스 UV 사각형(0~1)
var g = GameObject.Find("SceneA4").transform.Find("garage_v5_combo"); var log = new System.Text.StringBuilder();
foreach (var n in new[] { "floor", "roof", "apron" }) { var mf = g.Find(n).GetComponent<MeshFilter>(); Vector2 a = Vector2.positiveInfinity, b = Vector2.negativeInfinity; foreach (var u in mf.sharedMesh.uv) { a = Vector2.Min(a, u); b = Vector2.Max(b, u); } log.Append(n + " " + a.x.ToString("F4") + " " + a.y.ToString("F4") + " " + b.x.ToString("F4") + " " + b.y.ToString("F4") + "\n"); }
return log.ToString();
