using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// 게임뷰 영상 녹화 (game-craft-upgrade ④ 영상 버전, 2026-05-20).
/// "영상 찍고 → 도현이 보고 느낌 판단 → 삭제" 워크플로.
/// 스크린샷이 못 잡는 움직임·느낌(화물 흔들림·카메라 전환·물리)을 영상으로.
///
/// Unity Recorder API 사용 — 게임뷰만 깨끗하게 mp4 (에디터 UI 안 들어감).
/// 출력: docs/screenshots/auto/video/rec_yyyyMMdd_HHmmss.mp4 (gitignore).
///
/// unity-cli menu "PitCrew/Capture/Start Recording" → Play 중 시작
/// unity-cli menu "PitCrew/Capture/Stop Recording"  → 정지 + 경로 로그
///
/// 참고: 나(클로드)는 mp4 직접 못 봄 → 도현 판단용. 내 자체점검은 ③ 스크린샷 / ④ First30s.
/// </summary>
public static class GameViewRecorder
{
    static RecorderController _controller;
    static string _outFile;

    [MenuItem("PitCrew/Capture/Start Recording")]
    public static void StartRecording()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Recorder] Play 모드에서 시작해야 함. 먼저 Play 진입.");
            return;
        }
        if (_controller != null && _controller.IsRecording())
        {
            Debug.Log("[Recorder] 이미 녹화 중.");
            return;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        var dir = Path.Combine(repoRoot, "docs", "screenshots", "auto", "video");
        Directory.CreateDirectory(dir);
        // Recorder는 확장자 없이 OutputFile 받음 (.mp4 자동)
        _outFile = Path.Combine(dir, $"rec_{System.DateTime.Now:yyyyMMdd_HHmmss}");

        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();

        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "GameViewMovie";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };
        movie.OutputFile = _outFile;

        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = 30f;

        _controller = new RecorderController(settings);
        _controller.PrepareRecording();
        _controller.StartRecording();
        Debug.Log($"[Recorder] 녹화 시작 -> docs/screenshots/auto/video/{Path.GetFileName(_outFile)}.mp4");
    }

    [MenuItem("PitCrew/Capture/Stop Recording")]
    public static void StopRecording()
    {
        if (_controller == null || !_controller.IsRecording())
        {
            Debug.Log("[Recorder] 녹화 중이 아님.");
            return;
        }
        _controller.StopRecording();
        Debug.Log($"[Recorder] 녹화 정지. 저장: docs/screenshots/auto/video/{Path.GetFileName(_outFile)}.mp4 (도현이 보고 판단 후 삭제)");
        _controller = null;
    }
}
