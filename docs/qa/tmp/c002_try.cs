var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
float x=float.Parse(UnityEditor.SessionState.GetString("c002.x","185")), z=float.Parse(UnityEditor.SessionState.GetString("c002.z","-69"));
RaycastHit h; Physics.Raycast(new Vector3(x,60,z), Vector3.down, out h, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
d.Player.Teleport(h.point, 77f);
d.Player.Cam.LookAt(d.Boss.position + Vector3.up*2f);
return "at " + h.point.ToString("F1") + " on " + h.collider.name;
