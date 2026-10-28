// 지형 그대로, 땅+3.5 m 에서 바다가 보이는 자리 — 방 원래 자리(-116.7,-31.7) 반경 200 m, 5 m 격자
// seacand.cs(D-198) 기반. 바꾼 점: 바다 점은 100 m 이상 떨어진 곳(가까운 지형 구멍 오판 제거), 바다 판정은 Sea 콜라이더 또는 y<0.05 지형만(아무것도 안 맞는 구멍 제외)
var C = new Vector3(-116.7f, 0, -31.7f);
var sb = new System.Text.StringBuilder();
var seaNames = new HashSet<string>();
bool SeaAt(float x, float z) { var hs = Physics.RaycastAll(new Vector3(x, 200, z), Vector3.down, 400); if (hs.Length == 0) return false; var top = hs.OrderBy(h => h.distance).First(); if (top.collider.name.Contains("Sea")) { seaNames.Add(top.collider.name); return true; } return top.point.y < 0.05f && top.collider.name.Contains("Terrain"); }
var targets = new List<(int yaw, Vector3 p)>();
for (int yaw = 0; yaw < 360; yaw += 5) { float s = Mathf.Sin(yaw * Mathf.Deg2Rad), c = Mathf.Cos(yaw * Mathf.Deg2Rad); int k = 0; for (float Dd = 100; Dd < 800 && k < 3; Dd += 5) { float x = C.x + s * Dd, z = C.z + c * Dd; if (SeaAt(x, z)) { targets.Add((yaw, new Vector3(x, 0.5f, z))); k++; Dd += 25; } } }
var cand = new List<(int vis, Vector3 g, float d, Vector3 dir, string yaws)>();
for (float dx = -200; dx <= 200; dx += 5) for (float dz = -200; dz <= 200; dz += 5) {
  if (dx * dx + dz * dz > 40000) continue; float x = C.x + dx, z = C.z + dz;
  if (!Physics.Raycast(new Vector3(x, 200, z), Vector3.down, out var g, 400)) continue;
  if (!g.collider.name.Contains("Terrain") || Vector3.Angle(g.normal, Vector3.up) > 12 || g.point.y < 2f) continue;
  if (Physics.OverlapSphere(g.point + Vector3.up * 1.5f, 3f).Any(cl => cl.name.StartsWith("Walls") || cl.name.StartsWith("ROAD") || cl.name.StartsWith("D198"))) continue;
  var eye = g.point + Vector3.up * 3.5f; int vis = 0; var dir = Vector3.zero; var ys = new SortedSet<int>();
  foreach (var t in targets) if ((t.p - eye).sqrMagnitude > 90 * 90 && !Physics.Linecast(eye, t.p)) { vis++; dir += (t.p - eye).normalized; ys.Add(t.yaw); }
  if (vis > 0) cand.Add((vis, g.point, Mathf.Sqrt(dx * dx + dz * dz), dir.normalized, string.Join(",", ys)));
}
// 상위 3곳, 서로 30 m 이상 떨어지게
var top = new List<(int vis, Vector3 g, float d, Vector3 dir, string yaws)>();
foreach (var c in cand.OrderByDescending(c => c.vis).ThenBy(c => c.d)) { if (top.All(t => (t.g - c.g).magnitude > 30)) top.Add(c); if (top.Count == 3) break; }
System.IO.File.WriteAllLines("D:/Github/F1Game/docs/qa/tmp/hill/cand_all.txt", cand.OrderByDescending(c => c.vis).Select(c => c.vis + " " + c.g.x.ToString("F1") + " " + c.g.z.ToString("F1") + " y" + c.g.y.ToString("F2") + " d" + c.d.ToString("F0") + " yaws " + c.yaws));
sb.Append("targets " + targets.Count + " sea=" + string.Join("/", seaNames) + " cand " + cand.Count + "\n");
// 캡처: 땅+3.5 m, 보이는 바다 평균 방향
var main = Camera.main; var go = new GameObject("CandCam"); go.hideFlags = HideFlags.HideAndDontSave;
var cam = go.AddComponent<Camera>(); cam.CopyFrom(main); go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); cam.fieldOfView = 70f;
var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.aspect = 1280f / 720f;
for (int i = 0; i < top.Count; i++) { var c = top[i];
  cam.transform.position = c.g + Vector3.up * 3.5f; cam.transform.rotation = Quaternion.LookRotation(c.dir);
  cam.Render(); RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
  System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/hill/cand_" + (i + 1) + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
  sb.Append("cand_" + (i + 1) + " vis " + c.vis + "/" + targets.Count + " at (" + c.g.x.ToString("F1") + ", " + c.g.y.ToString("F2") + ", " + c.g.z.ToString("F1") + ") d" + c.d.ToString("F0") + " lookYaw " + (Mathf.Atan2(c.dir.x, c.dir.z) * Mathf.Rad2Deg).ToString("F0") + " yaws " + c.yaws + "\n"); }
cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
sb.Append("dirty=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty);
return sb.ToString();
