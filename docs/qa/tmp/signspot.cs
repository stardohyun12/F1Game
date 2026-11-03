var sb = new System.Text.StringBuilder();
System.Func<float, float, float?> gy = (x, z) => { if (UnityEngine.Physics.Raycast(new UnityEngine.Vector3(x, 60f, z), UnityEngine.Vector3.down, out var h, 80f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) return h.point.y; return null; };
var eyes = new[] { new UnityEngine.Vector2(184.8f, -66.2f), new UnityEngine.Vector2(184.6f, -65.0f), new UnityEngine.Vector2(188f, -67f) };
var ey = new float[eyes.Length]; for (int i = 0; i < eyes.Length; i++) ey[i] = (gy(eyes[i].x, eyes[i].y) ?? 25f) + 1.6f;
sb.AppendLine("eye y " + string.Join(",", ey));
for (float z = -82f; z <= -60f; z += 2f) {
  var line = "z=" + z.ToString("F0") + " ";
  for (float x = 192f; x <= 232f; x += 2f) {
    var g = gy(x, z); if (g == null) { line += "?"; continue; }
    int vis = 0;
    for (int i = 0; i < eyes.Length; i++) {
      var to = new UnityEngine.Vector3(x, g.Value + 2.0f, z); var from = new UnityEngine.Vector3(eyes[i].x, ey[i], eyes[i].y);
      if (!UnityEngine.Physics.Linecast(from, to, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) vis++;
    }
    line += vis.ToString();
  }
  sb.AppendLine(line);
}
sb.AppendLine("x: 192..232 step2");
return sb.ToString();
