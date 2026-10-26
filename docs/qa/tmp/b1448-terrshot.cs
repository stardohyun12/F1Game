// 과제 1448: 지형 톱니 전후 캡처(에디트 모드). 자리는 처음 한 번 고르고 SessionState 에 고정 → after 에서 같은 자리.
// A = 해안 비탈(바다 근처 가파른 땅 + 위에 본선), B = 언덕 비탈(높은 곳 가파른 땅 + 옆에 본선). 각 자리: 위에서(도현 스샷 꼴) + 차 시점.
string tag = UnityEditor.SessionState.GetString("b1448.tag", "before");
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/bug-1447/" + tag));
System.IO.Directory.CreateDirectory(outDir);
var terr = GameObject.Find("BayTerrain"); var mesh = terr.GetComponent<MeshFilter>().sharedMesh; var V = mesh.vertices; var N = mesh.normals;
var main = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
var P = main.centerline;
string saved = UnityEditor.SessionState.GetString("b1448.spots", "");
var log = new System.Text.StringBuilder(tag + " ");
if (saved == "")
{
    // 중심선 점마다 옆 4~22m 의 지형 정점 중 경사>45° 개수, 그 점 땅 높이
    int bestA = -1, bestB = -1; float sA = 0, sB = 0;
    var steep = new List<Vector3>(); for (int k = 0; k < V.Length; k++) if (N[k].y < 0.7f) steep.Add(V[k]);
    for (int pass = 0; pass < 2; pass++)
    for (int i = 0; i < P.Length; i += 4)
    {
        int cnt = 0; float lowY = 999;
        foreach (var s in steep) { float d = Vector2.Distance(new Vector2(s.x, s.z), new Vector2(P[i].x, P[i].z)); if (d > 4 && d < 22 && s.y < P[i].y - 1) { cnt++; lowY = Mathf.Min(lowY, s.y); } }
        if (pass == 0 && lowY < 2.5f && cnt > sA) { sA = cnt; bestA = i; }
        if (pass == 1 && P[i].y > 18 && cnt > sB && (bestA < 0 || Vector3.Distance(P[i], P[bestA]) > 150)) { sB = cnt; bestB = i; }
    }
    saved = bestA + "," + bestB; UnityEditor.SessionState.SetString("b1448.spots", saved);
    log.Append("picked A#" + bestA + "(" + sA + ") B#" + bestB + "(" + sB + ") ");
}
var ids = saved.Split(',').Select(int.Parse).ToArray();
var go = new GameObject("b1448cam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.3f; cam.farClipPlane = 1500f;
var mc = Camera.main; if (mc != null) { cam.clearFlags = mc.clearFlags; cam.backgroundColor = mc.backgroundColor; }
var ad = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ad.renderShadows = true;
System.Action<string, Vector3, Vector3, float> Shot = (name, cp, look, fov) =>
{
    cam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(look - cp)); cam.fieldOfView = fov;
    var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); rt.Release();
    log.Append(name + " ");
};
System.Func<int, Vector3> Fw = i => { var f = P[Mathf.Min(i + 3, P.Length - 1)] - P[Mathf.Max(i - 3, 0)]; f.y = 0; return f.normalized; };
string[] nm = { "A_coast", "B_hill" };
for (int s = 0; s < 2; s++)
{
    int i = ids[s]; if (i < 0) continue; var p = P[i]; var f = Fw(i);
    // 비탈 쪽(낮은 쪽) 찾기
    var nL = new Vector3(-f.z, 0, f.x); float hL = 0, hR = 0;
    if (Physics.Raycast(p + nL * 12 + Vector3.up * 80, Vector3.down, out var a, 200f)) hL = a.point.y;
    if (Physics.Raycast(p - nL * 12 + Vector3.up * 80, Vector3.down, out var b, 200f)) hR = b.point.y;
    var down = hL < hR ? nL : -nL; var mid = p + down * 10; mid.y = p.y - 5;
    Shot(nm[s] + "_1_top", mid + Vector3.up * 40 - f * 6, mid, 50f);               // 위에서(도현 스샷 꼴)
    Shot(nm[s] + "_2_top_close", mid + Vector3.up * 16 - f * 3, mid, 50f);
    Shot(nm[s] + "_3_car", p - f * 7 + Vector3.up * 3.2f, p + f * 10 + down * 6 - Vector3.up * 1.5f, 60f);   // 차 시점(카메라 B 근사), 비탈 쪽으로
    log.Append(nm[s] + "@" + p.ToString("F0") + " ");
}
UnityEngine.Object.DestroyImmediate(go);
return log.ToString();
