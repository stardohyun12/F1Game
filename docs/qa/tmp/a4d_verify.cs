// 과제 1401 확인만(저장 안 함): Scene_A4d_Town 차 치수·콜라이더·바퀴·속도값 + 직전 봇 결과(SessionState a4d.drive)
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A4d_Town.unity");
var car = GameObject.Find("Race").transform; float s = car.lossyScale.x; var sb = new System.Text.StringBuilder();
var kv = car.Find("A4_KartVisual"); bool f = false; var b = new Bounds();
foreach (var r in kv.GetComponentsInChildren<MeshRenderer>(true)) { if (!r.enabled || !r.gameObject.activeInHierarchy) continue; foreach (var p in new[]{r.bounds.min, r.bounds.max}) { var lp = car.InverseTransformPoint(p) * s; if (!f) { b = new Bounds(lp, Vector3.zero); f = true; } else b.Encapsulate(lp); } }
sb.Append("raceScale " + s.ToString("F3") + " vis LxWxH " + b.size.z.ToString("F2") + "x" + b.size.x.ToString("F2") + "x" + b.size.y.ToString("F2") + " minY " + b.min.y.ToString("F2") + "\n");
var box = car.GetComponent<BoxCollider>(); sb.Append("box size(m) " + (box.size * s).ToString("F2") + " center(m) " + (box.center * s).ToString("F2") + "\n");
var so = new SerializedObject(car.GetComponent<ArcadeCarController>());
foreach (var n in new[]{"driveMode","wheelRadius","bodyLift","wheelInset","centerOfMassOffset","accelTime","maxSpeedKmh"}) { var p = so.FindProperty(n); if (p == null) { sb.Append(n + "=?; "); continue; }
  string v = p.propertyType == SerializedPropertyType.Float ? p.floatValue.ToString("F3") : p.propertyType == SerializedPropertyType.Vector2 ? p.vector2Value.ToString("F3") : p.propertyType == SerializedPropertyType.Vector3 ? p.vector3Value.ToString("F3") : p.propertyType == SerializedPropertyType.Enum ? p.enumNames[p.enumValueIndex] : p.intValue.ToString(); sb.Append(n + "=" + v + "; "); }
var rb = car.GetComponent<Rigidbody>(); sb.Append("mass " + rb.mass + "\n");
var kw = kv.GetComponent<KartWheelVisual>(); sb.Append("KartWheelVisual on " + (kw ? kw.gameObject.name + " enabled " + kw.enabled : "NONE") + "\n");
if (kw) { var kso = new SerializedObject(kw); sb.Append(" body " + kso.FindProperty("body").objectReferenceValue?.name + " maxSteer " + kso.FindProperty("maxSteerDeg").floatValue + " track " + kso.FindProperty("trackWidth").floatValue.ToString("F2") + " base " + kso.FindProperty("wheelBase").floatValue.ToString("F2") + "\n");
  var w = kso.FindProperty("wheels"); for (int i = 0; i < w.arraySize; i++) { var t = w.GetArrayElementAtIndex(i).objectReferenceValue as Transform; if (!t) { sb.Append(" wheel" + i + " NULL\n"); continue; }
    var lp = car.InverseTransformPoint(t.position) * s; var rs = t.GetComponentsInChildren<Renderer>(); var wb = rs.Length > 0 ? rs[0].bounds : new Bounds(); foreach (var r in rs) wb.Encapsulate(r.bounds);
    sb.Append(" " + t.name + " pos(m) " + lp.ToString("F2") + " r " + wb.extents.y.ToString("F3") + " bottomY " + (car.InverseTransformPoint(wb.min)*s).y.ToString("F2") + " parts " + rs.Length + "\n"); } }
var m = GameObject.Find("a3_M1_mound"); if (m) { var mm = m.GetComponent<MeshFilter>().sharedMesh; sb.Append("mound mesh " + mm.name + " asset [" + AssetDatabase.GetAssetPath(mm) + "]\n"); }
sb.Append("scene " + sc.path + " dirty " + sc.isDirty + "\nDRIVE " + SessionState.GetString("a4d.drive", "(none)"));
return sb.ToString();
