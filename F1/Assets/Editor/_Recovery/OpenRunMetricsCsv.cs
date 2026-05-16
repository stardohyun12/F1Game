using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 수치 audit — CSV 열기/리셋 메뉴. 도현이 5런 돌린 후 메뉴로 결과 열어봄.
/// </summary>
public static class OpenRunMetricsCsv
{
    [MenuItem("PitCrew/Metrics/Open runs.csv")]
    public static void Open()
    {
        var path = Path.Combine(Application.persistentDataPath, "runs.csv");
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Metrics",
                $"CSV 아직 없음. 한 런 끝나면 자동 생성됨.\n경로: {path}",
                "OK");
            return;
        }
        EditorUtility.RevealInFinder(path);
        Application.OpenURL("file://" + path.Replace("\\", "/"));
    }

    [MenuItem("PitCrew/Metrics/Reset runs.csv")]
    public static void Reset()
    {
        var path = Path.Combine(Application.persistentDataPath, "runs.csv");
        if (File.Exists(path)) File.Delete(path);
        Debug.Log($"[Metrics] runs.csv 삭제됨. 다음 런부터 새로 기록.");
    }

    [MenuItem("PitCrew/Metrics/Show CSV path")]
    public static void ShowPath()
    {
        var path = Path.Combine(Application.persistentDataPath, "runs.csv");
        Debug.Log($"[Metrics] {path}");
        EditorGUIUtility.systemCopyBuffer = path;
    }
}
