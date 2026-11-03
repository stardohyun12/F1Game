// 과제 20261102-1543: 찻집 STOP_tea 북쪽 벽(길 쪽)에 문 앞 건네기 대역 설치. 에디트 모드 전용, 다시 돌려도 같은 결과.
if (Application.isPlaying) return "play 중 — 중단";
var sb = new System.Text.StringBuilder();
var st = GameObject.Find("STOP_tea");
var house = st != null ? st.transform.Find("House") : null;
if (house == null) return "STOP_tea/House 없음";
var old = st.transform.Find("Handover_tea");
if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
var b = house.GetComponent<Renderer>().bounds;
var root = new GameObject("Handover_tea");
root.transform.SetParent(st.transform, false);
root.transform.position = new Vector3(b.center.x, b.min.y, b.max.z);   // 문 아래 가운데, 앞(+Z)이 길
root.transform.rotation = Quaternion.identity;
var ho = root.AddComponent<DoorHandover>();

var hinge = new GameObject("Hinge").transform;
hinge.SetParent(root.transform, false);
hinge.localPosition = new Vector3(-0.5f, 0f, 0.05f);
var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
panel.name = "DoorPanel";
panel.transform.SetParent(hinge, false);
panel.transform.localPosition = new Vector3(0.5f, 1.0f, 0f);
panel.transform.localScale = new Vector3(1.0f, 2.0f, 0.08f);   // BoxCollider 유지 = [E] 시선 대상

var opening = GameObject.CreatePrimitive(PrimitiveType.Cube);
opening.name = "Opening";
UnityEngine.Object.DestroyImmediate(opening.GetComponent<Collider>());
opening.transform.SetParent(root.transform, false);
opening.transform.localPosition = new Vector3(0f, 1.0f, 0.012f);
opening.transform.localScale = new Vector3(1.0f, 2.0f, 0.03f);

var rec = new GameObject("Recipient_Julia").transform;          // 발 = 피벗
rec.SetParent(root.transform, false);
rec.localPosition = new Vector3(0f, 0f, -0.8f);
rec.localRotation = Quaternion.identity;
var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
body.name = "Body";
UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
body.transform.SetParent(rec, false);
body.transform.localPosition = new Vector3(0f, 0.825f, 0f);
body.transform.localScale = new Vector3(0.45f, 0.825f, 0.35f);   // 키 1.65 m

var stand = new GameObject("StandOut").transform;
stand.SetParent(root.transform, false);
stand.localPosition = new Vector3(0f, 0f, 0.55f);

ho.hinge = hinge; ho.recipient = rec; ho.standOut = stand; ho.opening = opening.GetComponent<Renderer>();
foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var so = new UnityEditor.SerializedObject(dir);
so.FindProperty("handover").objectReferenceValue = ho;
so.ApplyModifiedProperties();
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
sb.AppendLine("root=" + root.transform.position.ToString("F2") + " door=" + ho.DoorCenter.ToString("F2") + " stand=" + stand.position.ToString("F2") + " rec=" + rec.position.ToString("F2"));
sb.AppendLine("dir.handover=" + (new UnityEditor.SerializedObject(dir).FindProperty("handover").objectReferenceValue != null) + " saved=" + saved + " scene=" + scene.path);
return sb.ToString();
