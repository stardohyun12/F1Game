// 비탈 위에서 내려다본 바다 후보 — 지형 능선(z≈106)에서 남쪽. 자유 카메라(걷는 시점 아님): 위치는 지형 위 1.7 m.
var sb = new System.Text.StringBuilder();
DebugWindow.SetDusk(1f);
var cands = new[] { (new Vector3(216, 63.7f, 106), 175f, -0.10f), (new Vector3(252, 63.9f, 106), 185f, -0.10f), (new Vector3(192, 61.9f, 106), 165f, -0.12f), (new Vector3(240, 63.2f, 106), 200f, -0.10f) };
int i = 0;
foreach (var c in cands) {
  var dir = Quaternion.Euler(0, c.Item2, 0) * Vector3.forward; dir = (dir + Vector3.up * c.Item3).normalized;
  sb.Append(Still("cand_overlook" + i, c.Item1, c.Item1 + dir * 100f, 52f)); i++;
}
return sb.ToString();
