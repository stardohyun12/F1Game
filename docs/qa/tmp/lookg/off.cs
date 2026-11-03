var s = SunsetLook.Find(); s.SetOn(false);
return "off on=" + s.IsOn + " sky=" + RenderSettings.skybox.name + " sea=" + s.sea.sharedMaterial.name + " seaNear=" + s.seaNear.enabled;
