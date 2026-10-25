Physics.SyncTransforms();
var sb=new System.Text.StringBuilder(); int empty=0,tot=0; float step=4f; float x0=-80f,x1=260f,z0=-80f,z1=270f; var holes=new System.Text.StringBuilder();
for(float z=z1; z>=z0; z-=step){ for(float x=x0; x<=x1; x+=step){ tot++; var o=new Vector3(x,5f,z); bool hit=Physics.Raycast(o,Vector3.down,out var h,50f,~0,QueryTriggerInteraction.Ignore); if(hit){ sb.Append(h.point.y>2.5f?'B':'.'); } else {empty++; sb.Append('X'); if(empty<=40) holes.Append("("+x+","+z+") ");} } sb.Append('\n'); }
return "tot="+tot+" empty="+empty+" region x["+x0+","+x1+"] z["+z0+","+z1+"] step="+step+"\nholes: "+holes+"\n"+(empty>0?sb.ToString():"");
