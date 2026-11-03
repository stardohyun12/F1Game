var sb = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  sb.Append("CAM " + c.name + " act=" + c.gameObject.activeInHierarchy + " fov=" + c.fieldOfView + " lr=" + (c.GetComponent<LowResRenderTest>()!=null) + " pos=" + c.transform.position + " parent=" + (c.transform.parent? c.transform.parent.name:"-") + " comps=");
  foreach (var m in c.GetComponents<MonoBehaviour>()) sb.Append(m.GetType().Name + ",");
  sb.Append("\n"); }
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(FindObjectsInactive.Include); sb.Append("A7 " + (lt? lt.name + " painterly=" + lt.painterly + " act=" + lt.isActiveAndEnabled : "none") + "\n");
foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) sb.Append("ROOT " + r.name + " " + r.activeSelf + "\n");
var bot = UnityEngine.Object.FindFirstObjectByType<TownPathBot>(FindObjectsInactive.Include); sb.Append("BOT " + (bot? bot.name:"none") + "\n");
return sb.ToString();
