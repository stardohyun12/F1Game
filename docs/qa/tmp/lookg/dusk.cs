Application.runInBackground = true;
var s = SunsetLook.Find(); s.dusk = __DUSK__f; if (!s.IsOn) s.SetOn(true); else s.Apply();
return "dusk=" + s.dusk + " az=" + s.SunAzimuthNow + " backdrop=" + (s.backdrop ? s.backdrop.activeSelf.ToString() : "-") + " seaNear=" + s.seaNear.transform.position + " srOn=" + SeaReflection.Find().isActiveAndEnabled;
