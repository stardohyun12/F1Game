var b = PitCrew.PackBench.PackBench.I;
var tray = b.Tray();
b.SimActive = true;
b.SimGrab(tray);
b.SimTarget = tray.Body.position + UnityEngine.Vector3.up * 0.10f;
return "grabbed at " + tray.Body.position.ToString("F3") + " target " + b.SimTarget.Value.ToString("F3") + " held=" + (b.Held != null) + " kin=" + tray.Body.isKinematic + " legacy=" + PitCrew.PackBench.PackBench.LegacyHold;