// artlive 1525 단계 ①-2 형태 수정(회색 그대로). iter_build.cs 다음에 돌린다. 재실행 가능(S02_* 지우고 s01 자리에서 다시).
// 1 크기 그대로. 2 비탈 접지: 바닥 = 발밑 땅 최고점(높은 쪽 기단 0) + 낮은 쪽까지 채우는 기단 + 출입구 앞 계단, 앞 소품은 발밑 땅/계단 위로.
// 3 road 컷: 카메라는 고정. 정면 왼쪽 모서리 축 돌림(0~30°) 후보를 Cam_IT_road 에서 정면 점 55개 레이캐스트로 비교, 길·이웃 침범 없고 최고점 85% 이상 중 가장 작은 돌림 + 돌출 간판판.
var log = new System.Text.StringBuilder();
var root = GameObject.Find("ITER_Depot").transform; var dp = root.Find("Depot_s01_form");
for (int i = root.childCount - 1; i >= 0; i--) if (root.GetChild(i).name.StartsWith("S02_")) UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
foreach (Transform t in dp) t.localPosition = Vector3.zero;   // 앞 소품 내린 것 되돌림
var slot = GameObject.Find("TV6_Loop").transform.Find("AssetSlots/Q3_Depot");
var yaw0 = Quaternion.Euler(0, slot.eulerAngles.y - 90f, 0); var fw0 = yaw0 * Vector3.forward; var piv0 = slot.position + fw0 * 3.5f;
var cam = GameObject.Find("ITER_Cams").transform.Find("Cam_IT_road").position;
var gray = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Iter_Depot_Mats/IT_depot_gray.mat");
System.Func<Vector3, RaycastHit> GHit = p => { RaycastHit best = new RaycastHit(); float y = -999; foreach (var h in Physics.RaycastAll(new Vector3(p.x, 60f, p.z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore)) if (h.point.y > y) { y = h.point.y; best = h; } return best; };
// 부품별 root-로컬 경계(정점 기준)
System.Func<Transform, Bounds> LB = t => { var mf = t.GetComponent<MeshFilter>(); var b = new Bounds(); bool f = false; foreach (var v in mf.sharedMesh.vertices) { var p = root.InverseTransformPoint(t.TransformPoint(v)); if (!f) { b = new Bounds(p, Vector3.zero); f = true; } else b.Encapsulate(p); } return b; };
var bB = LB(dp.Find("Q3_building")); log.Append("building local " + bB.min.ToString("F2") + ".." + bB.max.ToString("F2") + "\n");
foreach (var n in new[] { "Q3_shutter", "Q3_person_door", "Q3_front_goods", "Q3_packing_station", "Q3_sign" }) { var b = LB(dp.Find(n)); log.Append(n + " " + b.min.ToString("F2") + ".." + b.max.ToString("F2") + "\n"); }
// ---- 3. 후보 비교
// s01 자리에서도 앞 1m 가 길(ROAD) → 앞당김 불가. 정면 왼쪽 모서리(로컬 x=min)를 축으로 돌려 오른쪽(카메라 쪽)이 뒤로 물러나게. 「살짝」= 30° 까지
var pL = piv0 + yaw0 * new Vector3(bB.min.x, 0, 0);
var base0 = new System.Collections.Generic.HashSet<string>(); foreach (var h in Physics.OverlapBox(piv0 + yaw0 * new Vector3(0, 2f, bB.min.z / 2f), new Vector3(bB.extents.x, 1.8f, -bB.min.z / 2f), yaw0, ~0, QueryTriggerInteraction.Ignore)) base0.Add(h.name);   // s01 자리에서 이미 겹친 것(Walls_0 등)은 기준선
float bestS = -1; var cand = new System.Collections.Generic.List<(float dy, float s, string bad)>();
foreach (var dy in new[] { 0f, 5f, 10f, 15f, 20f, 25f, 30f }) {
  var q = Quaternion.Euler(0, yaw0.eulerAngles.y + dy, 0); var p0 = pL - q * new Vector3(bB.min.x, 0, 0); var fwd = q * Vector3.forward; p0.y = GHit(p0).point.y;
  int vis = 0; for (float x = -5f; x <= 5.01f; x += 1f) for (float y = 0.6f; y <= 3.41f; y += 0.7f) { var wp = p0 + q * new Vector3(x, y, 0.05f); var d = cam - wp; if (Vector3.Dot(d, fwd) <= 0) continue; if (!Physics.Raycast(wp, d.normalized, d.magnitude - 0.2f, ~0, QueryTriggerInteraction.Ignore)) vis++; }
  var tc = cam - p0; tc.y = 0; float face = Mathf.Max(0f, Vector3.Dot(fwd, tc.normalized)); float sc0 = vis * (0.3f + face);
  string bad = ""; foreach (var lx in new[] { bB.min.x, 0f, bB.max.x }) { var c = GHit(p0 + q * new Vector3(lx, 0, 0.3f)).collider; if (c && c.name.StartsWith("ROAD")) bad += "road@" + lx + " "; }
  foreach (var h in Physics.OverlapBox(p0 + q * new Vector3(0, 2f, bB.min.z / 2f), new Vector3(bB.extents.x, 1.8f, -bB.min.z / 2f), q, ~0, QueryTriggerInteraction.Ignore)) if (!h.name.Contains("Terrain") && !h.name.StartsWith("ROAD") && !base0.Contains(h.name)) bad += "hit:" + h.name + " ";
  cand.Add((dy, sc0, bad)); if (bad == "" && sc0 > bestS) bestS = sc0; }
foreach (var c in cand) log.Append("cand yaw+" + c.dy + " score " + c.s.ToString("F1") + " " + c.bad + "\n");
if (bestS < 0) return "NO CANDIDATE\n" + log;
var pick = cand.Where(c => c.bad == "" && c.s >= 0.85f * bestS).OrderBy(c => c.dy).First();
log.Append("PICK yaw+" + pick.dy + " score " + pick.s.ToString("F1") + " (best " + bestS.ToString("F1") + ")\n");
var rq = Quaternion.Euler(0, yaw0.eulerAngles.y + pick.dy, 0); var rp = pL - rq * new Vector3(bB.min.x, 0, 0);
// ---- 2. 접지: 바닥 높이 = 몸체 발밑 땅 최고점
root.rotation = rq; root.position = new Vector3(rp.x, 0f, rp.z);
float gMax = -999, gMin = 999; for (float x = bB.min.x; x <= bB.max.x + 0.01f; x += (bB.max.x - bB.min.x) / 8f) for (float z = bB.min.z; z <= 0.01f; z += -bB.min.z / 8f) { var g = GHit(root.TransformPoint(new Vector3(x, 0, z))).point.y; gMax = Mathf.Max(gMax, g); gMin = Mathf.Min(gMin, g); }
root.position = new Vector3(rp.x, gMax, rp.z); log.Append("pivot " + root.position.ToString("F2") + " yaw " + rq.eulerAngles.y.ToString("F2") + " ground under body " + gMin.ToString("F2") + ".." + gMax.ToString("F2") + " (기단 높이 0.." + (gMax - gMin).ToString("F2") + "m)\n");
System.Func<string, Vector3, Vector3, GameObject> Box = (nm, c, s) => { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = nm; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); g.transform.SetParent(root, false); g.transform.localPosition = c; g.transform.localRotation = Quaternion.identity; g.transform.localScale = s; var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = gray; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; return g; };
float bot = gMin - gMax - 0.3f;   // 땅속 0.3m 까지
Box("S02_plinth", new Vector3(bB.center.x, bot / 2f, bB.min.z / 2f + 0.05f), new Vector3(bB.size.x + 0.3f, -bot, -bB.min.z + 0.25f));   // 앞면보다 0.17m 앞으로 나옴
// 앞 땅과의 높이 차 → 출입구(셔터·사람 문) 앞 계단 1~2단
var bS = LB(dp.Find("Q3_shutter")); var bD = LB(dp.Find("Q3_person_door")); float sx0 = Mathf.Min(bS.min.x, bD.min.x) - 0.3f, sx1 = Mathf.Max(bS.max.x, bD.max.x) + 0.3f;
float gF = 999; for (float x = sx0; x <= sx1; x += 0.5f) gF = Mathf.Min(gF, GHit(root.TransformPoint(new Vector3(x, 0, 0.8f))).point.y); float gap = gMax - gF;
int ns = gap > 0.32f ? 2 : (gap > 0.1f ? 1 : 0);
for (int i = 1; i <= ns; i++) { float top = -gap * i / (ns + 1); Box("S02_step" + i, new Vector3((sx0 + sx1) / 2f, (top + bot) / 2f, 0.17f + 0.3f * (i - 0.5f)), new Vector3(sx1 - sx0, top - bot, 0.3f)); }
log.Append("front gap " + gap.ToString("F2") + "m steps " + ns + " x " + sx0.ToString("F2") + ".." + sx1.ToString("F2") + "\n");
// 앞 소품: 기단(앞 0.25m) 밖이면 발밑 땅으로 내림
foreach (var n in new[] { "Q3_front_goods", "Q3_packing_station" }) { var t = dp.Find(n); var b = LB(t); if (b.center.z < 0.3f) continue; var g = GHit(root.TransformPoint(new Vector3(b.center.x, 0, b.center.z))).point.y - gMax; t.localPosition = root.InverseTransformVector(Vector3.up) * 0f + Vector3.zero; t.position += Vector3.up * Mathf.Min(0f, g); log.Append(n + " lowered " + g.ToString("F2") + "\n"); }
// ---- 3b. 돌출 간판판: road 카메라 쪽 끝(+x), 벽에서 길 쪽으로 1.2m, 높이 2.3~3.2m + 받침대
float ex = bB.max.x - 0.4f;
Box("S02_bladeSign", new Vector3(ex, 2.75f, 0.85f), new Vector3(0.1f, 0.9f, 1.1f));
Box("S02_bladeArm", new Vector3(ex, 3.27f, 0.75f), new Vector3(0.06f, 0.06f, 1.5f));
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
log.Append("saved dirty " + sc.isDirty);
return log.ToString();
