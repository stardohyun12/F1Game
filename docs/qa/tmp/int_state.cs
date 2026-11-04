var cam = Camera.main;
var dio = cam != null ? cam.GetComponent<RoomDiorama>() : null;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
string extra = "";
var pi = dio != null ? dio.GetType().GetProperty("RoomNow") : null;
if (pi != null) extra = " room=" + pi.GetValue(dio);
return "state=" + (d != null ? d.State.ToString() : "-") + " active=" + RoomDiorama.Active + " w=" + (dio != null ? dio.Weight.ToString("F2") : "-") + extra
  + " cam=" + (cam != null ? cam.transform.position.ToString("F2") + " rot=" + cam.transform.eulerAngles.ToString("F1") : "-")
  + " player=" + (cam != null && cam.GetComponent<OnFootCamera>() != null && cam.GetComponent<OnFootCamera>().target != null ? cam.GetComponent<OnFootCamera>().target.position.ToString("F2") : "-");
