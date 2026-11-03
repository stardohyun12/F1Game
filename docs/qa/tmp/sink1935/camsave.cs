// 1935: 도현 사진(sink_1031.png) 구도 카메라 Cam_FB_sink — 꺼진 Camera(렌더 안 함), 씬에 저장. 그 카메라로 캡처 → terrain-sink/<arg>.png
string shot = "sink_after";
var go = GameObject.Find("Cam_FB_sink");
if (go == null) { go = new GameObject("Cam_FB_sink"); var c0 = go.AddComponent<Camera>(); c0.enabled = false; c0.fieldOfView = 55; c0.nearClipPlane = .1f; c0.farClipPlane = 2000; }
go.transform.position = new Vector3(88, 44, 50); go.transform.LookAt(new Vector3(120, 10, 18));
var cam = go.GetComponent<Camera>(); var main = Camera.main; if (main != null) { cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; }
var rt = new RenderTexture(1600, 1080, 24); cam.targetTexture = rt; cam.Render();
RenderTexture.active = rt; var tx = new Texture2D(1600, 1080, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1600, 1080), 0, 0); tx.Apply();
RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt);
var p = System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/evidence/terrain-sink/" + shot + ".png");
System.IO.File.WriteAllBytes(p, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
return p;
