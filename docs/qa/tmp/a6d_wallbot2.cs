// 과제 1815 봇 2 (Play 중): a4e_wallbot 과 같은 12회(집마다 길 가운데→집 쪽 35°·90°, 40km/h 풀스로틀 3초)
//  판정을 담 기준으로: 차 중심을 연석/담 로컬로 옮겨, 담 뒷면(z<-0.27) 안쪽(|x|<L/2)에 들어가면 【넘음】, 담 끝 밖으로 돌아 z<-0.27 이면 【옆으로 돎】.
//  넘었으면 그 순간 차 높이(담 위 몇 m)·속도를 남김. 결과 SessionState "a4e.wall"
if (!Application.isPlaying) return "Play 중에만";
Application.runInBackground = true;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var mi = typeof(ArcadeCarController).GetField("moveInput", BF);
var root = GameObject.Find("SceneA4").transform; var g = root.Find("A4b_Houses"); var sb = new System.Text.StringBuilder();
sb.Append("kart cols: "); foreach (var c in car.GetComponentsInChildren<Collider>()) sb.Append(c.GetType().Name + " " + c.bounds.size.ToString("F2") + " "); sb.Append(" cd " + rb.collisionDetectionMode + "\n");
var runs = new System.Collections.Generic.List<(string n, Vector3 pos, Vector3 dir, Transform cb, float L, float ang)>();
foreach (Transform t in g) {
  var cb = root.Find("A4e_Curbs/Curb_" + t.name); float L = cb.GetComponent<BoxCollider>().size.x;
  var q = t.rotation; t.rotation = Quaternion.identity; var b = t.GetComponentInChildren<Renderer>().bounds; t.rotation = q; float fz = b.max.z - t.position.z;
  var o = t.forward; o.y = 0; o.Normalize(); var f0 = t.position + o * fz; float near = -1, far = -1;
  for (float d = 0; d < 25; d += 0.5f) { bool road = false; foreach (var hh in Physics.RaycastAll(f0 + o * d + Vector3.up * 20, Vector3.down, 60)) if (hh.collider.name == "Road") road = true; if (road && near < 0) near = d; if (road) far = d; else if (near >= 0) break; }
  if (near < 0) { sb.Append(t.name + ": 앞 25m 안에 Road 없음\n"); continue; }
  var mid = f0 + o * ((near + far) * 0.5f); float gy = mid.y; foreach (var hh in Physics.RaycastAll(mid + Vector3.up * 20, Vector3.down, 60)) if (hh.collider.name == "Road") gy = hh.point.y; mid.y = gy + 0.6f;
  var ml = cb.InverseTransformPoint(mid); sb.Append(t.name + " 출발 담로컬 x " + ml.x.ToString("F1") + " z " + ml.z.ToString("F1") + " L " + L.ToString("F1") + "; ");
  foreach (var a in new[] { 35f, 90f }) runs.Add((t.name, mid, Quaternion.AngleAxis(90f - a, Vector3.up) * -o, cb, L, a)); }
sb.Append("\n"); UnityEditor.SessionState.SetString("a4e.wall", "running");
int k = -1; float t0 = 0, zmin = 99, hOver = -9; string res = ""; Vector3 lpAt = Vector3.zero;
UnityEditor.EditorApplication.CallbackFunction cbk = null;
System.Action Next = () => { k++; if (k >= runs.Count) { UnityEditor.EditorApplication.update -= cbk; mi.SetValue(car, Vector2.zero); UnityEditor.SessionState.SetString("a4e.wall", sb.ToString()); return; }
  var r = runs[k]; var rot = Quaternion.LookRotation(r.dir, Vector3.up); rb.position = r.pos; rb.rotation = rot; car.transform.SetPositionAndRotation(r.pos, rot);
  rb.angularVelocity = Vector3.zero; rb.linearVelocity = r.dir * (40f / 3.6f); t0 = Time.time; zmin = 99; res = ""; hOver = -9; };
cbk = () => {
  if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cbk; return; }
  if (k < 0) { Next(); return; }
  var r = runs[k]; mi.SetValue(car, new Vector2(0f, 1f));
  var lp = r.cb.InverseTransformPoint(rb.position); if (lp.z < zmin) { zmin = lp.z; lpAt = lp; }
  if (res == "" && lp.z < -0.27f) { res = Mathf.Abs(lp.x) < r.L * 0.5f ? "【넘음】" : "【옆으로 돎】";
    var wall = root.Find("A6d_StoneWalls/StoneWall_" + r.n); float wy = -99; foreach (var hh in Physics.RaycastAll(rb.position + Vector3.up * 5, Vector3.down, 10, ~0, QueryTriggerInteraction.Ignore)) { }
    hOver = lp.y; }
  if (Time.time - t0 > 3f) { sb.Append(r.n + " " + r.ang + "°: " + (res == "" ? "막힘" : res) + " 최소 담로컬 z " + zmin.ToString("F2") + " (x " + lpAt.x.ToString("F1") + " y " + lpAt.y.ToString("F2") + ") 끝 " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "km/h\n"); Next(); } };
UnityEditor.EditorApplication.update += cbk;
return "started " + runs.Count + " runs: " + sb.ToString();
