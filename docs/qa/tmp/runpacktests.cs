var t = new PackRulesTests(); var sb = new System.Text.StringBuilder(); int ok = 0, bad = 0;
foreach (var m in typeof(PackRulesTests).GetMethods())
{
    if (m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false).Length == 0) continue;
    try { m.Invoke(t, null); ok++; }
    catch (System.Exception e) { bad++; sb.AppendLine("FAIL " + m.Name + ": " + (e.InnerException ?? e).Message); }
}
return $"PackRulesTests ok {ok} fail {bad}\n" + sb;
