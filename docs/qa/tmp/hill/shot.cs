var main = Camera.main;
var go = new GameObject("HillShotCam"); go.hideFlags = HideFlags.HideAndDontSave;
var cam = go.AddComponent<Camera>(); cam.CopyFrom(main); go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
cam.fieldOfView = 70f;
var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.aspect = 1280f / 720f;
var shots = new (string n, Vector3 p, float yaw, float pitch)[] { ("before_balcony", new Vector3(-117.71f, 9.44f, -11.88f), 180f, 4f), ("before_balcony_tower", new Vector3(-117.71f, 15.94f, -11.88f), 185f, 6f) };
var room = GameObject.Find("D198_room").GetComponent<Collider>();
foreach (var s in shots) {
  cam.transform.position = s.p; cam.transform.rotation = Quaternion.Euler(s.pitch, s.yaw, 0);
  cam.Render(); RenderTexture.active = rt;
  var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
  System.IO.Directory.CreateDirectory("D:/Github/F1Game/docs/qa/evidence/hill");
  System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/hill/" + s.n + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
}
cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
return "ok dirty=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty;
