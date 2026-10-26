// 1514: BayTerrain 메시 정점 높이 vs BuildBayLoop.H (격자점) — 메시를 만든 뒤 terr 가 바뀌었나
var t = GameObject.Find("BayTerrain"); var V = t.GetComponent<MeshFilter>().sharedMesh.vertices;
int bad = 0; float worst = 0; Vector3 wp = Vector3.zero; int near = 0, nearBad = 0;
foreach (var v0 in V)
{
    var v = t.transform.TransformPoint(v0); float d = BuildBayLoop.H(v.x, v.z) - v.y;
    bool inBox = v.x > -130 && v.x < -60 && v.z > -150 && v.z < -90; if (inBox) near++;
    if (Mathf.Abs(d) > 0.05f) { bad++; if (inBox) nearBad++; if (Mathf.Abs(d) > Mathf.Abs(worst)) { worst = d; wp = v; } }
}
// 지형 메시 위로 레이: 석축 칸 자리에서 맨 위가 무엇인가(렌더러는 콜라이더 없음 → 메시 Raycast 대신 높이 비교)
return "verts " + V.Length + " |H-mesh|>5cm " + bad + " worst " + worst.ToString("F2") + " at " + wp.ToString("F1") + " | box near " + near + " bad " + nearBad;
