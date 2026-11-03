// 0141: 구덩이 자리 남은 땅 종류 + P1422 둘레
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var t = typeof(ParcelBuild); var ll = (System.Collections.IList)t.GetField("lastLefts", bf).GetValue(null);
string r = "";
foreach (var l in ll) { var lt = l.GetType(); var c = (UnityEngine.Vector2)lt.GetField("ctr").GetValue(l); if ((c - new UnityEngine.Vector2(213, -41)).magnitude < 20) r += lt.GetField("kind").GetValue(l) + " area " + lt.GetField("area").GetValue(l) + " @" + c + "\n"; }
var ps = (System.Collections.IList)t.GetField("cellsOf", bf).GetValue(null);
foreach (var p in ps) { var pt = p.GetType(); var c = (UnityEngine.Vector2)pt.GetField("ctr").GetValue(p); if ((c - new UnityEngine.Vector2(213, -41)).magnitude < 16) r += "P" + pt.GetField("id").GetValue(p) + " @" + c + " empty=" + pt.GetField("empty").GetValue(p) + " back=" + pt.GetField("back").GetValue(p) + " mass=" + (pt.GetField("mass").GetValue(p) != null) + " area=" + pt.GetField("area").GetValue(p) + "\n"; }
return r + ParcelBuild.Ascii(1422);
