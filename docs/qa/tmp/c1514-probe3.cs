// 1514: OG_G_quay 정점 높이 vs 지형 메시(레이) — 겉면이 땅 밑으로 들어간 곳 세기
var og = GameObject.Find("OG_G_quay"); var m = og.GetComponent<MeshFilter>().sharedMesh; var V = m.vertices;
var terr = GameObject.Find("BayTerrain").GetComponent<MeshCollider>();
int below = 0, n = 0; float worst = 0; Vector3 wp = Vector3.zero; float minY = 99, maxY = -99;
for (int k = 0; k < V.Length; k++)
{
    var w = og.transform.TransformPoint(V[k]); minY = Mathf.Min(minY, w.y); maxY = Mathf.Max(maxY, w.y);
    if (terr.Raycast(new Ray(w + Vector3.up * 20, Vector3.down), out var h, 60f)) { n++; float dd = w.y - h.point.y; if (dd < 0.005f) below++; if (dd < worst) { worst = dd; wp = w; } }
}
var b = og.GetComponent<Renderer>().bounds;
return "verts " + V.Length + " tested " + n + " below/at " + below + " worst " + worst.ToString("F2") + " at " + wp.ToString("F1") + " y " + minY.ToString("F1") + "~" + maxY.ToString("F1") + " bounds " + b.min.ToString("F0") + "~" + b.max.ToString("F0") + " mat " + og.GetComponent<Renderer>().sharedMaterial.name;
