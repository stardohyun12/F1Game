// artlive 2056: 집 마당 컷 Cam_FP_yard(house_home + 밭 한 화면) = Cam_ST_34 복제. 편집 모드에서.
if (Application.isPlaying) return "FAIL: Play 끄고";
var cams = GameObject.Find("STUDIO/STUDIO_Cams").transform; var src = cams.Find("Cam_ST_34"); var o = cams.Find("Cam_FP_yard"); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject);
var c = UnityEngine.Object.Instantiate(src.gameObject, cams); c.name = "Cam_FP_yard"; c.transform.position = new Vector3(-1.5f, 5.2f, 10.5f); c.transform.LookAt(new Vector3(-5.2f, 1.2f, -1.6f)); c.GetComponent<Camera>().fieldOfView = 58f; c.SetActive(false);
var slot = GameObject.Find("STUDIO/STUDIO_Slot"); var hh = slot.transform.Find("house_home");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(slot.scene);
return "slot " + slot.transform.position + " house " + (hh ? hh.position.ToString() + " active " + hh.gameObject.activeSelf : "none") + " fp " + slot.transform.Find("field_patch").gameObject.activeSelf;
