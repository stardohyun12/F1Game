// artlive 1623: Cam_ST_in(실내 캠) 추가 — Cam_ST_close 복제, 꺼 둠. 위치는 캡처 때 건물의 STUDIO_CamIn → STUDIO_CamInLook 로 맞춤.
var cams = GameObject.Find("STUDIO/STUDIO_Cams"); if (!cams) return "FAIL: STUDIO_Cams 없음";
var old = cams.transform.Find("Cam_ST_in"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var src = cams.transform.Find("Cam_ST_close"); if (!src) return "FAIL: Cam_ST_close 없음";
var go = UnityEngine.Object.Instantiate(src.gameObject, cams.transform); go.name = "Cam_ST_in"; go.SetActive(false);
var cam = go.GetComponent<Camera>(); cam.nearClipPlane = 0.05f; var al = go.GetComponent<AudioListener>(); if (al) UnityEngine.Object.DestroyImmediate(al); go.tag = "Untagged";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return "ok Cam_ST_in fov " + cam.fieldOfView;
