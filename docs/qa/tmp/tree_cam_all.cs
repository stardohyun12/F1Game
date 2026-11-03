// artlive 20261031-0142 s02: T 텍스처 다시 읽기 + Cam_TR_all 을 집 서쪽 앞으로(s01 은 집이 나무를 가림)
if (Application.isPlaying) return "FAIL: Play 끄고";
if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio 만";
for (int n = 1; n <= 8; n++) AssetDatabase.ImportAsset("Assets/Art/Review/trees/T" + n + ".png", ImportAssetOptions.ForceUpdate);
var c = GameObject.Find("STUDIO/STUDIO_Cams").transform.Find("Cam_TR_all");
c.position = new Vector3(-9.5f, 4.2f, 13.5f); c.LookAt(new Vector3(-8.2f, 2.6f, -4.5f)); c.GetComponent<Camera>().fieldOfView = 62f;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(c.gameObject.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(c.gameObject.scene);
return "OK";
