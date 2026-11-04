var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var t = d.PackSpot.V3Table; var parcel = t.receive.today[0];
d.SimInteract();
if (!t.Picked) t.SimUse(parcel);
d.DebugTeleport(new Vector3(273.2f, 31.1f, -29.5f), 90f, null, 0f);
d.Player.Cam.LookAt(t.workSpot.position);
return "picked=" + t.Picked;
