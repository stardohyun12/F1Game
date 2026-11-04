var d = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>();
var fl = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var gl = (System.Collections.Generic.List<Renderer>)typeof(RoomDiorama).GetField("glass", fl).GetValue(d);
var box = (Matrix4x4)typeof(RoomDiorama).GetField("box", fl).GetValue(d);
var sb = new System.Text.StringBuilder("n=" + gl.Count + " ");
foreach (var r in gl) sb.Append(r.name + " q" + box.MultiplyPoint3x4(r.bounds.center).ToString("F2") + " off=" + r.forceRenderingOff + " mat=" + r.sharedMaterial.name + " col=" + r.sharedMaterial.GetColor("_BaseColor") + " sh=" + r.sharedMaterial.shader.name + " | ");
return sb.ToString();
