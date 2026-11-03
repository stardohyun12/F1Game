// 20261030-2334: house_home 평면 점유도(집 로컬 x·z, 0.15 m 칸). # = 막힘(바닥 위 0.3~1.3 m), D = 문짝, S = 시작점, . = 빈 곳
// 위가 +z(앞·발코니), 왼쪽이 +x. 층 = FLOOR_Y(집 로컬)
var h = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "house_home" && t.parent != null && t.parent.name == "D198_Buildings");
float FLOOR_Y = 0.0f; Physics.SyncTransforms(); int mask = ~(1 << 2);
var sb = new System.Text.StringBuilder("floor " + FLOOR_Y + "\n");
var sp = GameObject.Find("OnFoot_HomeGarage/OnFoot_Spawn_Room"); var spl = sp ? h.InverseTransformPoint(sp.transform.position) : Vector3.zero;
for (float z = 4.8f; z >= -3.4f; z -= 0.15f) {
  sb.Append(z.ToString("F2").PadLeft(6) + " ");
  for (float x = 4.6f; x >= -4.6f; x -= 0.15f) {
    if (Mathf.Abs(x - spl.x) < 0.075f && Mathf.Abs(z - spl.z) < 0.075f) { sb.Append('S'); continue; }
    var a = h.TransformPoint(new Vector3(x, FLOOR_Y + 0.3f, z)); var b = h.TransformPoint(new Vector3(x, FLOOR_Y + 1.3f, z));
    var cs = Physics.OverlapCapsule(a, b, 0.05f, mask, QueryTriggerInteraction.Ignore);
    if (cs.Length == 0) sb.Append('.'); else if (cs.Any(c => c.name.StartsWith("DOOR_"))) sb.Append('D'); else if (cs.Any(c => c.name.StartsWith("RAIL"))) sb.Append('r'); else if (cs.Any(c => c.name == "HOUSE_shell")) sb.Append('#'); else sb.Append('o'); }
  sb.Append('\n'); }
sb.Append("x: left=+4.6 right=-4.6");
return sb.ToString();
