// Play: 노을 그늘색·물 차가움 시험(__SH__ = 그늘 rgb, __COOL__ = 물 _Cool.x, __CC__ = _CoolCol rgb) → Apply
var sl = SunsetLook.Find();
sl.shade = new Color(__SH__);
sl.Apply();
var m = sl.seaMaterial;
var c = m.GetVector("_Cool"); c.x = __COOL__f; m.SetVector("_Cool", c);
m.SetColor("_CoolCol", new Color(__CC__));
return "shade=" + sl.shade + " cool=" + m.GetVector("_Cool") + " coolCol=" + m.GetColor("_CoolCol");
