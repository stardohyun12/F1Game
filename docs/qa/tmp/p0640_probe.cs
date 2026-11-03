// 과제 0640(fb 4607 3회차) 읽기 전용: 갈림목 (285.5,-65) 반경 6 m 의 길 가장자리 중 |길-지형|>0.15 인 점과, 그 점에서 각 길 중심선 e·높이
var sb = new System.Text.StringBuilder(); var inv = System.Globalization.CultureInfo.InvariantCulture; System.Func<float,string> F = x => x.ToString("F2", inv);
var P = new Vector3(273.405f, 23.8f, -69.821f); float RAD = 25f;   // 2차: 코멘트 반경 전체, 덮임(d<-0.02)만
var terr = GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
System.Func<float,float,float> ground = (x,z) => { RaycastHit h; return terr.Raycast(new Ray(new Vector3(x,500,z),Vector3.down), out h, 1000) ? h.point.y : float.NaN; };
var roads = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None);
sb.Append("terrain mesh " + terr.sharedMesh.name + " / mf " + terr.GetComponent<MeshFilter>().sharedMesh.name + "\n");
foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
  if (!mf.name.StartsWith("ROAD") || mf.sharedMesh == null || !mf.gameObject.activeInHierarchy) continue;
  var m = mf.sharedMesh; var v = m.vertices; var tri = m.triangles; var wv = new Vector3[v.Length]; for (int i=0;i<v.Length;i++) wv[i]=mf.transform.TransformPoint(v[i]);
  var key = new int[v.Length]; var map = new System.Collections.Generic.Dictionary<long,int>();
  for (int i=0;i<v.Length;i++){ long k=((long)Mathf.RoundToInt(wv[i].x*100f)*73856093L)^((long)Mathf.RoundToInt(wv[i].y*100f)*19349663L)^((long)Mathf.RoundToInt(wv[i].z*100f)*83492791L); int id; if(!map.TryGetValue(k,out id)){id=i;map[k]=i;} key[i]=id; }
  var cnt = new System.Collections.Generic.Dictionary<long,int>();
  for (int t3=0;t3<tri.Length;t3+=3) for(int e=0;e<3;e++){int a=key[tri[t3+e]],b=key[tri[t3+(e+1)%3]]; long ek=a<b?((long)a<<32)|(uint)b:((long)b<<32)|(uint)a; int c; cnt.TryGetValue(ek,out c); cnt[ek]=c+1;}
  foreach (var kv in cnt) { if (kv.Value!=1) continue; var A=wv[(int)(kv.Key>>32)]; var B=wv[(int)(kv.Key&0xffffffffL)]; int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(A,B)));
    for (int s=0;s<steps;s++){ var q=Vector3.Lerp(A,B,(s+.5f)/steps); if (new Vector2(q.x-P.x,q.z-P.z).magnitude>RAD) continue; float g=ground(q.x,q.z); float d=q.y-g; if (d>=-.02f && Mathf.Abs(d)<=.15f) continue;
      sb.Append(mf.name+" @("+F(q.x)+","+F(q.z)+") road "+F(q.y)+" terr "+F(g)+" d "+F(d)+" | ");
      foreach (var r in roads){ var c=r.centerline; if(c==null) continue; float bd=1e9f, by=0; for(int i=1;i<c.Length;i++){ var ab=c[i]-c[i-1]; ab.y=0; var ap=q-c[i-1]; ap.y=0; float t=Mathf.Clamp01(Vector3.Dot(ap,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude)); var qq=Vector3.Lerp(c[i-1],c[i],t); var dd=q-qq; dd.y=0; if(dd.magnitude<bd){bd=dd.magnitude;by=qq.y;} } float e=bd-r.width*.5f; if(e<3f) sb.Append(r.name+" e"+F(e)+" y"+F(by)+"; "); }
      sb.Append("\n"); } } }
return sb.ToString();
