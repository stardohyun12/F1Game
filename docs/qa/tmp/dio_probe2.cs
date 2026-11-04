var sb = new System.Text.StringBuilder();
UnityEngine.Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None)) if (t.name == "ROOM_2F" && t.parent != null) room = t;
var house = room.parent;
A7ShadeGlobals.TryRoomBox(room, out var box);
sb.AppendLine("== house_home/house_home kids touching box (pad 1.3)");
foreach (UnityEngine.Transform c in house.Find("house_home")) { var r = c.GetComponent<UnityEngine.Renderer>(); if (!r) { sb.AppendLine(" (no r) " + c.name + " kids=" + c.childCount); continue; } var q = box.MultiplyPoint3x4(r.bounds.center); if (UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(q.x), UnityEngine.Mathf.Abs(q.y), UnityEngine.Mathf.Abs(q.z)) > 1.6f) continue; sb.AppendLine(" " + c.name + " act=" + c.gameObject.activeSelf + " q=" + q.ToString("F2") + " sz=" + r.bounds.size.ToString("F2") + " m=" + (r.sharedMaterial? r.sharedMaterial.name:"")); }
sb.AppendLine("== room/room kids");
foreach (UnityEngine.Transform c in room.Find("room")) { var r = c.GetComponent<UnityEngine.Renderer>(); var q = r ? box.MultiplyPoint3x4(r.bounds.center) : UnityEngine.Vector3.zero; sb.AppendLine(" " + c.name + " act=" + c.gameObject.activeSelf + (r? " q=" + q.ToString("F2") + " sz=" + r.bounds.size.ToString("F2") + " m=" + (r.sharedMaterial? r.sharedMaterial.name + "|" + r.sharedMaterial.shader.name:"") : " (no r)") + " kids=" + c.childCount + " comps=" + string.Join(",", System.Linq.Enumerable.Select(c.GetComponents<UnityEngine.Component>(), k => k.GetType().Name))); }
return sb.ToString();
