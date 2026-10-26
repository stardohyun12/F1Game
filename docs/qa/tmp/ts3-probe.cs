// 2203: 톤 구간(1040~1130) 주변 큰 렌더러의 재질·셰이더 목록 (밭·비탈 풀·보도 정체 찾기)
var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (s.name != "Map_BayLoop") UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Map_BayLoop.unity");
var rs = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var c = rs.centerline;
var sb = new System.Text.StringBuilder();
var seen = new System.Collections.Generic.Dictionary<string, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    var b = r.bounds; bool near = false;
    for (int i = 1040; i <= 1130; i += 10) if (b.SqrDistance(c[i]) < 45f * 45f) { near = true; break; }
    if (!near || b.size.magnitude < 6f) continue;
    if (r.transform.IsChildOf(GameObject.Find("BayLoop").transform.Find("ToneSlice_ReturnRoad"))) continue;
    var m = r.sharedMaterial; string key = (m ? m.name : "null") + " | " + (m ? m.shader.name : "") + " | " + r.GetType().Name;
    string nm = r.name; if (r.transform.parent) nm = r.transform.parent.name + "/" + nm;
    if (seen.ContainsKey(key)) { seen[key]++; continue; }
    seen[key] = 1; sb.AppendLine(key + " <- " + nm + " size=" + b.size.ToString("F0"));
}
foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)) sb.AppendLine("TERRAIN " + t.name + " mat=" + (t.materialTemplate ? t.materialTemplate.name + "/" + t.materialTemplate.shader.name : "null"));
return sb.ToString();
