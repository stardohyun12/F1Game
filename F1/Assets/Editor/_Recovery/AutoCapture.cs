using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스크린샷 강제 (game-craft-upgrade ③, 2026-05-20).
/// 화면(UI/카메라/마커) 변경 commit 전, 결과를 눈으로 보게 만드는 강제 장치.
/// ScreenCapture.CaptureScreenshot 사용 — ScreenSpaceOverlay UI까지 잡힘
/// (unity-cli screenshot은 overlay 못 잡음. Play 중에 실행해야 인게임 + UI 둘 다).
///
/// 저장 위치: PitCrew/docs/screenshots/auto/auto_yyyyMMdd_HHmmss.png
/// Stop hook이 화면 commit에서 이 폴더의 최근 png를 commit body 'Screenshot:' 경로로 요구.
///
/// unity-cli menu "PitCrew/Capture/Auto Screenshot" 로 호출.
/// </summary>
public static class AutoCapture
{
    [MenuItem("PitCrew/Capture/Auto Screenshot")]
    public static void Capture()
    {
        // dataPath = .../PitCrew/F1/Assets → repo root = ../..
        var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        var dir = Path.Combine(repoRoot, "docs", "screenshots", "auto");
        Directory.CreateDirectory(dir);
        var rel = $"docs/screenshots/auto/auto_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        var abs = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar));

        // CaptureScreenshot은 다음 프레임에 비동기 저장 — Play 중이면 인게임 + overlay 둘 다.
        ScreenCapture.CaptureScreenshot(abs);
        Debug.Log($"[AutoCapture] queued -> {rel} (다음 프레임 저장. Play 중이어야 overlay UI 포함됨)\nABS:{abs}");
    }
}
