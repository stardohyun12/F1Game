using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "처음 30초" 자동 테스트 (game-craft-upgrade ④, 2026-05-20).
/// Play 중 일정 간격으로 자동 스크린샷 → 새 플레이어의 첫 30초를 사진으로 보게.
/// 스팀 데모 첫인상 검증의 핵심.
///
/// 동작:
///  - 핵심: 시작 시점부터 5초 간격으로 ScreenCapture (overlay UI 포함) → docs/screenshots/auto/30s/
///  - 보너스: 정해진 시점에 버튼 onClick 자동 호출 (메인메뉴 '시작' 등) — 흐름 진행 시도.
///    버튼 못 찾으면 조용히 skip (사진은 계속 찍힘).
///
/// unity-cli menu "PitCrew/Sim/First 30 Seconds" 로 호출 (Play 중이어야 함).
/// EditorApplication.update 기반 — Play 모드에서 editor 코루틴처럼 시간 누적.
/// </summary>
[InitializeOnLoad]
public static class First30Seconds
{
    static double _startTime;
    static int _nextShotIdx;
    static bool _running;
    static string _dir;

    // 캡처 시점 (초). 0~30초, 5초 간격.
    static readonly float[] ShotTimes = { 0f, 5f, 10f, 15f, 20f, 25f, 30f };

    [MenuItem("PitCrew/Sim/First 30 Seconds")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[First30s] Play 모드에서 실행해야 함. 먼저 Play 진입 후 호출.");
            return;
        }
        if (_running)
        {
            Debug.Log("[First30s] 이미 실행 중.");
            return;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        _dir = Path.Combine(repoRoot, "docs", "screenshots", "auto", "30s");
        // 이전 결과 정리
        if (Directory.Exists(_dir))
            foreach (var f in Directory.GetFiles(_dir, "*.png")) File.Delete(f);
        Directory.CreateDirectory(_dir);

        _startTime = EditorApplication.timeSinceStartup;
        _nextShotIdx = 0;
        _running = true;
        EditorApplication.update += Tick;
        Debug.Log($"[First30s] 시작 — 5초 간격 30초 캡처 -> docs/screenshots/auto/30s/");
    }

    static void Tick()
    {
        if (!_running) { EditorApplication.update -= Tick; return; }
        if (!EditorApplication.isPlaying)
        {
            // Play 중단되면 종료
            Stop();
            return;
        }

        float elapsed = (float)(EditorApplication.timeSinceStartup - _startTime);

        // 보너스: 입력 자동화 (버튼 onClick 호출) — 흐름 진행 시도
        TryAutoInput(elapsed);

        // 핵심: 예정 시점 도달하면 캡처
        if (_nextShotIdx < ShotTimes.Length && elapsed >= ShotTimes[_nextShotIdx])
        {
            int sec = Mathf.RoundToInt(ShotTimes[_nextShotIdx]);
            var path = Path.Combine(_dir, $"shot_{sec:00}s.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[First30s] t={sec}s 캡처 -> 30s/shot_{sec:00}s.png");
            _nextShotIdx++;
        }

        if (_nextShotIdx >= ShotTimes.Length && elapsed >= ShotTimes[ShotTimes.Length - 1] + 1f)
        {
            Debug.Log("[First30s] 완료 — 30s/ 폴더 7장 확인.");
            Stop();
        }
    }

    static int _inputStage;
    static void TryAutoInput(float elapsed)
    {
        // t=2s: 메인메뉴 '시작' 버튼 클릭
        if (_inputStage == 0 && elapsed >= 2f)
        {
            _inputStage = 1;
            if (ClickButtonByLabel("시작")) Debug.Log("[First30s] t=2s '시작' 클릭");
        }
    }

    /// <summary>자식 텍스트 라벨로 버튼 찾아 onClick 호출. pointer 이벤트 우회.</summary>
    static bool ClickButtonByLabel(string label)
    {
        var buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (var b in buttons)
        {
            if (!b.gameObject.activeInHierarchy || !b.isActiveAndEnabled) continue;
            var txt = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (txt != null && txt.text != null && txt.text.Contains(label))
            {
                b.onClick.Invoke();
                return true;
            }
        }
        return false;
    }

    static void Stop()
    {
        _running = false;
        _inputStage = 0;
        EditorApplication.update -= Tick;
    }
}
