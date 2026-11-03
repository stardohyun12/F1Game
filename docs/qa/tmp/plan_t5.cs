UnityEngine.Application.runInBackground = true;
var main = UnityEngine.Camera.main;
var go = new UnityEngine.GameObject("_shotcam"); var cam = go.AddComponent<UnityEngine.Camera>(); cam.CopyFrom(main); cam.enabled = false;
var dir = "D:/Github/F1Game/docs/qa/evidence/plan-window/";
var sb = new System.Text.StringBuilder("frame=" + UnityEngine.Time.frameCount + " main=" + main.transform.position.ToString("F1") + "\n");
// 정비소(garage 루트 -63.2,8.32,-6.7 yaw -174.8) 앞 길에서, 보관소(depot -43.77,8.68,-28.2) 앞 길에서 — 운전석 높이 2.2m
var shots = new[] {
  new { n = "game-garage.png", from = new UnityEngine.Vector3(-61f, 10.5f, -24f), at = new UnityEngine.Vector3(-63.5f, 10f, -8f) },
  new { n = "game-depot.png", from = new UnityEngine.Vector3(-46f, 10.8f, -12f), at = new UnityEngine.Vector3(-44f, 10f, -29f) } };
foreach (var s in shots) {
  go.transform.position = s.from; go.transform.rotation = UnityEngine.Quaternion.LookRotation(s.at - s.from);
  sb.Append(s.n + " " + PitCrewPlan.PlanIO.RenderPng(cam, dir + s.n) + "\n");
}
UnityEngine.Object.Destroy(go);
return sb.ToString();
