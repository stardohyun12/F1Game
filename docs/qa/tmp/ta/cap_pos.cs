// 그림3 자리(보관소 뒤 T자 틈 27.5 cm, (279, 31.2, -25)) — 9 m 북쪽에서 내려다본다. 전/후 같은 자리·같은 시선.
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d == null) return "감독 없음";
Vector3 gap = new Vector3(279f, 31.2f, -25f);
Vector3 stand = new Vector3(279f, 40f, -16f);
if (Physics.Raycast(stand + Vector3.up * 30f, Vector3.down, out var h, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) stand.y = h.point.y + 0.3f;
var r = d.DebugTeleport(stand, 180f, new Vector3(273.5f, 29.6f, -37.4f), 90f);
var cam = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
if (cam != null) cam.LookAt(gap);
return r + " · 땅 " + h.collider?.name + " y=" + h.point.y.ToString("F2");
