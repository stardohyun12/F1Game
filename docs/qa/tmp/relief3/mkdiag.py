# concave.cs → diag.cs: 블록 16·27 의 낮은 칸 목록(id x z (지형−S) 길까지거리) → diag_low.txt
import os
D = os.path.dirname(os.path.abspath(__file__))
s = open(os.path.join(D, "concave.cs"), encoding="utf8").read()
s = s.replace('string tag = System.IO.File.ReadAllText(T + "tag.txt").Trim();',
              'string tag = "diag"; var want = new System.Collections.Generic.HashSet<int> { 16, 27 }; var img = new System.Collections.Generic.List<string>();')
s = s.replace('if (dd < -.3f) nLow++;',
              'if (dd < -.3f) { nLow++; if (want.Contains(id)) img.Add($"{id} {x:F0} {z:F0} {dd:F2} {dist:F1}"); }')
s = s.replace('System.IO.File.WriteAllText(T + "concave_" + tag + ".json", sb.ToString());',
              'System.IO.File.WriteAllText(T + "diag_low.txt", string.Join("\\n", img));')
open(os.path.join(D, "diag.cs"), "w", encoding="utf8").write(s)
print("ok")
