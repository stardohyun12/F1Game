var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var sb = new System.Text.StringBuilder();
foreach (var root in scene.GetRootGameObjects())
foreach (var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))
{
    if (t.name != "Tape" && t.name != "Labels") continue;
    string path = t.name; var p = t.parent; while (p != null) { path = p.name + "/" + path; p = p.parent; }
    sb.Append(path + " id=" + t.gameObject.GetInstanceID() + " hide=" + t.gameObject.hideFlags + " persistent=" + UnityEditor.EditorUtility.IsPersistent(t.gameObject) + " missingMB=" + UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) + " comps=");
    foreach (var c in t.GetComponents<UnityEngine.Component>()) sb.Append(ReferenceEquals(c, null) ? "REALNULL," : (c == null ? "FAKENULL:" + c.GetType().Name + "," : c.GetType().Name + ","));
    sb.Append(" | ");
}
return sb.ToString();
