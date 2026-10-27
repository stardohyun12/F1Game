// 과제 1815: 돌담 MeshCollider(얇은 0.42m 껍데기)를 차가 90° 에서 뚫고 지나감(봇2: 담 높이보다 낮은 채로 z -1.7 까지) → 구간마다 BoxCollider(속이 찬 상자)로.
//  상자 = 보이는 담 앞면(zF) 그대로, 높이 = 그 구간 담 높이(top), 뒤쪽 두께만 담 뒤 흙 속으로 TH 만큼. 위로 덧댄 투명 높이 없음.
const float TH = 1.0f, zF = 0.15f;
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); var log = new System.Text.StringBuilder();
foreach (Transform w in GameObject.Find("SceneA4").transform.Find("A6d_StoneWalls")) {
  var mc = w.GetComponent<MeshCollider>(); if (mc) UnityEngine.Object.DestroyImmediate(mc);
  var old = new System.Collections.Generic.List<GameObject>(); foreach (Transform c in w) old.Add(c.gameObject); foreach (var o in old) UnityEngine.Object.DestroyImmediate(o);
  var V = w.GetComponent<MeshFilter>().sharedMesh.vertices; int N = (V.Length - 8) / 12; float hmin = 9, hmax = 0;
  for (int i = 0; i < N; i++) {
    Vector3 b1 = V[12 * i], t1 = V[12 * i + 1], t0 = V[12 * i + 2], b0 = V[12 * i + 3];
    float dx = t1.x - t0.x, ang = Mathf.Atan2(t1.y - t0.y, dx); float len = Mathf.Sqrt(dx * dx + (t1.y - t0.y) * (t1.y - t0.y));
    float h = Mathf.Min(t0.y - b0.y, t1.y - b1.y); var mid = (t0 + t1) * 0.5f;
    var go = new GameObject("Col_" + i); go.layer = w.gameObject.layer; go.transform.SetParent(w, false);
    go.transform.localRotation = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg); go.transform.localPosition = mid;
    var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, -h * 0.5f, zF - TH * 0.5f - mid.z); bc.size = new Vector3(len + 0.04f, h, TH);
    hmin = Mathf.Min(hmin, h); hmax = Mathf.Max(hmax, h); }
  log.Append(w.name + " boxes " + N + " h " + hmin.ToString("F2") + "~" + hmax.ToString("F2") + "; "); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return log.ToString();
