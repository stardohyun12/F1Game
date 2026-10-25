Physics.SyncTransforms(); var race=GameObject.Find("Race"); var o=race.transform.position; var s="race="+o+" yaw="+race.transform.eulerAngles.y+" | ";
for(int a=0;a<360;a+=30){ var d=Quaternion.Euler(0,a,0)*Vector3.forward; RaycastHit h; var origin=o+Vector3.up*1f; if(Physics.SphereCast(origin,1.2f,d,out h,120f,~0,QueryTriggerInteraction.Ignore)) s+=a+":"+h.distance.ToString("F0")+"("+h.collider.name+") "; else s+=a+":>120 "; }
return s;
