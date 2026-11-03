// 남색 판 3: 숨김(hideFlags) 오브젝트까지 — Resources.FindObjectsOfTypeAll 로 씬에 붙은 렌더러 중 c2 광선과 만나는 것
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
var ray=new Ray(new Vector3(188.4f,19.4f,-82.4f),new Vector3(0.827f,0.542f,0.148f).normalized);
int hidden=0;
foreach(var r in Resources.FindObjectsOfTypeAll<Renderer>()){
 if(UnityEditor.EditorUtility.IsPersistent(r))continue;
 bool hid=(r.gameObject.hideFlags&HideFlags.HideInHierarchy)!=0;if(hid)hidden++;
 bool hit=r.bounds.IntersectRay(ray,out float d);
 if(hid||hit&&d>0)sb.AppendLine((hid?"HIDDEN ":"")+r.GetType().Name+" "+Path(r.transform)+" flags="+r.gameObject.hideFlags+" scene="+r.gameObject.scene.name+" active="+r.gameObject.activeInHierarchy+" en="+r.enabled+" b="+r.bounds.min.ToString("F0")+"~"+r.bounds.max.ToString("F0")+" hit="+hit+" mat="+(r.sharedMaterial?r.sharedMaterial.name:"null"));
}
sb.AppendLine("hidden="+hidden);
// 광원·카메라 목록 (남색 = 그림자 볼륨?)
foreach(var c in Resources.FindObjectsOfTypeAll<Camera>())if(!UnityEditor.EditorUtility.IsPersistent(c))sb.AppendLine("cam "+Path(c.transform)+" flags="+c.gameObject.hideFlags+" en="+c.enabled);
return sb.ToString();
