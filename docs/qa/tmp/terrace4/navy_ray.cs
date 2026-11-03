// c2_cart 카메라(3차와 같은 자리)에서 남색 판 픽셀 쪽으로 광선 → 맞는 메시 삼각형(콜라이더 없이 렌더러 메시 직접)
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};
try{
var cam=go.AddComponent<Camera>();cam.enabled=false;cam.fieldOfView=70;cam.aspect=16f/9f;cam.nearClipPlane=.05f;cam.farClipPlane=3000;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);
go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
var pix=new[]{new Vector2(1100,620),new Vector2(1200,520),new Vector2(1000,690),new Vector2(1240,330)};
foreach(var px in pix){
 var ray=cam.ViewportPointToRay(new Vector3(px.x/1280f,px.y/720f,0));
 float best=1e9f;string hit="none";
 foreach(var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
  var r=mf.GetComponent<Renderer>();if(!r||!r.enabled||!mf.gameObject.activeInHierarchy||!mf.sharedMesh)continue;
  if(!r.bounds.IntersectRay(ray,out float bd)||bd>best)continue;
  Mesh m=mf.sharedMesh;Vector3[] vs;int[] tr;try{vs=m.vertices;tr=m.triangles;}catch{continue;}
  var lt=mf.transform;var lo=lt.InverseTransformPoint(ray.origin);var ld=lt.InverseTransformDirection(ray.direction);
  for(int t=0;t<tr.Length;t+=3){var a=vs[tr[t]];var b=vs[tr[t+1]];var c=vs[tr[t+2]];
   var e1=b-a;var e2=c-a;var p=Vector3.Cross(ld,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-9f)continue;float inv=1/det;var s=lo-a;float u=Vector3.Dot(s,p)*inv;if(u<0||u>1)continue;var q=Vector3.Cross(s,e1);float v=Vector3.Dot(ld,q)*inv;if(v<0||u+v>1)continue;float tt=Vector3.Dot(e2,q)*inv;if(tt<=0)continue;
   var w=lt.TransformPoint(lo+ld*tt);float dist=(w-ray.origin).magnitude;if(dist<best){best=dist;hit=Path(mf.transform)+" mesh="+m.name+" mat="+(r.sharedMaterial?r.sharedMaterial.name:"null")+" at="+w.ToString("F1")+" bounds="+r.bounds.min.ToString("F0")+"~"+r.bounds.max.ToString("F0");}}
 }
 foreach(var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)){if(sr.enabled&&sr.bounds.IntersectRay(ray,out float bd2)&&bd2<best)sb.AppendLine("  sprite? "+Path(sr.transform)+" d="+bd2);}
 sb.AppendLine(px+" d="+best.ToString("F1")+" -> "+hit);
}
}finally{UnityEngine.Object.DestroyImmediate(go);}
return sb.ToString();
