// 과제 1331 고침 2: 아직 길을 덮는 mound 삼각형(길 격자 0.5m 에서 길보다 0.05m 위로 맞은 것)의 꼭짓점 3개를 그 길 면 −0.3m 로. 3회 반복
var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
var m = GameObject.Find("a3_M1_mound"); var mc = m.GetComponent<MeshCollider>(); var road = GameObject.Find("Road").GetComponent<Collider>(); var mesh = m.GetComponent<MeshFilter>().sharedMesh;
var log = ""; var rbb = road.bounds;
for (int it = 0; it < 3; it++) { var v = mesh.vertices; var tri = mesh.triangles; int moved = 0, cov = 0;
  for (float x = rbb.min.x; x <= rbb.max.x; x += 0.5f) for (float z = rbb.min.z; z <= rbb.max.z; z += 0.5f) {
    var ray = new Ray(new Vector3(x, 80, z), Vector3.down); if (!road.Raycast(ray, out var rh, 200)) continue;
    if (!mc.Raycast(ray, out var mh, 200) || mh.point.y <= rh.point.y + 0.05f) continue; cov++;
    for (int j = 0; j < 3; j++) { int vi = tri[mh.triangleIndex * 3 + j]; var wp = m.transform.TransformPoint(v[vi]); if (wp.y > rh.point.y - 0.3f) { wp.y = rh.point.y - 0.3f; v[vi] = m.transform.InverseTransformPoint(wp); moved++; } } }
  mesh.vertices = v; mesh.RecalculateBounds(); mesh.RecalculateNormals(); mc.sharedMesh = null; mc.sharedMesh = mesh; log += "it" + it + " covered " + cov + " moved " + moved + "; "; }
EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssets(); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return log;
