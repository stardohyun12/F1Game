// terrace3: 흙길(DirtPath_house_field) 가운데줄을 메시에서 찾아, 그 위 지형+보도 높이 단면과 단차를 낸다
var go=GameObject.Find("TownV6_Gray/TerraceBlock/DirtPath_house_field");var m=go.GetComponent<MeshFilter>().sharedMesh;
var tmp=new GameObject("_T3_pathcol"){hideFlags=HideFlags.HideAndDontSave};tmp.transform.SetPositionAndRotation(go.transform.position,go.transform.rotation);tmp.transform.localScale=go.transform.lossyScale;var pc=tmp.AddComponent<MeshCollider>();pc.sharedMesh=m;
var cols=new System.Collections.Generic.List<Collider>();cols.Add(GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>());var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways");if(wk)cols.Add(wk.GetComponent<MeshCollider>());
float Y(Vector2 p){float y=float.NegativeInfinity;foreach(var c in cols){RaycastHit h;if(c.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000))y=Mathf.Max(y,h.point.y);}return y;}
bool OnPath(Vector2 p){RaycastHit h;return pc.Raycast(new Ray(new Vector3(p.x,500,p.y),Vector3.down),out h,1000);}
var sb=new System.Text.StringBuilder();
try{
 var vs=m.vertices;var w=new System.Collections.Generic.List<Vector2>();foreach(var v in vs){var p=go.transform.TransformPoint(v);w.Add(new Vector2(p.x,p.y==p.y?p.z:0));}
 Vector2 lo=w[0],hi=w[0];foreach(var p in w){if(p.y<lo.y)lo=p;if(p.y>hi.y)hi=p;}
 Vector2 A=Vector2.zero,B=Vector2.zero;int na=0,nb=0;foreach(var p in w){if((p-lo).magnitude<1.2f){A+=p;na++;}if((p-hi).magnitude<1.2f){B+=p;nb++;}}A/=na;B/=nb;
 var d=(B-A).normalized;var nrm=new Vector2(-d.y,d.x);float L=(B-A).magnitude;
 sb.Append("{\"A\":\""+A.ToString("F2")+"\",\"B\":\""+B.ToString("F2")+"\",\"prof\":\"");float prev=float.NaN,worst=0;int jumps=0;string wat="";
 for(float s=0;s<=L;s+=.1f){var c=A+d*s;float o0=float.NaN,o1=float.NaN;for(float o=-2.5f;o<=2.5f;o+=.05f)if(OnPath(c+nrm*o)){if(float.IsNaN(o0))o0=o;o1=o;}if(float.IsNaN(o0))continue;var q=c+nrm*((o0+o1)*.5f);float y=Y(q);
  if(Mathf.Abs(s%1f)<.05f)sb.Append(s.ToString("F0")+":"+y.ToString("F2")+"("+q.x.ToString("F1")+","+q.y.ToString("F1")+") ");
  if(!float.IsNaN(prev)){float j=Mathf.Abs(y-prev);if(j>worst){worst=j;wat=q.ToString("F2");}if(j>.2f)jumps++;}prev=y;}
 sb.Append("\",\"maxStep\":"+worst.ToString("F3")+",\"at\":\""+wat+"\",\"jumps>0.2\":"+jumps+"}");
}finally{UnityEngine.Object.DestroyImmediate(tmp);}
return sb.ToString();
