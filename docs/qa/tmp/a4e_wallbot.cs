// 과제 1429 봇 (Play 중): 길 벽 낮춘 6곳(집 앞)에서 차가 길 밖(집 쪽)으로 나가는가.
//  집마다 집 정면(부모 로컬 +Z) 밖으로 0.5m 씩 내려 쏴 Road 구간(가까운 끝·먼 끝)을 찾고, 길 가운데에서 집 쪽으로
//  ① 35° 비스듬히 ② 90° 정면 — 40km/h 로 출발, 풀스로틀 조향 0, 3초. 나감 = 차 중심이 길 가까운 끝보다 집 쪽으로 0.3m 넘게 들어감.
//  결과 SessionState "a4e.wall"
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var mi = typeof(ArcadeCarController).GetField("moveInput", BF);
var g = GameObject.Find("SceneA4").transform.Find("A4b_Houses"); var sb = new System.Text.StringBuilder();
var runs = new System.Collections.Generic.List<(string n, Vector3 pos, Vector3 dir, Vector3 outD, Vector3 nearP, float ang)>();
foreach (Transform t in g) {
  var q = t.rotation; t.rotation = Quaternion.identity; var b = t.GetComponentInChildren<Renderer>().bounds; t.rotation = q; float fz = b.max.z - t.position.z;
  var o = t.forward; o.y = 0; o.Normalize(); var f0 = t.position + o * fz; float near = -1, far = -1;
  for (float d = 0; d < 25; d += 0.5f) { bool road = false; foreach (var hh in Physics.RaycastAll(f0 + o * d + Vector3.up * 20, Vector3.down, 60)) if (hh.collider.name == "Road") road = true; if (road && near < 0) near = d; if (road) far = d; else if (near >= 0) break; }
  if (near < 0) { sb.Append(t.name + ": 앞 25m 안에 Road 없음\n"); continue; }
  var nearP = f0 + o * near; var mid = f0 + o * ((near + far) * 0.5f); float gy = mid.y; foreach (var hh in Physics.RaycastAll(mid + Vector3.up * 20, Vector3.down, 60)) if (hh.collider.name == "Road") gy = hh.point.y; mid.y = gy + 0.6f;
  sb.Append(t.name + " 길 " + near.ToString("F1") + "~" + far.ToString("F1") + "m; ");
  foreach (var a in new[] { 35f, 90f }) runs.Add((t.name, mid, Quaternion.AngleAxis(90f - a, Vector3.up) * -o, o, nearP, a)); }
sb.Append("\n"); UnityEditor.SessionState.SetString("a4e.wall", "running");
int k = -1; float t0 = 0, worst = -99; string worstHit = "";
UnityEditor.EditorApplication.CallbackFunction cb = null;
System.Action Next = () => { k++; if (k >= runs.Count) { UnityEditor.EditorApplication.update -= cb; mi.SetValue(car, Vector2.zero); UnityEditor.SessionState.SetString("a4e.wall", sb.ToString()); return; }
  var r = runs[k]; var rot = Quaternion.LookRotation(r.dir, Vector3.up); rb.position = r.pos; rb.rotation = rot; car.transform.SetPositionAndRotation(r.pos, rot);
  rb.angularVelocity = Vector3.zero; rb.linearVelocity = r.dir * (40f / 3.6f); t0 = Time.time; worst = -99; worstHit = ""; };
cb = () => {
  if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
  if (k < 0) { Next(); return; }
  var r = runs[k]; mi.SetValue(car, new Vector2(0f, 1f));
  float into = Vector3.Dot(r.nearP - rb.position, r.outD);   // + = 길 가까운 끝보다 집 쪽
  if (into > worst) { worst = into; worstHit = ""; foreach (var c in Physics.OverlapSphere(rb.position, 1.2f, ~0, QueryTriggerInteraction.Ignore)) if (c.attachedRigidbody != rb && c.name != "Road" && worstHit.Length < 60) worstHit += c.name + ","; }
  if (Time.time - t0 > 3f) { sb.Append(r.n + " " + r.ang + "°: 최대 " + worst.ToString("F2") + "m 집 쪽 " + (worst > 0.3f ? "【나감】" : "막힘") + " 닿음 " + worstHit + " 끝 " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "km/h\n"); Next(); } };
UnityEditor.EditorApplication.update += cb;
return "started " + runs.Count + " runs: " + sb.ToString();
