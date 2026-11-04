var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
  if (r.name.StartsWith("GLASS_") && (r.bounds.center - new Vector3(187f, 29f, -63f)).magnitude < 4f) sb.Append(r.name + (r.forceRenderingOff ? ":off " : ":ON ") + (r.sharedMaterial ? r.sharedMaterial.name : "-") + " | ");
var p = GameObject.Find("Player");
return sb.ToString();
