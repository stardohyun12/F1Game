// 20261030-2334: 새 house_home 안의 문·방·계단·난간 위치(월드 + 집 로컬)
var log = new System.Text.StringBuilder();
var h = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "house_home" && t.parent != null && t.parent.name == "D198_Buildings");
Physics.SyncTransforms();
foreach (var t in h.GetComponentsInChildren<Transform>(true)) {
  var n = t.name; if (!(n.StartsWith("DOOR_") || n.StartsWith("RAIL") || n == "STAIRS" || n == "BED_01" || n == "BED_02" || n == "ROOM_2F" || n == "DESK_01" || n.StartsWith("KNOB"))) continue;
  var r = t.GetComponent<Renderer>(); var b = r ? r.bounds : new Bounds(t.position, Vector3.zero);
  var lc = h.InverseTransformPoint(b.center);
  log.Append(n + " w=" + b.center.ToString("F2") + " size=" + b.size.ToString("F2") + " local=" + lc.ToString("F2") + " pivotLocal=" + h.InverseTransformPoint(t.position).ToString("F2") + "\n"); }
var sp = h.TransformPoint(new Vector3(0f, 3.25f, 1.35f));
log.Append("spawnOld w=" + sp.ToString("F2"));
if (Physics.Raycast(sp + Vector3.up * 0.5f, Vector3.down, out var hit, 1.5f)) log.Append(" floor " + hit.point.y.ToString("F2") + " " + hit.collider.name);
return log.ToString();
