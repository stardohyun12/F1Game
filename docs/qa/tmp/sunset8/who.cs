// Play: 화면의 흰 곡선 정체 — LineRenderer·TrailRenderer 전부와 파문 전역
var sb = new System.Text.StringBuilder(); var cam = Camera.main;
foreach (var l in UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
{
    sb.Append("LR " + l.name + " parent=" + (l.transform.parent ? l.transform.parent.name : "") + " en=" + l.enabled + " n=" + l.positionCount + " world=" + l.useWorldSpace);
    if (l.positionCount > 0) { var p0 = l.GetPosition(0); var p1 = l.GetPosition(l.positionCount - 1); if (!l.useWorldSpace) { p0 = l.transform.TransformPoint(p0); p1 = l.transform.TransformPoint(p1); } sb.Append(" a=" + p0.ToString("F1") + cam.WorldToViewportPoint(p0).ToString("F2") + " b=" + p1.ToString("F1") + cam.WorldToViewportPoint(p1).ToString("F2")); }
    sb.Append(" mat=" + (l.sharedMaterial ? l.sharedMaterial.name : "") + "\n");
}
foreach (var l in UnityEngine.Object.FindObjectsByType<TrailRenderer>(FindObjectsSortMode.None)) sb.Append("TR " + l.name + "\n");
var rp = Shader.GetGlobalVectorArray("_PCRipples"); if (rp != null) foreach (var v in rp) sb.Append("rip " + v.ToString("F2") + "\n");
sb.Append("clock " + Shader.GetGlobalVector("_PCSeaClock") + " t=" + Time.time);
return sb.ToString();
