// 과제 1331 — Scene_A4d_Town = Scene_A4c_Town 복사. 이 씬 Race 만 바꿈.
//  1 A4_KartVisual 을 실제 카트 길이 1.8m 로 축소(1232 의 ×1.47 되돌림), 바퀴 바닥 = 상자 바닥, 가운데 정렬
//  2 BoxCollider = 카트 치수(폭=외형 폭, 길이 1.8, 높이 0.45), 바퀴 레이 위치(wheelInset)=실제 타이어 중심, wheelRadius=타이어 반지름, 무게중심=상자 바닥 높이
//  3 타이어·캡·허브를 바퀴 피벗 4개(바퀴 중심, 축=차 축)로 묶고 나머지는 KartBody 로 → KartWheelVisual 부착
//  4 옛 자리표시 body/wheel-* 렌더러 끔, 스키드·연기 위치 = 뒷바퀴
const string S = "Assets/Scenes/Scene_A4d_Town.unity"; const float LEN = 1.8f, BOXH = 0.45f, LIFT = 0.10f;
AssetDatabase.DeleteAsset(S); AssetDatabase.CopyAsset("Assets/Scenes/Scene_A4c_Town.unity", S);
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(S);
var car = GameObject.Find("Race").transform; var kv = car.Find("A4_KartVisual"); var log = new System.Text.StringBuilder();
if (PrefabUtility.IsPartOfPrefabInstance(kv.gameObject)) { var pr = PrefabUtility.GetOutermostPrefabInstanceRoot(kv.gameObject); log.Append("unpack " + pr.name + "; "); PrefabUtility.UnpackPrefabInstance(pr, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); }
float s = car.lossyScale.x; var box = car.GetComponent<BoxCollider>();
System.Func<Transform, Bounds> LB = (root) => { bool f = false; var b = new Bounds(); foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) foreach (var p in new[] { r.bounds.min, r.bounds.max }) { var lp = car.InverseTransformPoint(p) * s; if (!f) { b = new Bounds(lp, Vector3.zero); f = true; } else b.Encapsulate(lp); } return b; };   // Race 로컬(월드 m 단위)
var b0 = LB(kv); log.Append("before " + b0.size.ToString("F2") + "; ");
float k = LEN / b0.size.z; kv.localScale *= k;
float bottomW = (box.center.y - box.size.y * 0.5f) * s;   // 상자 바닥(Race 로컬, m)
var b1 = LB(kv); kv.localPosition += new Vector3(-b1.center.x, bottomW - b1.min.y, -b1.center.z) / s;
var b2 = LB(kv); log.Append("after " + b2.size.ToString("F2") + " k " + k.ToString("F3") + "; ");
// ---- 바퀴 피벗
var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Transform>>();
foreach (var r in kv.GetComponentsInChildren<MeshRenderer>(true)) { string n = r.name, key = null;
  if (n.StartsWith("k_tire")) key = n.Substring(6); else if (n.StartsWith("k_hub")) key = n.Substring(5); if (key == null) continue;
  if (key.EndsWith("_cap")) key = key.Substring(0, key.Length - 4); if (!groups.ContainsKey(key)) groups[key] = new System.Collections.Generic.List<Transform>(); groups[key].Add(r.transform); }
var body = new GameObject("KartBody").transform; body.SetParent(kv, false); body.position = car.TransformPoint(new Vector3(0, bottomW / s, 0)); body.rotation = car.rotation;
var wl = new Transform[4]; float rad = 0, tx = 0, zf = 0, zr = 0; var keys = new System.Collections.Generic.List<string>(groups.Keys);
foreach (var key in keys) { var ts = groups[key]; Bounds wb = ts[0].GetComponent<Renderer>().bounds; foreach (var t in ts) wb.Encapsulate(t.GetComponent<Renderer>().bounds);
  var lc = car.InverseTransformPoint(wb.center); int idx = (lc.z > 0 ? 0 : 2) + (lc.x < 0 ? 0 : 1);
  var pv = new GameObject("Wheel_" + (idx < 2 ? "F" : "R") + (idx % 2 == 0 ? "L" : "R")).transform; pv.SetParent(kv, true); pv.position = wb.center; pv.rotation = car.rotation; pv.localScale = Vector3.one;
  foreach (var t in ts) t.SetParent(pv, true); wl[idx] = pv;
  rad += wb.size.y * 0.5f / keys.Count; tx += Mathf.Abs(lc.x) / keys.Count; if (idx < 2) zf += lc.z * 0.5f; else zr += lc.z * 0.5f;
  log.Append(pv.name + "<" + key + " n" + ts.Count + " c " + (lc * s).ToString("F2") + "; "); }
var others = new System.Collections.Generic.List<Transform>(); foreach (Transform c in kv) if (c != body && System.Array.IndexOf(wl, c) < 0) others.Add(c);   // 바퀴 피벗을 만든 뒤에 모아야 타이어가 KartBody 로 다시 끌려가지 않는다
foreach (var t in others) t.SetParent(body, true);
// ---- 콜라이더·바퀴 레이·무게중심 (로컬 = 월드/s)
float widthW = b2.size.x; var ctr = new Vector3(0, (bottomW + BOXH * 0.5f) / s, (zf + zr) * 0.5f);
box.center = ctr; box.size = new Vector3(widthW, BOXH, LEN) / s;
var cso = new SerializedObject(car.GetComponent<ArcadeCarController>());
cso.FindProperty("wheelRadius").floatValue = rad; cso.FindProperty("bodyLift").floatValue = LIFT;
cso.FindProperty("wheelInset").vector2Value = new Vector2(tx / (box.size.x * 0.5f), (zf - zr) * 0.5f / (box.size.z * 0.5f));
cso.FindProperty("centerOfMassOffset").vector3Value = new Vector3(0, bottomW / s, ctr.z);
cso.ApplyModifiedProperties();
log.Append("box " + (box.size * s).ToString("F2") + " ctrZ " + (ctr.z * s).ToString("F2") + " rad " + rad.ToString("F3") + " inset " + cso.FindProperty("wheelInset").vector2Value.ToString("F2") + " trackX " + (tx * s).ToString("F2") + " zf " + (zf * s).ToString("F2") + " zr " + (zr * s).ToString("F2") + "; ");
var kw = kv.GetComponent<KartWheelVisual>(); if (!kw) kw = kv.gameObject.AddComponent<KartWheelVisual>();
var kso = new SerializedObject(kw); kso.FindProperty("body").objectReferenceValue = body; for (int i = 0; i < 4; i++) kso.FindProperty("wheels").GetArrayElementAtIndex(i).objectReferenceValue = wl[i];
kso.FindProperty("wheelRadius").floatValue = rad; kso.FindProperty("trackWidth").floatValue = tx * s * 2f; kso.FindProperty("wheelBase").floatValue = (zf - zr) * s; kso.ApplyModifiedProperties();
// ---- 옛 자리표시·스키드·연기
foreach (var n in new[] { "body", "wheel-back-left", "wheel-back-right", "wheel-front-left", "wheel-front-right" }) { var t = car.Find(n); if (t) foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false; }
foreach (var n in new[] { "SkidMark_L", "SkidMark_R", "Smoke_L", "Smoke_R" }) { var t = car.Find(n); if (t) t.localPosition = new Vector3((n.EndsWith("L") ? -tx : tx), t.localPosition.y, zr); }
var b3 = LB(kv); log.Append("final vis " + b3.size.ToString("F2") + " minY " + b3.min.y.ToString("F2") + " boxBottom " + bottomW.ToString("F2") + "; missing " + System.Array.IndexOf(wl, null));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return log.ToString();
