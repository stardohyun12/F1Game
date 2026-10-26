var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BuildBayLoop")).FirstOrDefault(x => x != null);
if (t == null) return "NOTYPE";
return (t.GetMethod("Place01Standalone") != null ? "READY " : "OLD ") + System.IO.File.GetLastWriteTime(t.Assembly.Location).ToString("HH:mm:ss");
