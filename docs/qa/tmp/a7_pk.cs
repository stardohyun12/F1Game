// 과제 2050: A7 다시 열고 사진 키트 두 무리를 에디트 모드에서 바로 찍기(LowRes 없이 960×540, 확인용)
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_A7_Light.unity");
var root = GameObject.Find("SceneA4").transform; var pk = root.Find("A7_PhotoKit"); var log = new System.Text.StringBuilder();
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/scene-a7/"));
var go = new GameObject("tmpcam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 50f; var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt;
int[] idx = { 0, 3 };
foreach (var i in idx) { var t = pk.GetChild(i); var f = t.forward; cam.transform.position = t.position + f * 4.5f + Vector3.up * 1.8f + t.right * 1.0f; cam.transform.LookAt(t.position + Vector3.up * 0.8f); cam.Render();
  RenderTexture.active = rt; var tx = new Texture2D(960, 540, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tx.Apply(); RenderTexture.active = null;
  System.IO.File.WriteAllBytes(outDir + "photokit_" + i + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx); log.Append(t.name + " " + t.position.ToString("F1") + "; "); }
UnityEngine.Object.DestroyImmediate(go); rt.Release();
return log + " scene " + sc.path + " dirty " + sc.isDirty;
