// 집 방향·바닥 사각형·문 위치 + 3차 터 높이(마당 vs 밭) — 텃밭 자리 정하기용
var sb=new System.Text.StringBuilder();
var h=GameObject.Find("D198_Buildings/house_home").transform;
sb.AppendLine("house pos="+h.position.ToString("F2")+" yaw="+h.eulerAngles.y.ToString("F1")+" fwd="+h.forward.ToString("F2"));
var inv=Quaternion.Inverse(Quaternion.Euler(0,h.eulerAngles.y,0));
float minY=1e9f;var pts=new System.Collections.Generic.List<Vector3>();
foreach(var mf in h.GetComponentsInChildren<MeshFilter>()){var mr=mf.GetComponent<MeshRenderer>();if(!mf.sharedMesh||!mr||!mr.enabled)continue;foreach(var v in mf.sharedMesh.vertices){var w=mf.transform.TransformPoint(v);pts.Add(w);minY=Mathf.Min(minY,w.y);}}
float x0=1e9f,x1=-1e9f,z0=1e9f,z1=-1e9f;foreach(var w in pts){if(w.y>minY+.5f)continue;var l=inv*(w-h.position);x0=Mathf.Min(x0,l.x);x1=Mathf.Max(x1,l.x);z0=Mathf.Min(z0,l.z);z1=Mathf.Max(z1,l.z);}
sb.AppendLine("foot local x "+x0.ToString("F2")+".."+x1.ToString("F2")+" z "+z0.ToString("F2")+".."+z1.ToString("F2")+" minY="+minY.ToString("F2"));
foreach(var t in h.GetComponentsInChildren<Transform>()){var n=t.name.ToLower();if(n.Contains("door")||n.Contains("step")||n.Contains("gate")||n.Contains("porch")||n.Contains("stair")){var r=t.GetComponent<Renderer>();var c=r?r.bounds.center:t.position;var l=inv*(c-h.position);sb.AppendLine("  "+t.name+" world="+c.ToString("F2")+" local="+l.ToString("F2")+(r?" size="+r.bounds.size.ToString("F2"):""));}}
// 땅 높이: 집 둘레 동쪽(local +x), 북쪽 등 격자 2 m
var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
var nat=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_v2.asset");var tg=new GameObject("_h"){hideFlags=HideFlags.HideAndDontSave};var mc=tg.AddComponent<MeshCollider>();mc.sharedMesh=nat;
sb.AppendLine("v2 terrain height (local x across, local z rows), house floor "+minY.ToString("F2"));
for(float lz=z1+8;lz>=z0-6;lz-=2){var line=new System.Text.StringBuilder("z"+lz.ToString("F0").PadLeft(4)+": ");for(float lx=x0-6;lx<=x1+16;lx+=2){var w=h.position+Quaternion.Euler(0,h.eulerAngles.y,0)*new Vector3(lx,0,lz);RaycastHit hit;line.Append(mc.Raycast(new Ray(new Vector3(w.x,500,w.z),Vector3.down),out hit,1000)?(hit.point.y-minY).ToString("+0.0;-0.0").PadLeft(6):"   nan");}sb.AppendLine(line.ToString());}
UnityEngine.Object.DestroyImmediate(tg);
return sb.ToString();
