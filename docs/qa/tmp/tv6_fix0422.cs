// 과제 0422 (에디트 모드, Map_TownV6 열린 상태): ① 찻집 건물(TUT_TeaHouse) 콜라이더를 고리 길 밖으로 ② 찻집 정류장 주황 상자 끄고 비컨 가늘게
// ③ 플레이어 외형 F1 → 카트 v6e (Scene_A6d_Town 의 Race/A6_KartVisual 복사, 콜라이더·스키드 자리도 A6d 값) ④ 튜토리얼 말풍선 이름 = 택배 보관소 직원. 저장.
var sb = new System.Text.StringBuilder();
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/"));
var L = System.IO.File.ReadAllLines(dir + "tv6_loop_pts.txt").Where(l => l.Trim() != "").Select(l => { var a = l.Split(' '); return new Vector2(float.Parse(a[0]), float.Parse(a[1])); }).ToList();
// ① 찻집: 콜라이더 xz 사각형(+2.5m 여유) 안에 고리 점이 없을 때까지 가장 짧게 민다
var th = GameObject.Find("TUT_TeaHouse"); var bc = th.GetComponent<BoxCollider>();
System.Func<Vector3, int> Hits = off => { var bb = bc.bounds; bb.center += off; bb.Expand(new Vector3(5f, 0, 5f)); int n = 0; foreach (var q in L) if (q.x > bb.min.x && q.x < bb.max.x && q.y > bb.min.z && q.y < bb.max.z) n++; return n; };
sb.Append("tea hits before " + Hits(Vector3.zero) + "; ");
Vector3 best = Vector3.zero; float bestD = 1e9f;
if (Hits(Vector3.zero) > 0)
    for (int k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward; for (float s = 0.5f; s <= 15f; s += 0.5f) if (Hits(d * s) == 0) { if (s < bestD) { bestD = s; best = d * s; } break; } }
th.transform.position += best; Physics.SyncTransforms();
sb.Append("tea moved " + best.ToString("F1") + " col " + bc.bounds.center.ToString("F1") + " hits " + Hits(Vector3.zero) + "; ");
// ② 찻집 정류장 표시
var stop = GameObject.Find("STOP_hill_field");
if (stop) foreach (Transform t in stop.transform)
{
    if (t.name == "House") { var r = t.GetComponent<Renderer>(); if (r) r.enabled = false; sb.Append("House off; "); }
    if (t.name == "Beacon") { var s = t.localScale; t.localScale = new Vector3(s.x * 0.25f, s.y, s.z * 0.25f); sb.Append("Beacon " + t.GetComponent<Renderer>().bounds.size.ToString("F2") + "; "); }
}
// ③ 카트 v6e
var race = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>().transform;
var old = race.Find("A6_KartVisual"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var b = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A6d_Town.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
Transform braces = null; foreach (var g in b.GetRootGameObjects()) { var c = g.GetComponentInChildren<ArcadeCarController>(true); if (c) { braces = c.transform; break; } }
var src = braces.Find("A6_KartVisual");
var kv = UnityEngine.Object.Instantiate(src.gameObject).transform; kv.name = "A6_KartVisual";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(kv.gameObject, sc);
kv.SetParent(race, false); kv.localPosition = src.localPosition; kv.localRotation = src.localRotation; kv.localScale = src.localScale; kv.gameObject.SetActive(true);
// 밖(A6d 씬)을 가리키는 참조 → 같은 이름의 TV6 Race 자식으로
int fixedRefs = 0, lost = 0;
foreach (var comp in kv.GetComponentsInChildren<Component>(true))
{
    if (comp is Transform) continue;
    var so = new UnityEditor.SerializedObject(comp); var it = so.GetIterator(); bool ch = false;
    while (it.Next(true))
    {
        if (it.propertyType != UnityEditor.SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
        GameObject rg = it.objectReferenceValue is Component rc0 ? rc0.gameObject : it.objectReferenceValue as GameObject;
        if (rg == null || rg.scene != b) continue;
        Transform tgt = rg.transform == braces ? race : race.Find(rg.name);
        UnityEngine.Object nv = null; if (tgt) nv = it.objectReferenceValue is Component ? (UnityEngine.Object)tgt.GetComponent(it.objectReferenceValue.GetType()) : tgt.gameObject;
        it.objectReferenceValue = nv; ch = true; if (nv) fixedRefs++; else lost++;
    }
    if (ch) so.ApplyModifiedPropertiesWithoutUndo();
}
// A6d 와 같은 상태: F1 외형 끄고, body·wheel 렌더러 끔, 콜라이더·스키드·연기 자리
foreach (Transform t in race)
{
    var bt = braces.Find(t.name);
    if (t.name == "FormulaGameplayVisual" || t.name == "CarVisual_A" || t.name == "A4_KartVisual") t.gameObject.SetActive(false);
    if ((t.name == "body" || t.name.StartsWith("wheel-")) && t.GetComponent<Renderer>()) t.GetComponent<Renderer>().enabled = false;
    if (bt && (t.name.StartsWith("SkidMark") || t.name.StartsWith("Smoke"))) t.localPosition = bt.localPosition;
}
var myBox = race.GetComponent<BoxCollider>(); var srcBox = braces.GetComponent<BoxCollider>();
sb.Append("col " + myBox.size.ToString("F2") + " → " + srcBox.size.ToString("F2") + "; ");
myBox.center = srcBox.center; myBox.size = srcBox.size;
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(b, true);
var vb = new Bounds(); bool f0 = false; foreach (var r in kv.GetComponentsInChildren<MeshRenderer>(false)) { if (!f0) { vb = r.bounds; f0 = true; } else vb.Encapsulate(r.bounds); }
sb.Append("kart refs fixed " + fixedRefs + " lost " + lost + " visualWorld " + vb.size.ToString("F2") + " colWorld " + myBox.bounds.size.ToString("F2") + "; ");
// ④ 말풍선 이름
var ot = UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>(UnityEngine.FindObjectsInactive.Include);
var oso = new UnityEditor.SerializedObject(ot); var pn = oso.FindProperty("packerName");
if (pn != null) { pn.stringValue = "택배 보관소 직원"; oso.ApplyModifiedPropertiesWithoutUndo(); sb.Append("packer ok; "); } else sb.Append("packerName 없음(컴파일 전?); ");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return sb.ToString();
