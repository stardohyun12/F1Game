var s = new System.Text.StringBuilder();
s.Append("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + "\n");
foreach (var c in UnityEngine.Object.FindObjectsByType<ArcadeCarController>(FindObjectsSortMode.None))
{
  var rb = c.GetComponent<Rigidbody>();
  s.Append($"car {c.name} pos={c.transform.position} scale={c.transform.lossyScale} mass={rb.mass} interp={rb.interpolation} cd={rb.collisionDetectionMode} damp={rb.linearDamping}\n");
  foreach (var col in c.GetComponentsInChildren<Collider>(true))
  {
    s.Append($"  col {col.name} {col.GetType().Name} trig={col.isTrigger} en={col.enabled} bounds c={col.bounds.center} size={col.bounds.size} mat={(col.sharedMaterial ? col.sharedMaterial.name + " f" + col.sharedMaterial.dynamicFriction : "-")}");
    if (col is BoxCollider b) s.Append($" local c={b.center} s={b.size}");
    if (col is CapsuleCollider cc) s.Append($" cap c={cc.center} r={cc.radius} h={cc.height}");
    s.Append("\n");
  }
  foreach (var m in c.GetComponents<MonoBehaviour>()) s.Append("  mb " + m.GetType().Name + "\n");
}
return s.ToString();
