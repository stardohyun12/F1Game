// 2253: 2147 규칙으로 다시 지어 「전」 측정용(같은 측정 스크립트). 측정 뒤 run.cs 로 새 규칙 다시.
ParcelBuild.OldSink2147 = true;
var r = ParcelBuild.Run(); ParcelBuild.OldSink2147 = false;
int i = r.IndexOf("해시"); return i >= 0 ? r.Substring(i, 14) : r.Substring(0, Mathf.Min(200, r.Length));
