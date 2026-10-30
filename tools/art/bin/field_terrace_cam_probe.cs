// read-only: which renderer is the black barn?
var sb=new System.Text.StringBuilder();
var ray=new Ray(new Vector3(190,27.69f,-51),(new Vector3(198,25.5f,-57)-new Vector3(190,27.69f,-51)).normalized+new Vector3(-.02f,.12f,.1f));
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){float dd;var b=r.bounds;if(b.size.x>4&&b.size.x<60&&b.IntersectRay(ray,out dd)&&dd<60){sb.AppendLine("R "+r.name+" parent="+(r.transform.parent?r.transform.parent.name:"-")+" d="+dd.ToString("F1")+" size="+b.size+" mats="+string.Join(",",System.Array.ConvertAll(r.sharedMaterials,m=>m!=null?(m.name+"/"+m.shader.name+(m.HasProperty("_Ambient")?" amb="+m.GetColor("_Ambient"):"")+(m.HasProperty("_Tint")?" tint="+m.GetColor("_Tint"):"")):"null")));}}
return sb.ToString();
