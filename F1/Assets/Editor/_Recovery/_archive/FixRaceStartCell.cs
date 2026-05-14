using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 활성 씬의 Race를 OpenWorldLayout.startCell 위치로 이동.
// SetupRaceInScene이 layout 휴리스틱 실패 시 Default(legacy)로 떨어지는 문제 우회.
public static class FixRaceStartCell
{
    const string LayoutPath = "Assets/Data/OpenWorldLayout.asset";
    const float CellSize = 6f;

    [MenuItem("PitCrew/Layout/Fix Race Start (OpenWorld)")]
    public static void Execute()
    {
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(LayoutPath);
        if (layout == null) { Debug.LogError("Layout not found"); return; }
        var race = GameObject.Find("Race");
        if (race == null) { Debug.LogError("Race not found in scene"); return; }
        float fx = layout.startCell.x * CellSize;
        float fz = layout.startCell.y * CellSize;
        var oldPos = race.transform.position;
        race.transform.position = new Vector3(fx, 0.5f, fz);
        race.transform.rotation = Quaternion.Euler(0f, layout.startYawDeg, 0f);

        // 카메라도 동기화
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            cam.transform.position = race.transform.position + new Vector3(0, 4, -6);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"FixRaceStartCell: Race {oldPos:0.0} → ({fx:0.0}, 0.5, {fz:0.0}) cell({layout.startCell.x},{layout.startCell.y})");
    }
}
