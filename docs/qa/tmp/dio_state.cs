var d = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>();
var sun = RenderSettings.sun;
var cam = d != null ? d.GetComponent<Camera>() : null;
var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return "dio=" + (d != null) + " active=" + RoomDiorama.Active + " w=" + (d ? d.Weight.ToString("F2") : "-") + " dist=" + (d ? d.distance.ToString("F1") : "-")
  + " win=" + (d ? d.WindowCenter.ToString("F2") : "-") + " cam=" + (cam ? cam.transform.position.ToString("F2") + " rot " + cam.transform.eulerAngles.ToString("F0") + " clear " + cam.clearFlags : "-")
  + " sun=" + (sun ? sun.enabled + " int " + sun.intensity.ToString("F2") + " col " + sun.color + " rot " + sun.transform.eulerAngles.ToString("F0") + " sh " + sun.shadows : "null")
  + " state=" + (dir ? dir.State.ToString() : "-") + " shadowDist=" + QualitySettings.shadowDistance;
