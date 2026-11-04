// PlayerPrefs "inte" = bed | calendar | desk : 대상 1.0m 앞(방 안쪽)에 세우고 대상 쪽을 보게 한다. 다음 호출(int_cap)에서 target 을 읽는다
Time.timeScale = 1f;
string k = PlayerPrefs.GetString("inte");
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var g = typeof(OnFootDirector).GetField("gaze", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(d) as Gaze;
var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
var it = list.Find(i => i.name == "house_home/ROOM_2F");
Vector3 mid = it.box.inverse.MultiplyPoint3x4(new Vector3(0f, -1f, 0f));
Vector3 c;
if (k == "bed") c = g.bed.Box.center;
else if (k == "calendar") c = g.calendar.transform.position;
else
{
    Renderer best = null;
    foreach (var r in it.building.GetComponentsInChildren<Renderer>())
        if (r.name.IndexOf("desk", System.StringComparison.OrdinalIgnoreCase) >= 0 && it.box.MultiplyPoint3x4(r.bounds.center).magnitude < 1.8f) { best = r; break; }
    if (best == null) return "desk 렌더러 없음";
    c = best.bounds.center;
}
Vector3 dir = mid - c; dir.y = 0f; dir.Normalize();
Vector3 feet = new Vector3(c.x, mid.y + 0.1f, c.z) + dir * 1.0f;
float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
return k + " c=" + c.ToString("F2") + " " + d.DebugTeleport(feet, yaw, null, 0f);
