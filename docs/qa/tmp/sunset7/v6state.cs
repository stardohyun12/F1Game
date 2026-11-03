// 6판 비교용: 도트 하늘 끄기(__ON__ = 0) / 켜기(1). 다른 7판 값은 setup.cs 전이라 6판 그대로.
var look = SunsetLook.Find(); var b = look.skyBands; b.x = __ON__; look.skyBands = b;
return "skyBands=" + look.skyBands;
