// Play 중: 건물별 창 발광 삼각형 수(경계 안 정점 수/3) — 이름 필터
var glow = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "WindowGlow").GetComponent<MeshFilter>().sharedMesh.vertices;
var sb = new System.Text.StringBuilder();
foreach (var n in new[] { "BLD_P203_terrace_house", "BLD_P209_terrace_house" })
{
    var go = GameObject.Find(n); if (go == null) { sb.Append(n + " 없음\n"); continue; }
    var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    int c = glow.Count(v => b.Contains(v));
    var mf = go.GetComponentInChildren<MeshFilter>(); var m = mf.sharedMesh; var uv = m.uv;
    int inRect = 0; var t = m.GetTriangles(0);
    for (int i = 0; i < t.Length; i += 3) { var cc = (uv[t[i]] + uv[t[i + 1]] + uv[t[i + 2]]) / 3f; cc = new Vector2(Mathf.Repeat(cc.x, 1), Mathf.Repeat(cc.y, 1)); if (Rect.MinMaxRect(.25f, .5f, .5f, .75f).Contains(cc)) inRect++; }
    // 타일별 삼각형 분포
    var hist = new int[16]; for (int i = 0; i < t.Length; i += 3) { var cc = (uv[t[i]] + uv[t[i + 1]] + uv[t[i + 2]]) / 3f; int tx = Mathf.Clamp((int)(Mathf.Repeat(cc.x, 1) * 4), 0, 3), ty = Mathf.Clamp((int)((1 - Mathf.Repeat(cc.y, 1)) * 4), 0, 3); hist[ty * 4 + tx]++; }
    sb.Append($"{n} glowVerts={c} tris={t.Length / 3} inGlassRect={inRect} subs={m.subMeshCount} readable={m.isReadable} hist(row,col)={string.Join(",", hist)}\n");
}
return sb.ToString();
