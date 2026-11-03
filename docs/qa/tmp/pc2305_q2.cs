var sb=new System.Text.StringBuilder();
foreach(var xz in new Vector2[]{new Vector2(206,-168),new Vector2(206,-166),new Vector2(204,-168),new Vector2(208,-168)}){
 var hits=Physics.RaycastAll(new Vector3(xz.x,200,xz.y),Vector3.down,400f);
 foreach(var h in hits){ var r=h.collider.GetComponent<Renderer>(); sb.AppendLine(xz+" hit "+h.collider.name+" y="+h.point.y.ToString("F2")+" mat="+(r!=null&&r.sharedMaterial!=null?r.sharedMaterial.name:"-")+" parent="+(h.collider.transform.parent!=null?h.collider.transform.parent.name:"-")); }
}
// renderers whose bounds contain the point (206,*,-168)
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 var b=r.bounds; if(b.min.x<=206&&b.max.x>=206&&b.min.z<=-168&&b.max.z>=-168&&b.size.x<400){ sb.AppendLine("R "+r.name+" par="+(r.transform.parent!=null?r.transform.parent.name:"-")+" mat="+(r.sharedMaterial!=null?r.sharedMaterial.name:"-")+" sh="+(r.sharedMaterial!=null?r.sharedMaterial.shader.name:"-")+" c="+b.center.ToString("F1")+" s="+b.size.ToString("F1")+" act="+r.gameObject.activeInHierarchy); }
}
return sb.ToString();
