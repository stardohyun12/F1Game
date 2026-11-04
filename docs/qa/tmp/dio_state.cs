UnityEngine.Application.runInBackground = true;
var cam = UnityEngine.Camera.main;
var d = cam ? cam.GetComponent<RoomDiorama>() : null;
var f = cam ? cam.GetComponent<OnFootCamera>() : null;
var p = f && f.target ? f.target.position : UnityEngine.Vector3.zero;
return "t=" + UnityEngine.Time.time.ToString("F1") + " dio=" + (d != null) + " active=" + RoomDiorama.Active + " w=" + (d ? d.Weight.ToString("F2") : "-")
    + " foot=" + (f ? f.enabled.ToString() : "-") + " yaw=" + (f ? f.Yaw.ToString("F0") : "") + " pitch=" + (f ? f.Pitch.ToString("F0") : "")
    + " player=" + p.ToString("F1") + " cam=" + cam.transform.position.ToString("F2") + " rot=" + cam.transform.eulerAngles.ToString("F0") + " clear=" + cam.clearFlags;
