// 지형 메시 속성 + 칼날 자리 3 m 안 정점 색/UV/법선 + 꿰매기가 붙인 뒤쪽 정점 색
var t = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var m = t.GetComponent<MeshFilter>().sharedMesh; var tr = t.transform;
var sb = new System.Text.StringBuilder();
for (int a = 0; a < 14; a++) { var at = (UnityEngine.Rendering.VertexAttribute)a; if (m.HasVertexAttribute(at)) sb.Append(at).Append(' ').Append(m.GetVertexAttributeDimension(at)).Append(' '); }
var mat = t.GetComponent<Renderer>().sharedMaterial; sb.Append("\nshader ").Append(mat.shader.name).Append(" tex ").Append(mat.mainTexture != null ? mat.mainTexture.name : "-").Append('\n');
var V = m.vertices; var C = m.colors; var U = m.uv; var N = m.normals;
sb.Append($"v {V.Length} colors {C.Length} uv {U.Length} normals {N.Length}\n");
int shown = 0; var hist = new System.Collections.Generic.Dictionary<string, int>();
for (int i = 0; i < V.Length; i++)
{
    var w = tr.TransformPoint(V[i]); if (Mathf.Abs(w.x - 279f) > 3f || Mathf.Abs(w.z + 25f) > 3f) continue;
    string key = (C.Length > i ? ((Color32)C[i]).ToString() : "-") + " uv" + (U.Length > i ? U[i].ToString("F2") : "-");
    hist[key] = hist.TryGetValue(key, out int c) ? c + 1 : 1;
    if (shown++ < 0) sb.Append(i).Append(' ').Append(w.ToString("F2")).Append(' ').Append(key).Append(" n").Append(N.Length > i ? N[i].ToString("F2") : "-").Append('\n');
}
foreach (var kv in hist) sb.Append(kv.Value).Append(" × ").Append(kv.Key).Append('\n');
var tail = new System.Collections.Generic.Dictionary<string, int>();
for (int i = V.Length - 2006; i < V.Length; i++) { string k = C.Length > i ? ((Color32)C[i]).ToString() : "-"; tail[k] = tail.TryGetValue(k, out int c) ? c + 1 : 1; }
sb.Append("꿰매기 중심점 색: "); foreach (var kv in tail) sb.Append(kv.Value).Append("×").Append(kv.Key).Append(' ');
return sb.ToString();
