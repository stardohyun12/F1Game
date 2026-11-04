var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var doors = UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
var sb = new System.Text.StringBuilder();
foreach (var it in list)
{
    float yaw = RoomDiorama.FixedYaw(it, list, doors, null);
    bool wide = it.room == null && Mathf.Max(it.half.x, it.half.z) > 3f;
    float d = it.room != null ? 5f : Mathf.Clamp(1.6f * Mathf.Max(it.half.x, it.half.z), 5f, 12f);
    var pose = RoomDiorama.FixedPose(it, yaw, Vector3.zero, 50f, d, 0f, false);
    sb.AppendLine(it.name + " yaw=" + yaw.ToString("F1") + " wide=" + wide + " dist=" + d.ToString("F1") + " camBox=" + it.box.MultiplyPoint3x4(pose.position).ToString("F2") + " cut=" + RoomDiorama.CanCut(it.building));
}
var m = typeof(WorldInvariants).GetMethod("InteriorFixedView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var bad = (System.Collections.Generic.List<string>)m.Invoke(null, null);
sb.AppendLine("invariant fails=" + bad.Count);
foreach (var b in bad) sb.AppendLine("  " + b);
return sb.ToString();
