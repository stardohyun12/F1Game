var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var item=ps.ActiveItem; var painter=item.GetComponent<PitCrew.Packing.WrapPainter>(); var ws=item.GetComponent<PitCrew.Packing.WrapState>();
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera; var col=painter.GetType().GetField("targetCollider",bf).GetValue(painter) as Collider;
var mats=Resources.LoadAll<PitCrew.Packing.WrapMaterialDef>("WrapMaterials");
Physics.SyncTransforms(); var b=col.bounds; var c=b.center; var front=c-cam.transform.forward*b.extents.z;
var sb=new System.Text.StringBuilder(); float cap=ws.CostCap; ws.CostCap=float.PositiveInfinity;
sb.Append("item="+item.name+" CostScale="+ws.CostScale.ToString("F5")+" cap="+cap+" ; ");
float cov0=ws.ComputeCoverage();
foreach(var m in mats){ if(m.channelIndex==1) continue;
 for(int k=0;k<3;k++) ws.CostByChannel[k]=0f;
 painter.SetMaterial(m);
 // 화면 기준 세로 띠 6줄(0.2m 간격, 리본 폭 0.24m — 살짝 겹치게 = 사람이 칠하는 식)
 for(int s=0;s<6;s++){ float x=-0.5f+0.1f+0.2f*s; if(x>0.5f) x=0.5f;
  Vector3 wt=front+cam.transform.right*x*b.size.x+cam.transform.up*b.extents.y*0.98f; Vector3 wb=front+cam.transform.right*x*b.size.x-cam.transform.up*b.extents.y*0.98f;
  Vector2 top=cam.WorldToScreenPoint(wt), bot=cam.WorldToScreenPoint(wb), mid=(top+bot)*0.5f;
  // 가운데(물체 위)에서 시작해야 획이 잡힌다 → 위로 반, 아래로 반
  painter.BeginStroke(mid); for(int i=1;i<=10;i++) painter.UpdateStroke(Vector2.Lerp(mid,top,i/10f)); painter.EndStroke();
  painter.BeginStroke(mid); for(int i=1;i<=10;i++) painter.UpdateStroke(Vector2.Lerp(mid,bot,i/10f)); painter.EndStroke(); }
 float face=ws.TotalCost; float cov=ws.ComputeCoverage();
 sb.Append(m.name+"(cpp="+m.costPerPixel+"): 1face="+face.ToString("F0")+" 6face="+(face*6).ToString("F0")+" coverageDelta="+((cov-cov0)*6).ToString("F2")+"face ; ");
}
ws.CostCap=cap; for(int k=0;k<3;k++) ws.CostByChannel[k]=0f;
return sb.ToString();
