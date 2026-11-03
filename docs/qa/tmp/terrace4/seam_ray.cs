// c2_cart 화소(밝은 비스듬한 선 부근) → 축대 메시의 어느 삼각형인지(법선·위치)
var sb=new System.Text.StringBuilder();
var go=new GameObject("_seam_cam"){hideFlags=HideFlags.HideAndDontSave};
try{
var cam=go.AddComponent<Camera>();cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;cam.aspect=1280f/720f;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
var wall=GameObject.Find("TownV6_Gray/TerraceBlock/RetainingWalls").GetComponent<MeshCollider>();var mesh=wall.sharedMesh;var tri=mesh.triangles;var vs=mesh.vertices;var nm=mesh.normals;
foreach(var px in new[]{new Vector2(318,330),new Vector2(322,380),new Vector2(326,430),new Vector2(300,380),new Vector2(345,380)}){
 var ray=cam.ViewportPointToRay(new Vector3(px.x/1280f,1-px.y/720f,0));RaycastHit h;
 if(wall.Raycast(ray,out h,500)){int t=h.triangleIndex;var n=Vector3.Cross(vs[tri[t*3+1]]-vs[tri[t*3]],vs[tri[t*3+2]]-vs[tri[t*3]]).normalized;sb.AppendLine("px "+px+" hit "+h.point.ToString("F2")+" tri "+t+" faceN "+n.ToString("F2")+" v0 "+vs[tri[t*3]].ToString("F2")+" v1 "+vs[tri[t*3+1]].ToString("F2")+" v2 "+vs[tri[t*3+2]].ToString("F2"));}
 else{var hs=Physics.RaycastAll(ray,500);System.Array.Sort(hs,(a,b)=>a.distance.CompareTo(b.distance));sb.AppendLine("px "+px+" wall miss; all: "+string.Join(" | ",hs.Take(3).Select(x=>{var t=x.collider.transform;var s2=t.name;while(t.parent){t=t.parent;s2=t.name+"/"+s2;}return s2+" @"+x.point.ToString("F2")+" n"+x.normal.ToString("F2");})));}}
}finally{UnityEngine.Object.DestroyImmediate(go);}
return sb.ToString();
