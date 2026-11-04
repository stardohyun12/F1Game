// 새 불변 조건 지금 결과(0 = 통과)
var m = typeof(WorldInvariants).GetMethod("RoomDioramaTimeLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var now = (System.Collections.Generic.List<string>)m.Invoke(null, null);
return "fails=" + now.Count + " [" + string.Join(" | ", now) + "]";
