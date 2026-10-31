var a=new Vector3(76.7f,12.6f,-59.6f); var b=new Vector3(65.6f,12.6f,-62.2f); string s="";
for(int k=0;k<=11;k++){ var p=Vector3.Lerp(a,b,k/11f);
  if(Physics.Raycast(p+Vector3.up*3f,Vector3.down,out var h,10f)) s+=k+": ("+p.x.ToString("F1")+","+p.z.ToString("F1")+") y="+h.point.y.ToString("F2")+" "+h.collider.name+" par="+(h.collider.transform.parent!=null?h.collider.transform.parent.name:"-")+"\n"; else s+=k+": none\n"; }
foreach(var c in Physics.OverlapCapsule(new Vector3(69f,13.0f,-63.5f),new Vector3(69f,14.0f,-63.5f),1.0f)) s+="overlap "+c.name+" par="+(c.transform.parent!=null?c.transform.parent.name:"-")+"\n";
return s;
