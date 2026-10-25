// 과제 0344: 지형 메시 경사 분포(면적 가중, 5° 칸). 셰이더처럼 보간 법선 기준 → 삼각형 세 꼭짓점 법선 평균.
var sb = new System.Text.StringBuilder();
foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f => f.GetComponent<Renderer>() != null && f.GetComponent<Renderer>().sharedMaterial != null && f.GetComponent<Renderer>().sharedMaterial.shader.name == "PitCrew/BayLoop/TerrainSlope"))
{
    var m = mf.sharedMesh; if (m == null || !m.isReadable) { sb.AppendLine(mf.name + " unreadable"); continue; }
    var v = m.vertices; var n = m.normals; var t = m.triangles; var tr = mf.transform;
    var bins = new double[19]; double tot = 0;
    for (int i = 0; i < t.Length; i += 3)
    {
        Vector3 a = tr.TransformPoint(v[t[i]]), b = tr.TransformPoint(v[t[i + 1]]), c = tr.TransformPoint(v[t[i + 2]]);
        double area = Vector3.Cross(b - a, c - a).magnitude * .5;
        var nn = tr.TransformDirection((n[t[i]] + n[t[i + 1]] + n[t[i + 2]]).normalized).normalized;
        float deg = Mathf.Acos(Mathf.Clamp01(nn.y)) * Mathf.Rad2Deg;
        bins[Mathf.Min(18, (int)(deg / 5f))] += area; tot += area;
    }
    sb.Append(mf.name + " area=" + tot.ToString("F0") + "m2 :");
    double cum = 0;
    for (int k = 0; k < 19; k++) { if (bins[k] <= 0) continue; cum += bins[k]; sb.Append($" {k * 5}-{k * 5 + 5}°={bins[k] / tot * 100:F1}%(cum{cum / tot * 100:F0})"); }
    sb.AppendLine();
}
return sb.ToString();
