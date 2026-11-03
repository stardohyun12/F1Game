var src=UnityEditor.SceneView.lastActiveSceneView.camera;
var go=new GameObject("_tmpR"); var c=go.AddComponent<Camera>(); c.CopyFrom(src); c.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation); c.pixelRect=new Rect(0,0,1060,648);
var sb=new System.Text.StringBuilder();
foreach(var p in new Vector2[]{ new Vector2(540,560), new Vector2(600,590), new Vector2(560,630), new Vector2(240,580), new Vector2(490,200), new Vector2(455,180), new Vector2(200,300), new Vector2(60,110), new Vector2(620,560)}){
  var ray=c.ScreenPointToRay(new Vector3(p.x, 648-p.y, 0));
  if(Physics.Raycast(ray, out var h, 2000f)) sb.AppendLine(p+" -> "+h.point.ToString("F1")+" "+h.collider.name+" par="+(h.collider.transform.parent?h.collider.transform.parent.name:""));
  else { float t=-ray.origin.y/ray.direction.y; sb.AppendLine(p+" -> sea "+(ray.origin+ray.direction*t).ToString("F1")); }
}
UnityEngine.Object.DestroyImmediate(go); return sb.ToString();
