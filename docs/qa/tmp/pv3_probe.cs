var sb = new System.Text.StringBuilder();
sb.Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path).Append(" play=").Append(UnityEditor.EditorApplication.isPlaying).Append("\n");
var ps = UnityEngine.GameObject.Find("OnFoot_HomeGarage/OnFoot_Depot");
foreach (UnityEngine.Transform c in ps.transform) {
  sb.Append(c.name).Append(" ").Append(c.position.ToString("F2")).Append(" rot ").Append(c.eulerAngles.ToString("F0")).Append(" act=").Append(c.gameObject.activeSelf).Append("\n");
  foreach (UnityEngine.Transform g in c) { var r = g.GetComponentsInChildren<UnityEngine.Renderer>(); UnityEngine.Bounds b = r.Length>0? r[0].bounds : new UnityEngine.Bounds(g.position, UnityEngine.Vector3.zero); foreach (var x in r) b.Encapsulate(x.bounds);
    sb.Append("   ").Append(g.name).Append(" ").Append(g.position.ToString("F2")).Append(" b=").Append(b.min.ToString("F2")).Append("~").Append(b.max.ToString("F2")).Append(" act=").Append(g.gameObject.activeSelf).Append("\n"); }
}
// floor + walls: ray down & 4 dirs from depot interior center
var o = new UnityEngine.Vector3(272f, 32.5f, -30.5f);
UnityEngine.RaycastHit h;
if (UnityEngine.Physics.Raycast(o, UnityEngine.Vector3.down, out h, 5f)) sb.Append("floor ").Append(h.point.y.ToString("F2")).Append(" ").Append(h.collider.name).Append("\n");
foreach (var d in new[]{UnityEngine.Vector3.left, UnityEngine.Vector3.right, UnityEngine.Vector3.forward, UnityEngine.Vector3.back})
  if (UnityEngine.Physics.Raycast(o, d, out h, 12f)) sb.Append("dir ").Append(d.ToString("F0")).Append(" ").Append(h.point.ToString("F2")).Append(" ").Append(h.collider.name).Append("\n");
return sb.ToString();
