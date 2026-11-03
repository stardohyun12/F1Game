var o = "";
var sp = GameObject.Find("OnFoot_Spawn_Room");
o += "spawn " + sp.transform.position.ToString("F2") + " yaw=" + sp.transform.eulerAngles.y + "\n";
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{
    if (t.name != "CALENDAR_01" && t.name != "BED_01" && t.name != "DESK_01") continue;
    if (Vector3.Distance(t.position, sp.transform.position) > 6f) continue;
    var r = t.GetComponentInChildren<Renderer>(true);
    var mf = t.GetComponentInChildren<MeshFilter>(true);
    o += t.name + " path=" + t.parent.name + " pos=" + t.position.ToString("F2") + " rot=" + t.eulerAngles.ToString("F0") + " active=" + t.gameObject.activeInHierarchy
       + " rend=" + (r != null ? r.bounds.center.ToString("F2") + " ext=" + r.bounds.extents.ToString("F2") + " mesh=" + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "-") : "none")
       + " col=" + (t.GetComponentInChildren<Collider>(true) != null) + "\n";
}
var home = GameObject.Find("OnFoot_HomeGarage");
o += "HomeGarage=" + (home != null) + "\n";
var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
o += "director=" + (dir != null ? dir.gameObject.name : "null") + "\n";
o += "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " playing=" + Application.isPlaying + "\n";
return o;
