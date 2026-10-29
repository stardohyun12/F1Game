// fix 4607 3회차(2259): 덮임 1점 @(268.1,-64.2) 주변 — 가까운 길·경계 샘플·지형 정점
var sb = new System.Text.StringBuilder(); var P = new Vector3(268.1f, 0, -64.2f);
var terrGo = GameObject.Find("TownV6_Gray/TownV6_Terrain"); var mc = terrGo.GetComponent<MeshCollider>();
sb.Append("terr mesh " + terrGo.GetComponent<MeshFilter>().sharedMesh.name + " col " + mc.sharedMesh.name + "\n");
System.Func<float,float,float> g = (x,z) => { RaycastHit h; return mc.Raycast(new Ray(new Vector3(x,500,z),Vector3.down), out h, 1000) ? h.point.y : float.NaN; };
foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { var c = r.centerline; if (c == null) continue;
  float bd = 1e9f; int bi = 0; float bt = 0; for (int i = 1; i < c.Length; i++) { var ab = c[i]-c[i-1]; ab.y=0; var ap = P-c[i-1]; ap.y=0; float t = Vector3.Dot(ap,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude); var q = Vector3.Lerp(c[i-1],c[i],Mathf.Clamp01(t)); q.y=0; float d=(P-q).magnitude; if (d<bd){bd=d;bi=i;bt=t;} }
  if (bd < 12) sb.Append(r.name + " w " + r.width + " dist " + bd.ToString("F2") + " seg " + bi + "/" + (c.Length-1) + " t " + bt.ToString("F2") + " y " + Vector3.Lerp(c[bi-1],c[bi],Mathf.Clamp01(bt)).y.ToString("F2") + " start " + c[0].ToString("F2") + " end " + c[c.Length-1].ToString("F2") + "\n"); }
foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) { if (!mf.name.StartsWith("ROAD") || mf.sharedMesh == null) continue;
  var m = mf.sharedMesh; var v = m.vertices; var tri = m.triangles; var wv = new Vector3[v.Length]; for (int i=0;i<v.Length;i++) wv[i]=mf.transform.TransformPoint(v[i]);
  bool near = false; foreach (var x in wv) if (new Vector2(x.x-P.x,x.z-P.z).magnitude < 6) { near = true; break; } if (!near) continue;
  var cnt = new System.Collections.Generic.Dictionary<long,int>(); var key = new int[v.Length]; var map = new System.Collections.Generic.Dictionary<long,int>();
  for (int i=0;i<v.Length;i++){ long k=((long)Mathf.RoundToInt(wv[i].x*100)*73856093L)^((long)Mathf.RoundToInt(wv[i].y*100)*19349663L)^((long)Mathf.RoundToInt(wv[i].z*100)*83492791L); int id; if(!map.TryGetValue(k,out id)){id=i;map[k]=i;} key[i]=id; }
  for (int t3=0;t3<tri.Length;t3+=3) for(int e=0;e<3;e++){ int a=key[tri[t3+e]], b=key[tri[t3+(e+1)%3]]; long ek=a<b?((long)a<<32)|(uint)b:((long)b<<32)|(uint)a; int c; cnt.TryGetValue(ek,out c); cnt[ek]=c+1; }
  foreach (var kv in cnt) { if (kv.Value!=1) continue; var A=wv[(int)(kv.Key>>32)]; var B=wv[(int)(kv.Key&0xffffffffL)]; int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(A,B)));
    for (int s=0;s<steps;s++){ var q=Vector3.Lerp(A,B,(s+.5f)/steps); if (new Vector2(q.x-P.x,q.z-P.z).magnitude>4) continue; sb.Append("  " + mf.name + " edge " + q.ToString("F2") + " d " + (q.y-g(q.x,q.z)).ToString("F3") + "\n"); } } }
var tm = mc.sharedMesh; var tv = tm.vertices; for (int i=0;i<tv.Length;i++){ var w=terrGo.transform.TransformPoint(tv[i]); if (Mathf.Abs(w.x-P.x)<3 && Mathf.Abs(w.z-P.z)<3) sb.Append("  tv " + w.ToString("F2") + "\n"); }
return sb.ToString();
