var sb=new System.Text.StringBuilder();
foreach(var h in Physics.OverlapBox(new Vector3(83.4f,1f,120f),new Vector3(2f,1.5f,3f))) sb.Append(h.name+" ("+h.transform.parent?.name+") b="+h.bounds.center+" ext="+h.bounds.extents+" layer="+h.gameObject.layer+"; ");
sb.Append("\nSEA collider? ");
if(Physics.SphereCast(new Vector3(83.4f,1.6f,100f),0.9f,Vector3.forward,out var hh,40f)) sb.Append(hh.collider.name+" dist="+hh.distance+" parent="+hh.collider.transform.parent?.name);
return sb.ToString();
