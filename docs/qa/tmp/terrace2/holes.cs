// terrace2: 지형 메시에서 삼각형 하나만 쓰는 모서리(위치 기준 mm) = 구멍/이음. v2 원본과 사본(terrace1)을 비교해 새로 생긴 것만 센다. 반경 68 m 안.
var c=new Vector2(185,-61);
System.Func<string,System.Collections.Generic.List<Vector3>> Open=path=>{
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);var v=m.vertices;var t=m.triangles;
 System.Func<int,string> K=k=>Mathf.RoundToInt(v[k].x*1000)+","+Mathf.RoundToInt(v[k].y*1000)+","+Mathf.RoundToInt(v[k].z*1000);
 var cnt=new System.Collections.Generic.Dictionary<string,int>();var pos=new System.Collections.Generic.Dictionary<string,Vector3>();
 for(int i=0;i<t.Length;i+=3){var cen=(v[t[i]]+v[t[i+1]]+v[t[i+2]])/3f;if(Vector2.Distance(new Vector2(cen.x,cen.z),c)>70)continue;
  for(int e=0;e<3;e++){int a=t[i+e],b=t[i+(e+1)%3];var ka=K(a);var kb=K(b);string s=string.CompareOrdinal(ka,kb)<0?ka+"|"+kb:kb+"|"+ka;int n;cnt.TryGetValue(s,out n);cnt[s]=n+1;pos[s]=(v[a]+v[b])*.5f;}}
 var r=new System.Collections.Generic.List<Vector3>();foreach(var kv in cnt)if(kv.Value==1){var p=pos[kv.Key];if(Vector2.Distance(new Vector2(p.x,p.z),c)<68)r.Add(p);}return r;};
var A=Open("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_v2.asset");var B=Open("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_terrace1.asset");
var cells=new System.Collections.Generic.HashSet<long>();
foreach(var p in A){int ci=Mathf.FloorToInt(p.x/.5f),cj=Mathf.FloorToInt(p.z/.5f);for(int dj=-2;dj<=2;dj++)for(int di=-2;di<=2;di++)cells.Add(((long)(ci+di)<<32)^(uint)(cj+dj));}
int nw=0;var sb=new System.Text.StringBuilder();
foreach(var p in B){int ci=Mathf.FloorToInt(p.x/.5f),cj=Mathf.FloorToInt(p.z/.5f);if(cells.Contains(((long)ci<<32)^(uint)cj))continue;nw++;if(nw<=15)sb.Append("("+p.x.ToString("F1")+","+p.y.ToString("F1")+","+p.z.ToString("F1")+") ");}
return "openV2="+A.Count+" openNow="+B.Count+" newOpen="+nw+" sample: "+sb;
