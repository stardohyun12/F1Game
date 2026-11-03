var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var sb=new System.Text.StringBuilder();
System.Action<Transform,int> dump=null;
dump=(t,d)=>{ if(d>3) return; var r=t.GetComponent<Renderer>(); sb.Append(new string(' ',d*2)+t.name+" lp="+t.localPosition+" act="+t.gameObject.activeSelf+(r!=null?" R":"")+(t.GetComponent<Rigidbody>()!=null?" RB":"")+"\n"); foreach(Transform ch in t) dump(ch,d+1); };
dump(c.transform.Find("A6_KartVisual"),0);
dump(c.transform.Find("body"),0);
var lean=c.GetComponent<CarVisualLean>(); var so=new UnityEditor.SerializedObject(lean); var it=so.GetIterator(); it.NextVisible(true); while(it.NextVisible(false)) if(it.propertyType==UnityEditor.SerializedPropertyType.Float) sb.Append(" lean "+it.name+"="+it.floatValue+"\n");
foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)){ var n=m.GetType().Name; if(n.Contains("Load")||n.Contains("Bed")||n.Contains("Haul")||n.Contains("Carry")||n.Contains("Cart")) sb.Append("mb "+n+" on "+m.name+"\n"); }
return sb.ToString();
