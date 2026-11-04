Application.runInBackground = true;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null) return "no dir";
if (d.State == OnFootDirector.Mode.Starting) return "starting";
var t = d.PackSpot.V3Table; var parcel = t.receive.today[0]; var pp0 = parcel.transform.position;
d.DebugTeleport(new Vector3(pp0.x, 31.1f, pp0.z - 1.1f), 0f, null, 0f);
d.Player.Cam.LookAt(pp0);
return "at shelf " + d.State;
