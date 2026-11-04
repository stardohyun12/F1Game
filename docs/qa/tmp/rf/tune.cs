// 윤곽선 값 런타임 조정(.cs 수정 아님) — SessionState rf.tune = "depth,crease,dark,ink"
var a = UnityEditor.SessionState.GetString("rf.tune", "0.04,0.75,0.45,0.5").Split(',');
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
var ci = System.Globalization.CultureInfo.InvariantCulture;
g.outlineDepth = float.Parse(a[0], ci); g.outlineCrease = float.Parse(a[1], ci); g.outlineDark = float.Parse(a[2], ci); g.outlineInk = float.Parse(a[3], ci);
var mi = typeof(A7ShadeGlobals).GetMethod("SetOutline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
mi.Invoke(g, new object[] { false }); mi.Invoke(g, new object[] { g.roomOutline });
return "tune " + g.outlineDepth + " " + g.outlineCrease + " " + g.outlineDark + " " + g.outlineInk + " on=" + (g.Outline != null);
