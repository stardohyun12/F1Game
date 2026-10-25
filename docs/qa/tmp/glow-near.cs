// Play 중: 02 상점가 지점 40m 안 건물 — 텍스처·창 발광 정점 수
var p0 = new Vector3(56, 23, -43);
var glow = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "WindowGlow").GetComponent<MeshFilter>().sharedMesh.vertices;
var bld = GameObject.Find("Buildings").transform; var sb = new System.Text.StringBuilder();
foreach (Transform h in bld)
{
    var rs = h.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    if (Vector3.Distance(b.ClosestPoint(p0), p0) > 40f) continue;
    var tex = string.Join(",", rs.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.HasProperty("_MainTex") && m.GetTexture("_MainTex") ? m.GetTexture("_MainTex").name : m.shader.name).Distinct());
    var mf = h.GetComponentInChildren<MeshFilter>();
    sb.Append($"{h.name} glow={glow.Count(v => b.Contains(v))} tex={tex} mesh={mf?.sharedMesh?.name}\n");
}
return sb.ToString();
