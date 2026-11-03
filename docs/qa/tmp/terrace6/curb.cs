// terrace6: 큰길 북쪽 보도 x 190..220 — 보도 가운데 높이 − 바로 앞 차길 가장자리 높이(= 연석 높이) 범위, 보도 1 m 경사 최대
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways").GetComponent<Collider>();
var roads=new System.Collections.Generic.List<Collider>();foreach(Transform ch in GameObject.Find("TownV6_Gray/RoadsV2").transform)if(ch.gameObject.activeInHierarchy&&ch.name.StartsWith("ROAD_V2e")){var c=ch.GetComponent<Collider>();if(c)roads.Add(c);}
float cmin=9,cmax=-9,gmax=0;string gAt="";float prev=float.NaN;var sb=new System.Text.StringBuilder();
for(float x=190;x<=220.01f;x+=1f){float zr=float.NaN,yr=0;for(float z=-70;z>=-85;z-=.05f){foreach(var c in roads){RaycastHit h;if(c.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)){zr=z;yr=h.point.y;break;}}if(!float.IsNaN(zr))break;}
 RaycastHit hw;float yw=wk.Raycast(new Ray(new Vector3(x,500,zr+1.1f),Vector3.down),out hw,1000)?hw.point.y:float.NaN;float curb=yw-yr;
 if(x<196||x>198){cmin=Mathf.Min(cmin,curb);cmax=Mathf.Max(cmax,curb);if(!float.IsNaN(prev)&&x>199){float g=Mathf.Abs(yw-prev);if(g>gmax){gmax=g;gAt=x.ToString("F0");}}}
 prev=yw;sb.Append($"x{x:F0} roadEdge z{zr:F2} y{yr:F2} walk y{yw:F2} curb {curb:F3}\n");}
sb.Append($"curbMin {cmin:F3} curbMax {cmax:F3} walkGrade1mMax(x>=200) {gmax:F3} @x{gAt}");return sb.ToString();
