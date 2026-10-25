var sb = new System.Text.StringBuilder();
var race = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include);
System.Action<Transform> B = t => {
  var mf = t.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) return;
  var b = mf.sharedMesh.bounds; var mn = race.transform.InverseTransformPoint(t.TransformPoint(b.min)); var mx = race.transform.InverseTransformPoint(t.TransformPoint(b.max));
  sb.Append(t.name + " min=" + mn.ToString("F2") + " max=" + mx.ToString("F2") + "\n"); };
foreach (var n in new[] { "FormulaGameplayVisual/ChassisLean/driver", "FormulaGameplayVisual/ChassisLean/CargoMount/delivery_bag", "FormulaGameplayVisual/ChassisLean/body" }) B(race.transform.Find(n));
var kb = race.transform.Find("body"); var kr = kb.GetComponent<Renderer>(); sb.Append("kenney body renderer enabled=" + (kr != null && kr.enabled) + "\n");
foreach (var w in new[] { "A", "C" })
{
  var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/BayLoop/Car/" + w + ".fbx");
  foreach (var mf in fbx.GetComponentsInChildren<MeshFilter>()) { if (mf.name.StartsWith("wheel")) continue; var b = mf.sharedMesh.bounds; sb.Append(w + "/" + mf.name + " (fbx-local, x1.0 vis) min=" + b.min.ToString("F2") + " max=" + b.max.ToString("F2") + " lp=" + mf.transform.localPosition.ToString("F2") + "\n"); }
}
return sb.ToString();
