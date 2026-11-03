// 캡처 카메라(눈 → (279,31.2,-25))를 다시 세워 칼날 픽셀(945,515 / 1920×1080) 시선에 걸리는 렌더러 bounds 를 가까운 순으로
var eye = new Vector3(279f, 33.58f + 1.6f, -16f); var tgt = new Vector3(279f, 31.2f, -25f);
var go = new GameObject("tmp_ray_cam"); var cam = go.AddComponent<Camera>();
cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(tgt - eye)); cam.pixelRect = new Rect(0, 0, 1920, 1080);
var sb = new System.Text.StringBuilder("fov " + cam.fieldOfView + "\n");
foreach (var px in new[] { new Vector2(945, 1080 - 515), new Vector2(900, 1080 - 480), new Vector2(1000, 1080 - 560) })
{
    var ray = cam.ScreenPointToRay(px);
    var hits = new System.Collections.Generic.List<(float, string)>();
    foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
    {
        if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
        if (r.bounds.IntersectRay(ray, out float dist) && dist < 25f) hits.Add((dist, r.name + "[" + string.Join(",", System.Linq.Enumerable.Select(r.sharedMaterials, x => x == null ? "null" : x.name)) + "] " + r.bounds.size.ToString("F1")));
    }
    hits.Sort((a, b) => a.Item1.CompareTo(b.Item1));
    bool ph = Physics.Raycast(ray, out var h, 40f, ~0, QueryTriggerInteraction.Collide);
    sb.Append(px).Append(" col ").Append(ph ? h.collider.name + " " + h.point.ToString("F2") + " d" + h.distance.ToString("F1") : "-").Append("\n  ");
    foreach (var x in hits) sb.Append(x.Item1.ToString("F1")).Append(' ').Append(x.Item2).Append(" | ");
    sb.Append('\n');
}
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();
