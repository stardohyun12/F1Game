var seat = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
var sb = new System.Text.StringBuilder();
sb.Append("scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " seat " + seat.position.ToString("F1") + " rot " + seat.eulerAngles.ToString("F1") + "\n");
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); sb.Append("baseFov " + (fc != null ? fc.BaseFov.ToString() : "-") + " mainFov " + Camera.main.fieldOfView + " far " + Camera.main.farClipPlane + "\n");
var lr = Camera.main.GetComponent<LowResRenderTest>(); sb.Append("lowres " + (lr != null ? lr.RenderSize.ToString() : "none") + "\n");
var look = SunsetLook.Find(); sb.Append("look " + look.name + " az " + look.sunAzimuth + " sea " + (look.sea ? look.sea.name : "") + " near " + (look.seaNear ? look.seaNear.transform.position.ToString("F0") : "") + "\n");
var shaders = new System.Collections.Generic.Dictionary<string, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
{
    var d = r.bounds.center - seat.position; if (d.magnitude > 120f) continue; if (Vector3.Dot(d.normalized, seat.forward) < 0.2f) continue;
    var k = (r.sharedMaterial ? r.sharedMaterial.shader.name + ":" + r.sharedMaterial.name : "null") + " | " + r.transform.root.name + " | " + r.name;
    shaders[k] = shaders.ContainsKey(k) ? shaders[k] + 1 : 1;
}
foreach (var kv in shaders) sb.Append(kv.Value + "x " + kv.Key + "\n");
return sb.ToString();
