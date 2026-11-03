int pass = 0, fail = 0; var bad = new System.Text.StringBuilder();
foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
{
    if (asm.GetName().Name != "PitCrew.Core.Tests") continue;
    foreach (var t in asm.GetTypes())
    foreach (var m in t.GetMethods())
    {
        bool isTest = false;
        foreach (var a in m.GetCustomAttributes(true)) if (a.GetType().Name == "TestAttribute") isTest = true;
        if (!isTest) continue;
        try { var o = m.IsStatic ? null : System.Activator.CreateInstance(t); m.Invoke(o, null); pass++; }
        catch (System.Exception e) { fail++; bad.Append(t.Name + "." + m.Name + ": " + (e.InnerException ?? e).Message + "\n"); }
    }
}
return "pass " + pass + " fail " + fail + "\n" + bad;
