var c = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Splines.SplineContainer>();
var sb = new System.Text.StringBuilder();
for (int si = 0; si < c.Splines.Count; si++) {
  var sp = c.Splines[si]; int n = 400; UnityEngine.Vector3 prev = default; float acc=0;
  for (int i = 0; i <= n; i++) {
    var p3 = UnityEngine.Splines.SplineUtility.EvaluatePosition(sp, i/(float)n); var p = new UnityEngine.Vector3(p3.x,p3.y,p3.z);
    if (i>0){ float dxz = new UnityEngine.Vector2(p.x-prev.x,p.z-prev.z).magnitude; acc+=dxz; float g = (p.y-prev.y)/UnityEngine.Mathf.Max(dxz,1e-3f);
      if (i%10==0) { float terr = BuildHilltopSlice.HilltopHeight(p.x,p.z); sb.Append($"s{si} {acc:F0}m ({p.x:F0},{p.y:F1},{p.z:F0}) g{g*100:F0}% orig{terr:F1}\n"); } }
    prev = p; }
}
return sb.ToString();
