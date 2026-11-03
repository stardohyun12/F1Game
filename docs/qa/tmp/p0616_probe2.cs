// 과제 0616 조사(읽기만): ROAD_ridge_up_c 끝(291,-37) 주변 지형 정점·길 메시 정점
var sb = new System.Text.StringBuilder();
var terrGo = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var tr = terrGo.transform; var mc = terrGo.GetComponent<MeshCollider>();
foreach (var lv in terrGo.GetComponent<MeshFilter>().sharedMesh.vertices) { var w = tr.TransformPoint(lv); if (w.x > 287 && w.x < 295 && w.z > -41 && w.z < -31) sb.Append("tv " + w.ToString("F2") + "\n"); }
var road = GameObject.Find("ROAD_ridge_up_c"); var mf = road.GetComponent<MeshFilter>();
foreach (var lv in mf.sharedMesh.vertices) { var w = mf.transform.TransformPoint(lv); if (w.x > 289) { RaycastHit h; float g = mc.Raycast(new Ray(new Vector3(w.x, 200, w.z), Vector3.down), out h, 400) ? h.point.y : float.NaN; sb.Append("rv " + w.ToString("F2") + " g " + g.ToString("F2") + "\n"); } }
return sb.ToString();
