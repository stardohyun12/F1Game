var sb = new System.Text.StringBuilder();
var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_TmpEnv/env_camo_204695159_v02.fbx");
var ins = UnityEngine.Object.Instantiate(go); ins.hideFlags = HideFlags.DontSave;
var b = new Bounds(); bool first = true;
foreach (var mf in ins.GetComponentsInChildren<MeshFilter>(true)) {
  var mb = new Bounds(); bool f2 = true;
  foreach (var p0 in mf.sharedMesh.vertices) { var p = mf.transform.TransformPoint(p0); if (f2) { mb = new Bounds(p, Vector3.zero); f2 = false; } else mb.Encapsulate(p); }
  if (first) { b = mb; first = false; } else b.Encapsulate(mb);
  sb.AppendLine(mf.name + " tris=" + mf.sharedMesh.triangles.Length / 3 + " min=" + mb.min.ToString("F2") + " max=" + mb.max.ToString("F2"));
}
sb.AppendLine("ALL min=" + b.min.ToString("F2") + " max=" + b.max.ToString("F2") + " rootScale=" + ins.transform.localScale + " rot=" + ins.transform.rotation.eulerAngles);
UnityEngine.Object.DestroyImmediate(ins);
return sb.ToString();
