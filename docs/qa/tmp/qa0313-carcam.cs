// QA 0313: 차 외형(A?)·카메라 프리셋(B?)·씬 상태
var sb = new System.Text.StringBuilder();
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
sb.Append("scene=" + sc.path + " dirty=" + sc.isDirty + "\n");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
sb.Append("car=" + (car ? car.name : "null") + "\n");
if (car)
    foreach (Transform t in car.GetComponentsInChildren<Transform>(true))
        if (t.parent == car.transform || t.name.StartsWith("CarVisual") || t.name.Contains("FormulaGameplayVisual") || t.name.StartsWith("Extra_"))
            sb.Append("  " + t.name + " active=" + t.gameObject.activeInHierarchy + "\n");
var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>();
sb.Append("cam preset=" + (fc && fc.Preset ? UnityEditor.AssetDatabase.GetAssetPath(fc.Preset) : "null") + "\n");
sb.Append("B=" + CameraFeelSwitcher.B + " Chosen=" + CameraFeelSwitcher.Chosen + "\n");
return sb.ToString();
