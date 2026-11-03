var d=new System.Collections.Generic.Dictionary<string,int>(); var mats=new System.Collections.Generic.Dictionary<string,int>();
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){ foreach(var m in r.sharedMaterials){ if(m==null) continue; var k=m.shader.name; d[k]=d.ContainsKey(k)?d[k]+1:1; var mk=m.name+" | "+k; mats[mk]=mats.ContainsKey(mk)?mats[mk]+1:1; } }
var sb=new System.Text.StringBuilder(); foreach(var kv in d) sb.AppendLine(kv.Value+"  "+kv.Key);
sb.AppendLine("---- mats ("+mats.Count+")"); int i=0; foreach(var kv in mats){ if(i++<60) sb.AppendLine(kv.Value+"  "+kv.Key);}
// 자리 근처 렌더러
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){ var c=r.bounds.center; if(Vector2.Distance(new Vector2(c.x,c.z), new Vector2(-8,-292))<12f && r.bounds.size.magnitude<200) sb.AppendLine("near "+r.name+" "+c.ToString("F1")+" sz="+r.bounds.size.ToString("F1")+" "+r.sharedMaterial.name+"|"+r.sharedMaterial.shader.name); }
return sb.ToString();
