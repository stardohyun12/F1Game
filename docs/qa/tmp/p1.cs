var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>();
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-qa-1-board.png");
typeof(OrderBoard).GetMethod("OnCardClicked",bf).Invoke(ob,new object[]{0});
typeof(OrderBoard).GetMethod("OnBoardGoClicked",bf).Invoke(ob,null);
var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
var item=ps.ActiveItem; item.transform.rotation=Quaternion.Euler(0,180,0);
var mats=ps.GetType().GetMethod("GetMaterials",bf).Invoke(ps,null) as PitCrew.Packing.WrapMaterialDef[];
ps.SelectMaterial(mats[2]);
var painter=item.GetComponent<PitCrew.Packing.WrapPainter>(); var ws=item.GetComponent<PitCrew.Packing.WrapState>();
var cam=ps.GetType().GetField("packingCamera",bf).GetValue(ps) as Camera; var col=painter.GetType().GetField("targetCollider",bf).GetValue(painter) as Collider;
Physics.SyncTransforms(); var b=col.bounds; var c=b.center;
Vector2 top=cam.WorldToScreenPoint(c+new Vector3(0,b.extents.y*0.8f,0)); Vector2 bot=cam.WorldToScreenPoint(c-new Vector3(0,b.extents.y*0.8f,0));
painter.BeginStroke(top); for(int i=1;i<=16;i++) painter.UpdateStroke(Vector2.Lerp(top,bot,i/16f)); painter.EndStroke();
string s="items="+(ps.GetType().GetField("spawnedItems",bf).GetValue(ps) as System.Collections.ICollection).Count+" "+item.name+"\n"; var dirs=new Vector3[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}; foreach(var d in dirs){int ch; float a=ws.QuerySideAbsorption(d,out ch); s+=d+" abs="+a+" ch="+ch+"\n";}
var g=ps.GetType().GetMethod("ComputeGrade",bf); object[] args=new object[]{0f}; s+="grade="+g.Invoke(ps,args)+" score="+args[0];
return s;
