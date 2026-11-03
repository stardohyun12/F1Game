// 0141: 형성 순서 흉내 — g 이상 묶음을 다 지운 셈 치고 g 의 GroupRun
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var t = typeof(ParcelBuild); var gr = t.GetMethod("GroupRun", bf); var sn = t.GetMethod("SideNb", bf);
var ps = (System.Collections.IList)t.GetField("cellsOf", bf).GetValue(null);
var pt = ps[0].GetType(); var fg = pt.GetField("group"); var fty = pt.GetField("type"); var fid = pt.GetField("id");
string r = "";
foreach (int g in new[] { 159, 160, 247, 337 })
{
  var lt = typeof(System.Collections.Generic.List<>).MakeGenericType(pt); var mem = (System.Collections.IList)System.Activator.CreateInstance(lt);
  var saved = new System.Collections.Generic.List<(object p, int g, object ty)>();
  foreach (var p in ps) { int pg = (int)fg.GetValue(p); if (pg == g) mem.Add(p); if (pg >= g) { saved.Add((p, pg, fty.GetValue(p))); fg.SetValue(p, -1); fty.SetValue(p, null); } }
  r += "g" + g + " ids " + string.Join(",", System.Linq.Enumerable.Select(System.Linq.Enumerable.Cast<object>(mem), p => fid.GetValue(p))) + " GroupRun=" + gr.Invoke(null, new object[] { mem, ps });
  foreach (var p in mem) { var nb = (System.Collections.IEnumerable)sn.Invoke(null, new object[] { p, ps }); foreach (var q in nb) r += " | " + fid.GetValue(p) + "->" + fid.GetValue(q) + "g" + fg.GetValue(q) + ":" + fty.GetValue(q); }
  r += "\n";
  foreach (var s in saved) { fg.SetValue(s.p, s.g); fty.SetValue(s.p, s.ty); }
}
return r;
