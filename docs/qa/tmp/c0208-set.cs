// 인자 파일: docs/qa/tmp/c0208-set.txt 한 줄 "A 1.2,1.2,1.2 0.65"
var line = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/c0208-set.txt")).Trim().Split(' ');
UnityEditor.SessionState.SetString("c.mat", line[0]); UnityEditor.SessionState.SetString("c.tint", line[1]); UnityEditor.SessionState.SetFloat("c.face", float.Parse(line[2]));
return "set " + string.Join(" ", line);
