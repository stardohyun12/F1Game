// 과제 0311: (160,24,14) 못오름 자리 — 위에서 내려 쏜 첫 충돌(이름 첫 글자·높이) 격자 + Walls_0 가 길 위에 있는 칸
var sb = new System.Text.StringBuilder();
for (int z = 24; z >= 6; z -= 2) { sb.Append("\nz" + z + ": "); for (int x = 148; x <= 174; x += 2) { if (Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out var h, 400, ~0, QueryTriggerInteraction.Ignore)) sb.Append(h.collider.name.Substring(0, 1) + h.point.y.ToString("F0") + " "); else sb.Append("--- "); } }
return sb.ToString();
