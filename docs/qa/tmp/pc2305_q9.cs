var sb=new System.Text.StringBuilder();
var c0=new Vector3(272.5f,31f,-33.6f);
var cols=Physics.OverlapBox(c0,new Vector3(4f,3f,4f));
foreach(var c in cols){ var b=c.bounds; string path=c.name; var t=c.transform.parent; int k=0; while(t!=null&&k<3){ path=t.name+"/"+path; t=t.parent; k++; }
 sb.AppendLine(path+" "+c.GetType().Name+" trig="+c.isTrigger+" c="+b.center.ToString("F2")+" s="+b.size.ToString("F2")); }
return sb.ToString();
