// 2판 씬 적용: Cam_Sunset 방향 180°, FishingSpot.yawLeft 8, 물 밑 깊이 굽기, 보이는 나무 교체. 노을 끈 상태로 저장.
var look = SunsetLook.Find(); if (look.IsOn) look.SetOn(false);
var cam = GameObject.Find("Sunset_D227/Cam_Sunset").transform;
UnityEditor.Undo.RecordObject(cam, "seat"); cam.rotation = Quaternion.Euler(PitCrewSunsetMenu.SeatEuler);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
var so = new UnityEditor.SerializedObject(spot); so.FindProperty("yawLeft").floatValue = PitCrewSunsetMenu.LookLeft; so.ApplyModifiedProperties();
var sw = System.Diagnostics.Stopwatch.StartNew();
PitCrewSunsetMenu.BakeSeaDepth(); long bakeMs = sw.ElapsedMilliseconds;
PitCrewSunsetMenu.SwapTrees();
var trees = look.transform.Find("Trees_F");
string tinfo = "";
if (trees != null && trees.childCount > 0)
{
    var b = trees.GetChild(0).GetComponentInChildren<Renderer>().bounds; tinfo = " F-tree h=" + b.size.y.ToString("F1");
}
if (look.hiddenTrees != null && look.hiddenTrees.Length > 0) { var rr = look.hiddenTrees[0].GetComponentInChildren<Renderer>(true); look.hiddenTrees[0].SetActive(true); tinfo += " old h=" + rr.bounds.size.y.ToString("F1"); look.hiddenTrees[0].SetActive(false); }
var scene = look.gameObject.scene;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "bake " + bakeMs + "ms trees=" + (look.hiddenTrees != null ? look.hiddenTrees.Length : 0) + tinfo + " rot=" + cam.eulerAngles + " yawLeft=" + spot.yawLeft + " saved=" + saved + " on=" + look.IsOn;
