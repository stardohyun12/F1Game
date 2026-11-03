// f1_high/c1/c2 하늘의 남색 판 찾기: 땅(약 20~30 m)보다 높이 떠 있거나 매우 큰 렌더러
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){
 var b=r.bounds;if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
 if(b.min.y<35&&b.size.y<40)continue;
 float d=Vector2.Distance(new Vector2(b.center.x,b.center.z),new Vector2(185,-61));if(d>400)continue;
 var m=r.sharedMaterial;string col="-";
 if(m){foreach(var pn in new[]{"_Tint","_BaseColor","_Color"})if(m.HasProperty(pn)){col=pn+m.GetColor(pn).ToString("F2");break;}}
 sb.AppendLine(Path(r.transform)+" | min="+b.min.ToString("F0")+" max="+b.max.ToString("F0")+" d="+d.ToString("F0")+" mat="+(m?m.name:"null")+" "+col+" type="+r.GetType().Name);
}
return sb.ToString();
