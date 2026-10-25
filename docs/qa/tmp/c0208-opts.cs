// 인자 파일 docs/qa/tmp/c0208-opts.txt: "A 1 1" = 차 운전자 가방
var a = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/c0208-opts.txt")).Trim().Split(' ');
UnityEditor.SessionState.SetString("c.use", a[0]); UnityEditor.SessionState.SetBool("c.drv", a[1] == "1"); UnityEditor.SessionState.SetBool("c.cargo", a[2] == "1");
return "opts " + string.Join(" ", a);
