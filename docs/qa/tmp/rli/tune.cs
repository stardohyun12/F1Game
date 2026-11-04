// 실행 중 값 맞추기(.cs 를 안 고치고): 유리 양면 + 밤 바탕빛
var d = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>();
var fl = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var gm = (Material)typeof(RoomDiorama).GetField("glassMat", fl).GetValue(d);
gm.SetFloat("_Cull", 0f);
var a = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/rli/tune.txt").Trim().Split(' ');
d.ambientNight = new Color(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2]));
return "cull0 ambNight " + d.ambientNight;
