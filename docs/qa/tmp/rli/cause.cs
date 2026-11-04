// 원인 가르기: mode.txt = "noshadow" (해 그림자 끔) | "noroof" (방 천장 위로 올라간 렌더러만 그림자 끔) | "restore"
var mode = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/rli/mode.txt").Trim();
var sun = RenderSettings.sun;
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (t.name == g.roomName && t.parent != null) { room = t; break; }
Matrix4x4 box;
if (g.IndoorBox != Matrix4x4.zero) box = g.IndoorBox; else A7ShadeGlobals.TryRoomBox(room, out box);
var sb = new System.Text.StringBuilder(mode + ": ");
if (mode == "noshadow") sun.shadows = LightShadows.None;
if (mode == "restore") sun.shadows = LightShadows.Hard;
int k = 0;
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    var q = box.MultiplyPoint3x4(r.bounds.center);
    if (Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.z)) > 3f) continue;
    var top = box.MultiplyPoint3x4(new Vector3(r.bounds.center.x, r.bounds.min.y, r.bounds.center.z));
    if (top.y < 0.85f) continue;   // 천장 근처 위로만
    if (mode == "noroof" && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; k++; sb.Append(r.name + "(" + r.sharedMaterial?.shader?.name + ") "); }
}
return sb.ToString() + " n=" + k + " sunShadows=" + sun.shadows + " str " + sun.shadowStrength + " shadowDist " + QualitySettings.shadowDistance;
