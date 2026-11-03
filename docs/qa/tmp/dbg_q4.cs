var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var f=d.Player.transform.position; float yaw=140f;
var fw=Quaternion.Euler(0,yaw,0)*Vector3.forward; var rt=Quaternion.Euler(0,yaw,0)*Vector3.right;
var half=new Vector3(1.0f,0.5f,2.2f); string o="";
Vector3[] offs={rt*3.5f,-rt*3.5f,-fw*5f,fw*5f};
foreach(var off in offs){ var p=f+off; RaycastHit h;
 bool r=Physics.Raycast(p+Vector3.up*4f,Vector3.down,out h,10f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
 var cols=r?Physics.OverlapBox(h.point+Vector3.up*1.2f,half,Quaternion.Euler(0,yaw,0),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore):new Collider[0];
 string cn=""; foreach(var c in cols) cn+=c.name+",";
 o+=off.ToString("F1")+" hit="+r+(r?" dy="+(h.point.y-f.y).ToString("F2")+" "+h.collider.name:"")+" overlap="+cn+"\n"; }
return o;
