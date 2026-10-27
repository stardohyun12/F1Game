// 과제 1145 — P1 에서 차가 z -18.5 에 멈춘 원인: 그 자리 앞쪽 콜라이더
var sb = new System.Text.StringBuilder(); var o = new Vector3(1.0f, -1.5f, -18.6f);
foreach (var c in Physics.OverlapBox(o + new Vector3(0, 0.6f, -1.5f), new Vector3(1.2f, 0.6f, 3f))) if (!c.GetComponentInParent<ArcadeCarController>()) sb.Append("overlap " + c.name + " (" + c.GetType().Name + ") b" + c.bounds.center.ToString("F1") + "/" + c.bounds.size.ToString("F1") + "; ");
for (float z = -14; z >= -30; z -= 1) { var hs = Physics.RaycastAll(new Vector3(1, 20, z), Vector3.down, 60); string s = ""; foreach (var h in hs) s += h.collider.name + "@" + h.point.y.ToString("F2") + " "; sb.Append("\nz" + z + ": " + s); }
return sb.ToString();
