var sb = new System.Text.StringBuilder();
var main = UnityEngine.Camera.main;
var go = new UnityEngine.GameObject("D199_ShotCam"); go.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var cam = go.AddComponent<UnityEngine.Camera>(); cam.CopyFrom(main);
var srcData = main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var dst = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
if (srcData) { dst.renderPostProcessing = srcData.renderPostProcessing; dst.antialiasing = srcData.antialiasing; }
var rt = new UnityEngine.RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.aspect = 1280f / 720f;
var root = UnityEngine.GameObject.Find("D198_Buildings").transform;
foreach (var id in new[] { "garage", "depot", "postoffice", "room" }) {
  var b = root.Find("D198_" + id);
  var rs = b.GetComponentsInChildren<UnityEngine.Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
  var fwd = b.forward; float dist = UnityEngine.Mathf.Max(bb.size.x, bb.size.z) * 1.3f + 6f;
  cam.transform.position = bb.center + fwd * dist + UnityEngine.Vector3.up * (bb.size.y * 0.6f + 3f);
  cam.transform.LookAt(bb.center);
  cam.Render();
  UnityEngine.RenderTexture.active = rt;
  var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false); tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
  UnityEngine.RenderTexture.active = null;
  var p = "D:/Github/F1Game/docs/qa/evidence/d199/" + id + ".png"; System.IO.File.WriteAllBytes(p, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
  sb.Append(id + " center=" + bb.center.ToString("F1") + " size=" + bb.size.ToString("F1") + " cam=" + cam.transform.position.ToString("F1") + "\n");
}
cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
sb.Append("dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
return sb.ToString();
