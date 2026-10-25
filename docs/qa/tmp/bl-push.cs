var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var mi = typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
mi.SetValue(car, new Vector2(0f, 1f));
var sb = new System.Text.StringBuilder("enabled=" + car.enabled + "\n");
foreach (var f in typeof(ArcadeCarController).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
  if (f.FieldType == typeof(bool) || f.FieldType == typeof(float) || f.FieldType == typeof(int)) sb.Append(f.Name + "=" + f.GetValue(car) + " ");
var cargo = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(m => m.GetType().Name.Contains("Cargo") || m.GetType().Name.Contains("Run")).Select(m => m.GetType().Name + (m.enabled ? "" : "(off)"));
sb.Append("\n" + string.Join(",", cargo));
return sb.ToString();
