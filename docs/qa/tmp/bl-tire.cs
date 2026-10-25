var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var p = typeof(ArcadeCarController).GetProperty("EffectiveMaxSpeedMps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
var sb = new System.Text.StringBuilder("effMax=" + (p != null ? p.GetValue(car) : "?") + "\n");
var ts = typeof(ArcadeCarController).GetField("tireSystem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(car);
if (ts != null) foreach (var pr in ts.GetType().GetProperties()) { try { if (pr.PropertyType.IsPrimitive || pr.PropertyType.IsEnum) sb.Append(pr.Name + "=" + pr.GetValue(ts) + " "); } catch { } }
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
sb.Append("\nmoveInput=" + mi.GetValue(car));
return sb.ToString();
