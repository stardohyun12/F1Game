var sb = new System.Text.StringBuilder(); var cam = Camera.main;
foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    if (t.name.StartsWith("Dusk_Boat") || t.name.StartsWith("Dusk_Lighthouse"))
    { var r = t.GetComponent<Renderer>(); var v = cam.WorldToViewportPoint(t.position); sb.Append(t.name + " act=" + t.gameObject.activeInHierarchy + " r=" + (r ? r.enabled.ToString() : "-") + " pos=" + t.position.ToString("F1") + " vp=" + v.ToString("F2") + " id=" + t.GetInstanceID() + "\n"); }
sb.Append("t=" + Time.time.ToString("F1"));
return sb.ToString();
