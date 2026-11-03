var log = new System.Text.StringBuilder();
log.AppendLine("project=" + Application.dataPath);
log.AppendLine("playing=" + EditorApplication.isPlaying + " compiling=" + EditorApplication.isCompiling);
log.AppendLine("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Library/Buildings/depot/q3_depot.fbx");
int n = 0; float err = 0; int maxParcel = 0;
foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true)) {
  if (mf.name.StartsWith("Q3_decals_") || mf.name.StartsWith("Q3_sign_")) {
    var b = new Bounds(); bool first = true;
    foreach (var v in mf.sharedMesh.vertices) { var w = mf.transform.TransformPoint(v); if (first) { b = new Bounds(w, Vector3.zero); first = false; } else b.Encapsulate(w); }
    var p = new Vector3(b.center.x,b.min.y,b.center.z); err = Mathf.Max(err,(mf.transform.position-p).magnitude); n++; log.AppendLine(mf.name);
  }
  if (mf.name.StartsWith("PARCEL_")) maxParcel = Mathf.Max(maxParcel,mf.sharedMesh.triangles.Length/3);
}
log.AppendLine("split_parts="+n+" pivot_max_m="+err+" parcel_tris_max="+maxParcel);
if (n!=10 || err>.0001f || maxParcel>2000) throw new System.Exception("depot import validation failed");
return log.ToString();
