var go = new GameObject("tmpcam"); var cam = go.AddComponent<Camera>();
cam.transform.position = new Vector3(320.028f,84.78f,-104.651f); cam.transform.LookAt(new Vector3(368.382f,-83.23f,-25.557f)); cam.fieldOfView=60; cam.aspect=1280f/611f;
var sb = new System.Text.StringBuilder();
foreach (var px in new[]{new Vector2(655,420),new Vector2(680,410),new Vector2(945,285),new Vector2(400,355),new Vector2(705,370),new Vector2(900,320)}) {
  var r = cam.ViewportPointToRay(new Vector3(px.x/1280f, 1-px.y/611f, 0));
  sb.Append(px+": ");
  foreach (var h in Physics.RaycastAll(r, 300).OrderBy(h=>h.distance).Take(4)) sb.Append(h.collider.name+" "+h.point.ToString("F2")+" | ");
  sb.AppendLine();
}
UnityEngine.Object.DestroyImmediate(go); return sb.ToString();
