// 방 바닥+벽 픽셀 가리개: 화면 격자마다 카메라 레이를 방 상자 좌표로 옮겨, 잘린 면(천장·카메라 쪽 벽)으로 들어와 안쪽 면(바닥·벽)에 닿는 점만.
// 가구·플레이어가 가리면 뺀다(들어온 점부터 물리 레이). 결과 JSON: [u, v, 면(0 바닥, 1 +x, 2 -x, 3 +z, 4 -z), 전등까지 m, 창 벽?]
var d = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>();
var cam = d.GetComponent<Camera>();
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (t.name == g.roomName && t.parent != null) { room = t; break; }
Matrix4x4 box;
if (g.IndoorBox != Matrix4x4.zero) box = g.IndoorBox; else A7ShadeGlobals.TryRoomBox(room, out box);
var inv = box.inverse;
Vector3 lamp = Vector3.zero; float lampR = 0f;
var lg = GameObject.Find("DIO_DeskLamp");
if (lg != null) { lamp = lg.transform.position; lampR = lg.GetComponent<Light>().range; }
var wq = box.MultiplyPoint3x4(d.WindowCenter);
int winFace = Mathf.Abs(wq.x) > Mathf.Abs(wq.z) ? (wq.x > 0 ? 1 : 2) : (wq.z > 0 ? 3 : 4);
var c = box.MultiplyPoint3x4(cam.transform.position);
var pl = (Transform)typeof(RoomDiorama).GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(d);
var pb = new Bounds(pl.position + Vector3.up * 0.9f, new Vector3(0.7f, 1.9f, 0.7f));
var sb = new System.Text.StringBuilder("{\"lampR\":" + lampR.ToString("F2") + ",\"winFace\":" + winFace + ",\"pts\":[");
int n = 0, GX = 320, GY = 180;
for (int iy = 0; iy < GY; iy++) for (int ix = 0; ix < GX; ix++)
{
    float u = (ix + 0.5f) / GX, v = (iy + 0.5f) / GY;
    var ray = cam.ViewportPointToRay(new Vector3(u, v, 0));
    Vector3 o = box.MultiplyPoint3x4(ray.origin), dd = box.MultiplyVector(ray.direction);
    float te = -1e9f, tx = 1e9f; int ea = -1, xa = -1;
    bool miss = false;
    for (int k = 0; k < 3; k++)
    {
        if (Mathf.Abs(dd[k]) < 1e-6f) { if (Mathf.Abs(o[k]) > 1f) miss = true; continue; }
        float t0 = (-1f - o[k]) / dd[k], t1 = (1f - o[k]) / dd[k];
        float lo = Mathf.Min(t0, t1), hi = Mathf.Max(t0, t1);
        if (lo > te) { te = lo; ea = k; }
        if (hi < tx) { tx = hi; xa = k; }
    }
    if (miss || te >= tx || te < 0) continue;
    // 들어오는 면 = 천장(+y) 또는 카메라 쪽 벽
    Vector3 pe = o + dd * te;
    bool cutIn = (ea == 1 && pe.y > 0) || (ea == 0 && Mathf.Abs(c.x) > 1f && Mathf.Sign(pe.x) == Mathf.Sign(c.x)) || (ea == 2 && Mathf.Abs(c.z) > 1f && Mathf.Sign(pe.z) == Mathf.Sign(c.z));
    if (!cutIn) continue;
    if (ea == 1 && Mathf.Max(Mathf.Abs(pe.x), Mathf.Abs(pe.z)) > 0.94f) continue;   // 잘린 벽 단면(벽 두께 윗면) 빼기
    if (ea != 1 && pe.y > 0.85f) continue;
    if (pb.IntersectRay(ray)) continue;   // 플레이어
    Vector3 px = o + dd * tx;
    int face;
    if (xa == 1) { if (px.y > 0) continue; face = 0; }
    else if (xa == 0) face = px.x > 0 ? 1 : 2; else face = px.z > 0 ? 3 : 4;
    // 잘린 벽(카메라 쪽)에 닿은 점은 안 그려지므로 뺀다
    if (face == 1 && c.x > 1f) continue; if (face == 2 && c.x < -1f) continue;
    if (face == 3 && c.z > 1f) continue; if (face == 4 && c.z < -1f) continue;
    Vector3 we = inv.MultiplyPoint3x4(pe), wx = inv.MultiplyPoint3x4(px);
    Vector3 dir = (wx - we); float len = dir.magnitude; dir /= len;
    Vector3 start = we + dir * 0.15f;
    if (Physics.Raycast(start, dir, out var hit, Mathf.Max(0f, len - 0.25f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
    float ld = lampR > 0 ? Vector3.Distance(wx, lamp) : 99f;
    if (n++ > 0) sb.Append(',');
    sb.Append("[" + u.ToString("F4") + "," + v.ToString("F4") + "," + face + "," + ld.ToString("F2") + "," + (face == winFace ? 1 : 0) + "]");
}
sb.Append("]}");
System.IO.File.WriteAllText("D:/Github/F1Game/docs/qa/tmp/rli/mask.json", sb.ToString());
return "mask pts " + n + " winFace " + winFace + " lampR " + lampR + " cam " + cam.transform.position.ToString("F2") + " camBox " + c.ToString("F2");
