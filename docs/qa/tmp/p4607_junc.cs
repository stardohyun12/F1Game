// 과제 0622: 갈림목(287,-63) 근처 길 중심선 높이 — 읽기만
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { var c = r.centerline; if (c == null) continue;
  for (int i = 0; i < c.Length; i++) if (Mathf.Abs(c[i].x - 287f) < 5f && Mathf.Abs(c[i].z + 63f) < 5f) sb.Append(r.name + " [" + i + "/" + c.Length + "] " + c[i].ToString("F2") + " w " + r.width + "\n"); }
return sb.ToString();
