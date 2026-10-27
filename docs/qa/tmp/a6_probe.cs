// 과제 1617 탐색(저장 안 함): A4e Race/A4_KartVisual 구조·재질·참조, KartWheelVisual 값
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4e_Town.unity");
var car = GameObject.Find("Race").transform; var kv = car.Find("A4_KartVisual"); var sb = new System.Text.StringBuilder();
sb.Append("race scale " + car.lossyScale.ToString("F3") + " kv lp " + kv.localPosition.ToString("F3") + " ls " + kv.localScale.ToString("F3") + " lr " + kv.localEulerAngles.ToString("F1") + "\n");
foreach (Transform c in kv) sb.Append(" child " + c.name + " n" + c.childCount + " lp " + c.localPosition.ToString("F3") + "\n");
var mset = new System.Collections.Generic.HashSet<Material>(); foreach (var r in kv.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) if (m) mset.Add(m);
foreach (var m in mset) sb.Append(" mat " + m.name + " [" + AssetDatabase.GetAssetPath(m) + "] sh " + m.shader.name + " tex " + (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") ? AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")) : "-") + "\n");
var kw = kv.GetComponent<KartWheelVisual>(); var so = new SerializedObject(kw); var it = so.GetIterator(); it.NextVisible(true);
while (it.NextVisible(false)) sb.Append(" kw." + it.name + "=" + (it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString("F3") : it.propertyType == SerializedPropertyType.ObjectReference ? (it.objectReferenceValue ? it.objectReferenceValue.name : "null") : it.propertyType.ToString()) + ";");
sb.Append("\n");
// 다른 컴포넌트가 kv 나 그 자식을 참조하는지
foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { if (!mb || mb == kw) continue; var s2 = new SerializedObject(mb); var p = s2.GetIterator();
  while (p.Next(true)) if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue) { var o = p.objectReferenceValue; Transform t = o is Component cc ? cc.transform : o is GameObject go ? go.transform : null; if (t && t.IsChildOf(kv)) sb.Append(" ref " + mb.GetType().Name + "@" + mb.name + "." + p.propertyPath + " -> " + t.name + "\n"); } }
// 바퀴 피벗 월드→Race 로컬 m
foreach (var n in new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" }) { var w = kv.Find(n); if (w) sb.Append(" " + n + " raceLocal_m " + (car.InverseTransformPoint(w.position) * car.lossyScale.x).ToString("F3") + "\n"); }
bool f = false; var b = new Bounds(); foreach (var r in kv.GetComponentsInChildren<MeshRenderer>(true)) { if (!r.enabled) continue; foreach (var q in new[] { r.bounds.min, r.bounds.max }) { var lp = car.InverseTransformPoint(q) * car.lossyScale.x; if (!f) { b = new Bounds(lp, Vector3.zero); f = true; } else b.Encapsulate(lp); } }
sb.Append(" vis bounds raceLocal_m min " + b.min.ToString("F3") + " max " + b.max.ToString("F3") + "\n");
var box = car.GetComponent<BoxCollider>(); sb.Append(" box c " + (box.center * car.lossyScale.x).ToString("F3") + " s " + (box.size * car.lossyScale.x).ToString("F3"));
var cso = new SerializedObject(car.GetComponent<ArcadeCarController>()); sb.Append(" ctrl wheelRadius " + cso.FindProperty("wheelRadius").floatValue.ToString("F3") + " inset " + cso.FindProperty("wheelInset").vector2Value.ToString("F3") + " groundLayer " + cso.FindProperty("groundLayer").intValue);
return sb.ToString();
