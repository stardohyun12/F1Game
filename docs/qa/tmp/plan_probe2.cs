var sb = new System.Text.StringBuilder();
var par = UnityEngine.GameObject.Find("TownV6_Gray/D198_Buildings").transform;
foreach (UnityEngine.Transform r in par) {
  var rs = r.GetComponentsInChildren<UnityEngine.Renderer>(true);
  var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
  // local-space bounds (yaw-removed) like the web viewer
  var q = UnityEngine.Quaternion.Inverse(r.rotation);
  UnityEngine.Vector3 mn = UnityEngine.Vector3.one * 1e9f, mx = -mn;
  foreach (var x in rs) { var mf = x.GetComponent<UnityEngine.MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
    foreach (var v in mf.sharedMesh.vertices) { var w = x.transform.TransformPoint(v); var l = q * (w - r.position); mn = UnityEngine.Vector3.Min(mn, l); mx = UnityEngine.Vector3.Max(mx, l); } }
  var cl = (mn + mx) * 0.5f; var cw = r.position + r.rotation * new UnityEngine.Vector3(cl.x, 0, cl.z);
  sb.Append(r.name + " root=" + r.position.ToString("F2") + " yaw=" + r.eulerAngles.y.ToString("F1") + " child0=" + r.GetChild(0).name + " lp=" + r.GetChild(0).localPosition.ToString("F2") + " lr=" + r.GetChild(0).localEulerAngles.ToString("F0") + " worldBB=" + b.center.ToString("F2") + " min.y=" + b.min.y.ToString("F2") + " localC=" + cl.ToString("F2") + " bottomCenterWorld=(" + cw.x.ToString("F2") + "," + (r.position.y + mn.y).ToString("F2") + "," + cw.z.ToString("F2") + ")\n");
}
return sb.ToString();
