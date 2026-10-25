// 중심선 2m 마다, 좌우 ±1.5m 에서 위(+6m)에서 아래로 쏴 맨 위 콜라이더가 Road_* 인지, 노면과 중심선 높이 차
var cl = UnityEngine.Object.FindFirstObjectByType<CourseCenterline>();
UnityEngine.Physics.SyncTransforms();
int bad = 0, total = 0; float maxDy = 0; var sb = new System.Text.StringBuilder();
for (int i = 0; i < cl.points.Length; i++) {
  var p = cl.points[i]; var f = cl.forwards[i]; var r = new UnityEngine.Vector3(f.z, 0, -f.x);
  foreach (var off in new float[] { -1.5f, 0f, 1.5f }) {
    total++;
    var o = p + r * off + UnityEngine.Vector3.up * 6f;
    if (!UnityEngine.Physics.Raycast(o, UnityEngine.Vector3.down, out var h, 20f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) { bad++; sb.Append($"MISS {p}\n"); continue; }
    if (!h.collider.name.StartsWith("Road_")) { bad++; if (bad < 15) sb.Append($"TOP {h.collider.name} at {p.x:F0},{p.z:F0} off{off} dy{h.point.y - p.y:F2}\n"); continue; }
    maxDy = UnityEngine.Mathf.Max(maxDy, UnityEngine.Mathf.Abs(h.point.y - p.y));
  }
}
return $"rays {total} nonRoadTop {bad} maxRoadDy {maxDy:F3}\n" + sb;
