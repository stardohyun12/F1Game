using System.IO;
using UnityEditor;
using UnityEngine;

public static class CaptureFullScreen
{
    [MenuItem("PitCrew/Debug/Capture Full Screen")]
    public static void Capture()
    {
        var dir = Path.Combine(Application.dataPath, "..", "Captures");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"fullscreen_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[CaptureFullScreen] queued -> {path} (다음 프레임에 저장됨, Play 중이어야 overlay 포함)");
    }
}
