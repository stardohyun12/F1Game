// 과제 0616 조사(읽기만): 보관소·우체국 위치, 근처 길 중심선, 앞 모서리 주변 지형 정점
var sb = new System.Text.StringBuilder();
var terrGo = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var mc = terrGo.GetComponent<MeshCollider>(); var tr = terrGo.transform;
var v = terrGo.GetComponent<MeshFilter>().sharedMesh.vertices;
var root = GameObject.Find("TownV6_Gray/D198_Buildings").transform;
foreach (var nm in new[] { "D198_depot", "D198_postoffice", "D198_garage" }) { var t = root.Find(nm); sb.Append(nm + " pos " + t.position.ToString("F2") + " rotY " + t.eulerAngles.y.ToString("F1") + "\n"); }
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { var c = r.centerline; if (c == null) continue; for (int i = 1; i < c.Length; i++) if (c[i].x > 240 && c[i].x < 320 && c[i].z > -70 && c[i].z < 10) sb.Append("seg " + r.name + " " + c[i - 1].ToString("F2") + " -> " + c[i].ToString("F2") + " hw " + (r.width * .5f).ToString("F2") + "\n"); }
float sx = 1e9f; Vector3 prev = Vector3.zero; int cnt = 0;
foreach (var lv in v) { var w = tr.TransformPoint(lv); if (w.x > 262f && w.x < 272f && w.z > -38f && w.z < -30f) { if (cnt++ < 60) sb.Append("v " + w.ToString("F2") + "\n"); } }
return sb.ToString();
