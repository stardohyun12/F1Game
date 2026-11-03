// Play/편집: 차가운 물 _Cool = (__CX__, __CY__), 색 = (__CR__, __CG__, __CB__)
var m = SunsetLook.Find().seaMaterial; m.SetVector("_Cool", new Vector4(__CX__f, __CY__f, 0, 0)); m.SetColor("_CoolCol", new Color(__CR__f, __CG__f, __CB__f));
SeaReflection.Find().enabled = true;
return "cool=" + m.GetVector("_Cool") + " col=" + m.GetColor("_CoolCol");
