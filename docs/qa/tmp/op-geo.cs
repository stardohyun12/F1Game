var sb = new System.Text.StringBuilder();
var all = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.centerline != null && r.centerline.Length > 0).ToArray();
var main = all.First(r => r.roadId == "main").centerline;
var vk = all.First(r => r.roadId == "V_kyoto_back").centerline;
for (int i = 0; i <= 40; i += 5) sb.Append("m" + i + main[i].ToString("F0") + " ");
sb.Append("\nm1497" + main[1497].ToString("F0") + " m1480" + main[1480].ToString("F0") + "\n");
var spot = new Vector3(-26, 3, -134); int bi = 0; float bd = 1e9f;
for (int i = 0; i < 200; i++) { float d = (main[i] - spot).magnitude; if (d < bd) { bd = d; bi = i; } }
sb.Append("spot nearest main#" + bi + main[bi].ToString("F1") + " d=" + bd.ToString("F1") + "\n");
var stop = new Vector3(2, 14, -99); bi = 0; bd = 1e9f;
for (int i = 0; i < vk.Length; i++) { float d = (vk[i] - stop).magnitude; if (d < bd) { bd = d; bi = i; } }
sb.Append("stop nearest vk#" + bi + vk[bi].ToString("F1") + " d=" + bd.ToString("F1") + "\n");
foreach (var n in new[] { "Sea", "MART", "STOP_1_kyoto_tea", "TUT_Garage_PitCrew", "Race", "DeliveryHUD", "BayLoopDriveOnly (임시: 주문 배선 전)" })
{
    var g = GameObject.Find(n); if (g == null) { sb.Append(n + " none\n"); continue; }
    var rs = g.GetComponentsInChildren<Renderer>(); var b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.zero);
    foreach (var r in rs) b.Encapsulate(r.bounds);
    sb.Append(n + " pos" + g.transform.position.ToString("F1") + " bounds c" + b.center.ToString("F0") + " s" + b.size.ToString("F0") + "\n");
}
var dm = UnityEngine.Object.FindFirstObjectByType<DeliveryManager>();
sb.Append("ret=" + (dm.ReturnTarget ? dm.ReturnTarget.name : "null") + "\n");
foreach (var p in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None)) sb.Append(p.name + p.transform.position.ToString("F0") + " ");
sb.Append("\nproto=" + Services.PrototypeMode + " fog=" + RenderSettings.fog + " " + RenderSettings.fogMode + " " + RenderSettings.fogStartDistance + "-" + RenderSettings.fogEndDistance + " sky=" + (RenderSettings.skybox ? RenderSettings.skybox.name : "null"));
var gc = UnityEngine.Object.FindFirstObjectByType<GameContext>(); if (gc) { var so = new UnityEditor.SerializedObject(gc); sb.Append(" ctxProto=" + so.FindProperty("prototypeMode").boolValue); }
for (int a = 0; a < 360; a += 45) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var o = new Vector3(-22, 0, -130) + d * 60; RaycastHit h; float y = Physics.Raycast(o + Vector3.up * 200, Vector3.down, out h, 400) ? h.point.y : -99; sb.Append("\nang" + a + " (" + o.x.ToString("F0") + "," + o.z.ToString("F0") + ") y=" + y.ToString("F1") + " " + (h.collider ? h.collider.name : "")); }
return sb.ToString();
