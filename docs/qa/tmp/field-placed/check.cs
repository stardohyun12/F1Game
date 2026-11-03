// 밭 투입 검사: 인스턴스 여부·개수, 자식별 바닥 대비 밑면(뜸/묻힘), 입구 문기둥 ↔ 흙길 끝, 흙길이 울·돌과 겹치는지, A7ShadeGlobals
var sb=new System.Text.StringBuilder();
var root=GameObject.Find("TownV6_Gray/TerraceBlock");int n=0;GameObject f=null;foreach(Transform c in root.transform)if(c.name=="FieldPatch"){n++;f=c.gameObject;}
sb.AppendLine("FieldPatch count="+n+" instanceRoot="+PrefabUtility.IsOutermostPrefabInstanceRoot(f)+" src="+AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(f))+" overrides="+PrefabUtility.GetObjectOverrides(f).Count+" added="+PrefabUtility.GetAddedGameObjects(f).Count);
sb.AppendLine("pos="+f.transform.position.ToString("F3")+" yaw="+f.transform.eulerAngles.y.ToString("F1"));
var terr=GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
float G(float x,float z){RaycastHit r;return terr.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out r,1000)?r.point.y:float.NaN;}
float worstFloat=-9,worstSink=9;string wf="",ws="";
foreach(var r in f.GetComponentsInChildren<Renderer>()){var b=r.bounds;float g=G(b.center.x,b.center.z);float d=b.min.y-g;
  if(d>worstFloat){worstFloat=d;wf=r.name;}if(d<worstSink){worstSink=d;ws=r.name;}
  if(r.name.StartsWith("FENCE")||r.name.StartsWith("GATE")||r.name.StartsWith("SOIL")||r.name.StartsWith("RIDGE")||r.name.StartsWith("BASKET")||r.name.StartsWith("CAN"))sb.AppendLine($"  {r.name} minY-ground={d:F3} topY-ground={b.max.y-g:F3}");}
sb.AppendLine($"most raised bottom: {wf} {worstFloat:F3} / most sunk: {ws} {worstSink:F3}");
// 울 아래 끝(밑면) 이 땅과 만나는지: FENCE 메시 정점 중 가장 낮은 것들 vs 땅
foreach(var nm in new[]{"FENCE_01","FENCE_02","FENCE_03","GATEPOST_01","GATEPOST_02"}){var t=f.transform.Find(nm);if(!t)t=f.transform.GetChild(0).Find(nm);var mf=t.GetComponent<MeshFilter>();float lo=1e9f,dmax=-9,dmin=9;foreach(var v in mf.sharedMesh.vertices){var w=t.TransformPoint(v);lo=Mathf.Min(lo,w.y);}
  // 밑면 정점(가장 낮은 것 +2cm 안) 의 땅 대비 차
  foreach(var v in mf.sharedMesh.vertices){var w=t.TransformPoint(v);if(w.y<lo+.02f){float d=w.y-G(w.x,w.z);dmax=Mathf.Max(dmax,d);dmin=Mathf.Min(dmin,d);}}
  sb.AppendLine($"  {nm} bottom verts vs ground [{dmin:F3},{dmax:F3}] world={t.position.ToString("F2")}");}
// 흙길 끝 ↔ 입구
var pg=root.transform.Find("DirtPath_door_garden");var pm=pg.GetComponent<MeshFilter>().sharedMesh;var pv=pm.vertices;
var g1=f.transform.GetChild(0).Find("GATEPOST_01");var g2=f.transform.GetChild(0).Find("GATEPOST_02");if(!g1){g1=f.transform.Find("GATEPOST_01");g2=f.transform.Find("GATEPOST_02");}
var fi=f.transform;var a=fi.InverseTransformPoint(g1.position);var b2=fi.InverseTransformPoint(g2.position);
sb.AppendLine($"gateposts field-local x {b2.x:F3}..{a.x:F3} z {a.z:F3}");
// 흙길 정점을 밭 로컬로: 문기둥 줄(z≈2.59) ±0.1 에 걸친 정점의 x 범위, 밭 안(z<2.5) 으로 들어간 정점 수, 울(|x|>2.6 또는 z<-2.6) 안쪽과 겹침
float xl=9,xr=-9,zmin=9;int inside=0,nearPost=0;float ymax=-9;
foreach(var v in pv){var l=fi.InverseTransformPoint(pg.TransformPoint(v));if(Mathf.Abs(l.z-2.59f)<.12f){xl=Mathf.Min(xl,l.x);xr=Mathf.Max(xr,l.x);}zmin=Mathf.Min(zmin,l.z);if(l.z<2.5f&&Mathf.Abs(l.x)<3.1f)inside++;
  foreach(var gp in new[]{a,b2})if(new Vector2(l.x-gp.x,l.z-gp.z).magnitude<.06f)nearPost++;}
sb.AppendLine($"path at gate line x {xl:F3}..{xr:F3} (center {(xl+xr)/2:F3}, opening center {(a.x+b2.x)/2:F3}), path min z(field-local)={zmin:F3}, verts inside field={inside}, verts within 6cm of a post={nearPost}");
// 흙길 메시 삼각형이 울·돌 콜라이더와 겹치는지 (흙길 정점 위 0.05~0.6 m 레이로 콜라이더 닿음)
int blocked=0;var hitNames=new System.Collections.Generic.HashSet<string>();
foreach(var v in pv){var w=pg.TransformPoint(v);foreach(var h in Physics.RaycastAll(new Ray(w+Vector3.up*.8f,Vector3.down),.78f)){if(h.collider.transform.IsChildOf(fi)){blocked++;hitNames.Add(h.collider.name);}}}
sb.AppendLine("path verts under field colliders="+blocked+" "+string.Join(",",hitNames));
int a7=0;foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(m&&m.GetType().Name=="A7ShadeGlobals"&&m.isActiveAndEnabled)a7++;
sb.AppendLine("A7ShadeGlobals active="+a7+" dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
return sb.ToString();
