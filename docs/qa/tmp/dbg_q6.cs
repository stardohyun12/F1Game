var c=new Vector3(93.8f,4f,-288f); string o="";
foreach(var off in new Vector3[]{new Vector3(-5,0,0),new Vector3(5,0,0),new Vector3(0,0,5),new Vector3(0,0,-5),new Vector3(-8,0,0),new Vector3(8,0,0),new Vector3(0,0,8),new Vector3(0,0,-8)}){
 var p=c+off; RaycastHit h; bool r=Physics.Raycast(p+Vector3.up*3f,Vector3.down,out h,8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
 o+=off+" "+(r?h.collider.name+" y="+h.point.y.ToString("F2"):"-")+"\n"; }
return o;
