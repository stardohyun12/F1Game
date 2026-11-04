var sb = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None))
    sb.AppendLine("cam " + c.name + " en=" + c.enabled + " main=" + (c == UnityEngine.Camera.main) + " rt=" + (c.targetTexture ? c.targetTexture.name + " " + c.targetTexture.width + "x" + c.targetTexture.height : "-") + " lowres=" + (c.GetComponent<LowResRenderTest>() != null) + " depth=" + c.depth + " pos=" + c.transform.position.ToString("F1"));
var cam = UnityEngine.Camera.main;
var hits = UnityEngine.Physics.RaycastAll(cam.transform.position, cam.transform.forward, 10f);
foreach (var h in hits) { var r = h.collider.GetComponentInParent<UnityEngine.Renderer>(); sb.AppendLine("hit " + h.collider.name + " d=" + h.distance.ToString("F2") + " r=" + (r ? r.name + " " + r.sharedMaterial.name + "|" + r.sharedMaterial.shader.name : "-")); }
sb.AppendLine("glob dio=" + UnityEngine.Shader.GetGlobalVector("_A7Dio") + " inp=" + UnityEngine.Shader.GetGlobalVector("_A7InP"));
var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
var q = g.IndoorBox.MultiplyPoint3x4(cam.transform.position);
sb.AppendLine("cam q=" + q.ToString("F2"));
return sb.ToString();
