// 남색 판 9: 렌더러·스카이박스 다 꺼도 남음 → UI Toolkit(UIDocument, 월드 패널) 후보 목록
var sb=new System.Text.StringBuilder();
string Path(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
foreach(var d in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 var ps=d.panelSettings;string mode="-";
 if(ps){var p=ps.GetType().GetProperty("renderMode");mode=p!=null?p.GetValue(ps).ToString():"(no renderMode)";}
 sb.AppendLine("UIDocument "+Path(d.transform)+" active="+d.gameObject.activeInHierarchy+" en="+d.enabled+" layer="+d.gameObject.layer+" pos="+d.transform.position.ToString("F1")+" rot="+d.transform.eulerAngles.ToString("F0")+" scale="+d.transform.lossyScale.ToString("F3")+" panel="+(ps?ps.name+"@"+UnityEditor.AssetDatabase.GetAssetPath(ps):"null")+" renderMode="+mode+" uxml="+(d.visualTreeAsset?UnityEditor.AssetDatabase.GetAssetPath(d.visualTreeAsset):"null"));
 foreach(var c in d.GetComponents<MonoBehaviour>())sb.AppendLine("   comp "+c.GetType().FullName);
}
return sb.ToString();
