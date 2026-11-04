var sb = new System.Text.StringBuilder();
var room = UnityEngine.GameObject.Find("ROOM_2F").transform;
A7ShadeGlobals.TryRoomBox(room, out var box);
var inv = box.inverse;
var wb = new UnityEngine.Bounds(inv.MultiplyPoint3x4(UnityEngine.Vector3.zero), UnityEngine.Vector3.zero);
for (int i = 0; i < 8; i++) wb.Encapsulate(inv.MultiplyPoint3x4(new UnityEngine.Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (r.forceRenderingOff || !r.enabled || !r.gameObject.activeInHierarchy || !r.bounds.Intersects(wb)) continue;
    var m = r.sharedMaterial; var sh = m ? m.shader.name : "-";
    if (sh == "PitCrew/SceneA7/Lit") continue;
    var q = box.MultiplyPoint3x4(r.bounds.center);
    sb.AppendLine(r.name + " <" + (r.transform.parent ? r.transform.parent.name : "") + "> " + r.GetType().Name + " " + (m ? m.name : "") + "|" + sh + " q=" + q.ToString("F2") + " sz=" + r.bounds.size.ToString("F1"));
}
return sb.ToString();
