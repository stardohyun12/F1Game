// terrace3: 돌벽 메시를 0.5 m 칸으로 묶어 칸별 높이(최고-최저)가 큰 곳 5개
var go=GameObject.Find("TownV6_Gray/TerraceBlock/RetainingWalls");var m=go.GetComponent<MeshFilter>().sharedMesh;var vs=m.vertices;
var lo=new System.Collections.Generic.Dictionary<long,float>();var hi=new System.Collections.Generic.Dictionary<long,float>();
foreach(var v0 in vs){var v=go.transform.TransformPoint(v0);long k=((long)Mathf.FloorToInt(v.x*2))*100000+Mathf.FloorToInt(v.z*2);if(!lo.ContainsKey(k)){lo[k]=v.y;hi[k]=v.y;}else{lo[k]=Mathf.Min(lo[k],v.y);hi[k]=Mathf.Max(hi[k],v.y);}}
var top=new System.Collections.Generic.List<(float h,long k)>();foreach(var k in lo.Keys)top.Add((hi[k]-lo[k],k));top.Sort((a,b)=>b.h.CompareTo(a.h));
var sb=new System.Text.StringBuilder();for(int i=0;i<Mathf.Min(12,top.Count);i++){var k=top[i].k;long kx=k/100000;long kz=k%100000;if(kz>50000){kz-=100000;kx+=1;}sb.Append("("+(kx*.5f).ToString("F1")+","+(kz*.5f).ToString("F1")+") h="+top[i].h.ToString("F2")+" lo="+lo[k].ToString("F2")+" | ");}
return sb.ToString();
