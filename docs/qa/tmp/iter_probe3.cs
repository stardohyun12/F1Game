// artlive 1509: 보관소 fbx 회전 확인(읽기만)
var log = new System.Text.StringBuilder();
var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PreviewCand/depot/q3_depot.fbx");
log.Append("asset root rot " + go.transform.localEulerAngles + " sc " + go.transform.localScale + "\n");
foreach (Transform t in go.transform) log.Append("  kid " + t.name + " rot " + t.localEulerAngles.ToString("F0") + " pos " + t.localPosition.ToString("F2") + " sc " + t.localScale.ToString("F2") + "\n");
var dp = GameObject.Find("ITER_Depot").transform.GetChild(0); log.Append("inst rot " + dp.localEulerAngles + "\n");
foreach (Transform t in dp) { var r = t.GetComponent<Renderer>(); log.Append("  " + t.name + " b " + (r ? r.bounds.size.ToString("F2") : "-") + "\n"); }
return log.ToString();
