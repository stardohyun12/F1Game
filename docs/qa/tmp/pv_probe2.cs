// 과제 0535: 방 창·문 구멍 찾기 + 시험 집 정면 방향 (임시 인스턴스, 저장 안 함)
var sb = new System.Text.StringBuilder();
var room = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PreviewCand/room/room.fbx")); room.transform.position = new Vector3(0, 500, 0);
foreach (var mf in room.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
  var b = mf.GetComponent<Renderer>().bounds; sb.Append(mf.name + " c " + (b.center - room.transform.position).ToString("F2") + " s " + b.size.ToString("F2") + " v " + mf.sharedMesh.vertexCount + "\n"); }
Physics.SyncTransforms();
var o = room.transform.position;
// 벽 4면: 안에서 밖으로 쏴서 안 맞는 칸 = 구멍. 높이 0.1~2.3, 폭 방향 0.1 간격
foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) {
  var side = Vector3.Cross(Vector3.up, d); float minS = 9, maxS = -9, minY = 9, maxY = -9; int holes = 0;
  for (float y = 0.1f; y < 2.35f; y += 0.1f) for (float s = -1.7f; s <= 1.7f; s += 0.1f) {
    var p = o + new Vector3(0, y, 0.08f) + side * s; var shell = room.transform.Find("ROOM_shell").GetComponent<Collider>();
    if (!shell.Raycast(new Ray(p, d), out var h, 3f)) { var p2 = p + d * 3f; if (shell.Raycast(new Ray(p2, -d), out var h2, 3f)) continue; /* 겉면도 없음 = 열린 벽 */ }
    else continue;
    holes++; minS = Mathf.Min(minS, s); maxS = Mathf.Max(maxS, s); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
  // 벽 위치
  float wall = -1; var sh2 = room.transform.Find("ROOM_shell").GetComponent<Collider>(); if (sh2.Raycast(new Ray(o + new Vector3(0, 0.3f, 0.08f), d), out var hw, 3f)) wall = hw.distance;
  sb.Append("dir " + d + " wall@" + wall.ToString("F2") + " holes " + holes + (holes > 0 ? " s " + minS.ToString("F1") + ".." + maxS.ToString("F1") + " y " + minY.ToString("F1") + ".." + maxY.ToString("F1") : "") + "\n"); }
UnityEngine.Object.DestroyImmediate(room);
var kit = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PreviewCand/kit/proof.fbx"));
foreach (Transform t in kit.transform) if (t.name.StartsWith("door_0") || t.name.StartsWith("door_handle_0") || t.name.Contains("sign") || t.name.StartsWith("front_eaves_0")) { var r = t.GetComponent<Renderer>(); if (r) sb.Append("kit " + t.name + " c " + r.bounds.center.ToString("F2") + " s " + r.bounds.size.ToString("F2") + "\n"); }
sb.Append("kit names: " + string.Join(",", kit.GetComponentsInChildren<Transform>().Select(t => t.name.Split('_')[0]).Distinct()) + "\n");
UnityEngine.Object.DestroyImmediate(kit);
var dep = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PreviewCand/depot/q3_depot.fbx"));
foreach (Transform t in dep.transform) { var r = t.GetComponent<Renderer>(); if (r) sb.Append("dep " + t.name + " c " + r.bounds.center.ToString("F2") + " s " + r.bounds.size.ToString("F2") + " mats " + string.Join(",", r.sharedMaterials.Select(m => m.name)) + "\n"); }
UnityEngine.Object.DestroyImmediate(dep);
return sb.ToString();
