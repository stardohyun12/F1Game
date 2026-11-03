// 남색 판 2: 메시필터가 아닌 그릴거리(월드 캔버스·TMP·라인·스킨·파티클·지형) + c2 광선이 지나는지
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};
try{
var cam=go.AddComponent<Camera>();cam.enabled=false;cam.fieldOfView=70;cam.aspect=16f/9f;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
var ray=cam.ViewportPointToRay(new Vector3(1100/1280f,620/720f,0));
sb.AppendLine("ray o="+ray.origin.ToString("F1")+" d="+ray.direction.ToString("F3"));
foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
 sb.AppendLine("canvas "+Path(c.transform)+" mode="+c.renderMode+" active="+c.gameObject.activeInHierarchy+" en="+c.enabled+" pos="+c.transform.position.ToString("F0")+" scale="+c.transform.lossyScale.ToString("F3"));
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){
 if(!r.enabled||r is MeshRenderer&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh)continue;
 bool hit=r.bounds.IntersectRay(ray,out float d);
 sb.AppendLine("rend "+r.GetType().Name+" "+Path(r.transform)+" b="+r.bounds.min.ToString("F0")+"~"+r.bounds.max.ToString("F0")+" rayHit="+hit+(hit?" d="+d.ToString("F0"):""));
}
// 광선이 지나는 모든 렌더러 bounds(가까운 순)
var list=new System.Collections.Generic.List<(float,string)>();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){if(r.enabled&&r.bounds.IntersectRay(ray,out float d)&&r.bounds.size.magnitude<500)list.Add((d,r.GetType().Name+" "+Path(r.transform)+" b="+r.bounds.min.ToString("F0")+"~"+r.bounds.max.ToString("F0")));}
list.Sort((a,b)=>a.Item1.CompareTo(b.Item1));foreach(var x in list.Take(12))sb.AppendLine("ray-bounds d="+x.Item1.ToString("F0")+" "+x.Item2);
}finally{UnityEngine.Object.DestroyImmediate(go);}
return sb.ToString();
