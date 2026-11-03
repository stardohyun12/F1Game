// artlive 20261031-0142 나무 후보: 스튜디오 마당 자리 재기(읽기만)
var sb = new System.Text.StringBuilder();
sb.Append("scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " play " + Application.isPlaying + "\n");
var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;
foreach (Transform ch in slotT) {
  var b = new Bounds(ch.position, Vector3.zero); foreach (var r in ch.GetComponentsInChildren<Renderer>(true)) b.Encapsulate(r.bounds);
  sb.Append(ch.name + " active " + ch.gameObject.activeSelf + " pos " + ch.position.ToString("F2") + " bounds " + b.min.ToString("F1") + ".." + b.max.ToString("F1") + "\n"); }
foreach (Transform ch in GameObject.Find("STUDIO").transform) {
  var b = new Bounds(ch.position, Vector3.zero); foreach (var r in ch.GetComponentsInChildren<Renderer>(true)) b.Encapsulate(r.bounds);
  sb.Append("STUDIO/" + ch.name + " " + b.min.ToString("F1") + ".." + b.max.ToString("F1") + "\n"); }
var cams = GameObject.Find("STUDIO/STUDIO_Cams").transform;
foreach (Transform c in cams) if (c.name.StartsWith("Cam_FP") || c.name == "Cam_ST_34") sb.Append(c.name + " " + c.position.ToString("F1") + " fwd " + c.forward.ToString("F2") + " fov " + c.GetComponent<Camera>().fieldOfView + "\n");
var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None); foreach (var l in sun) sb.Append("light " + l.name + " " + l.type + " rot " + l.transform.eulerAngles.ToString("F0") + "\n");
var tm = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat"); sb.Append("tmpl " + (tm ? tm.shader.name : "MISSING"));
return sb.ToString();
