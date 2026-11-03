var sb = new System.Text.StringBuilder();
sb.Append("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " play=" + Application.isPlaying + "\n");
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{
  var lr = c.GetComponent<LowResRenderTest>();
  sb.Append(c.name + " active=" + c.gameObject.activeInHierarchy + " en=" + c.enabled + " tag=" + c.tag + " depth=" + c.depth + " tt=" + (c.targetTexture ? c.targetTexture.width + "x" + c.targetTexture.height : "-") + (lr ? " LRT en=" + lr.enabled + " h=" + lr.targetHeight + " smooth=" + lr.smooth + " int=" + lr.integerScale + " msaa=" + lr.msaa + " size=" + lr.RenderSize : " noLRT") + "\n");
}
var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; sb.Append("rp=" + (rp ? rp.name : "-") + " scale=" + rp.GetType().GetProperty("renderScale").GetValue(rp) + " screen=" + Screen.width + "x" + Screen.height + "\n");
var root = GameObject.Find("Sunset_D227"); if (root) foreach (Transform t in root.transform) sb.Append(" - " + t.name + " " + t.position.ToString("F1") + " " + t.eulerAngles.ToString("F0") + "\n");
return sb.ToString();
