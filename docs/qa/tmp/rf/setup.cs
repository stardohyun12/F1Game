// mode: before = 원본 재질 · A = 단색 사본, 윤곽선 끔 · B = 단색 사본 + 윤곽선. Play 중 런타임 교체(정지하면 되돌아감)
string mode = UnityEditor.SessionState.GetString("rf.mode", "A");
UnityEngine.Transform room = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None)) if (t.name == "ROOM_2F" && t.parent != null) { room = t; break; }
var pairs = System.IO.File.ReadAllLines("Assets/Art/Library/Buildings/room_flat/pairs.txt");
var map = new System.Collections.Generic.Dictionary<UnityEngine.Material, UnityEngine.Material>();
foreach (var line in pairs) {
  var p = line.Split('\t');
  var o = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(p[0]); var f = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(p[1]);
  if (mode == "before") map[f] = o; else map[o] = f;
}
int n = 0;
foreach (var r in room.parent.GetComponentsInChildren<UnityEngine.Renderer>(true)) {
  var ms = r.sharedMaterials; bool ch = false;
  for (int i = 0; i < ms.Length; i++) if (ms[i] != null && map.TryGetValue(ms[i], out var m2)) { ms[i] = m2; ch = true; }
  if (ch) { r.sharedMaterials = ms; n++; }
}
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
g.roomOutline = mode == "B";
UnityEngine.Application.runInBackground = true;
return "mode=" + mode + " swapped=" + n + " indoorDot=" + g.indoorDot + " outline=" + g.roomOutline;
