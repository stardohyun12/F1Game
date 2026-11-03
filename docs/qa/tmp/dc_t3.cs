var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var sp = GameObject.Find("OnFoot_Spawn_Room").transform;
Vector3 to = d.Bed.Anchor - sp.position; to.y = 0f;
float yaw = Quaternion.LookRotation(to).eulerAngles.y;
string r = d.DebugTeleport(sp.position, yaw, null, 0f);
d.Player.Cam.Pitch = 40f;
return r + " | bedAnchor=" + d.Bed.Anchor.ToString("F2") + " state=" + d.State;
