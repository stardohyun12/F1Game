var sb = new System.Text.StringBuilder();
var st = GameObject.Find("STOP_tea");
sb.AppendLine("STOP_tea=" + (st != null ? st.transform.position.ToString("F2") + " rotY=" + st.transform.eulerAngles.y : "none"));
if (st != null) foreach (Transform c in st.transform) {
  var r = c.GetComponent<Renderer>();
  sb.AppendLine(" child " + c.name + " pos=" + c.position.ToString("F2") + " scl=" + c.lossyScale.ToString("F2") + (r != null ? " b=" + r.bounds.min.ToString("F2") + ".." + r.bounds.max.ToString("F2") + " mat=" + (r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "-") : "") + " col=" + (c.GetComponent<Collider>() != null));
}
var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var so = new UnityEditor.SerializedObject(dir);
var p = so.FindProperty("handover");
sb.AppendLine("dir.handover prop=" + (p != null ? (p.objectReferenceValue ? p.objectReferenceValue.name : "null") : "missing"));
sb.AppendLine("playing=" + Application.isPlaying + " compiling=" + UnityEditor.EditorApplication.isCompiling);
return sb.ToString();
