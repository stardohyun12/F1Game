var rc = Services.Run as RunController;
var sb = new System.Text.StringBuilder("timeScale=" + Time.timeScale + " t=" + Time.time.ToString("F1") + " stage=" + rc.CurrentStage + "\n");
foreach (var f in typeof(RunController).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
  if (f.FieldType.IsPrimitive || f.FieldType.IsEnum) sb.Append(f.Name + "=" + f.GetValue(rc) + " ");
return sb.ToString();
