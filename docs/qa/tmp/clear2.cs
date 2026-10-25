Physics.SyncTransforms(); var race=GameObject.Find("Race"); var o=race.transform.position; var s="";
foreach(var a in new int[]{0,90,180,270}){ s+=" ["+a+"] "; for(int d=0;d<=60;d+=10){ var p=o+Quaternion.Euler(0,a,0)*Vector3.forward*d+Vector3.up*20f; RaycastHit h; if(Physics.Raycast(p,Vector3.down,out h,40f,~0,QueryTriggerInteraction.Ignore)) s+=d+":"+h.collider.name+"@"+h.point.y.ToString("F1")+" "; } }
var mf=race.GetComponent<Rigidbody>(); return s+" fwd="+race.transform.forward;
