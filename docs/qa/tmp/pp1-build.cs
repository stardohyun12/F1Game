var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BuildProtoPlay01")).FirstOrDefault(x => x != null);
if (t == null) return "NOTYPE (아직 컴파일 안 됨)";
return (string)t.GetMethod("Build").Invoke(null, null);
