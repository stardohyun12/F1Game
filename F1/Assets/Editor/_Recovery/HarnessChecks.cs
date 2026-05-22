using UnityEditor;
using UnityEngine;

/// <summary>
/// 회귀 단언 라이브러리 — "이 상태면 이래야 한다"를 코드로 박아 자동 검증.
/// 정지 스크린샷이 못 잡는 비시각/크로스상태 버그(오디오 잔류·fog·피벗)를 잡는다.
/// 스크립트(scripts/regression-check.ps1)가 Play 중 비트별로 호출. 각 메서드는 "PASS/FAIL: detail" 반환.
///
/// 각 단언은 "우리가 실제로 겪은 버그"에 1:1 대응 (효과 증명됨):
///  - CheckRunInactiveSilent  → 2026-05-22 엔진음 런 후 잔류 (90edf70)
///  - CheckFog                → 2026-05-22 오버뷰 맵 흐릿 (90edf70)
///  - CheckCargoPivots        → 2026-05-21 화물 작업대 잘림/회전 궤도 (93f7f75)
/// </summary>
public static class HarnessChecks
{
    /// <summary>런 비활성(보드/정산/차고)인데 소리내는 AudioSource가 있으면 FAIL — 엔진음 잔류 검출.</summary>
    public static string CheckRunInactiveSilent()
    {
        bool runActive = Services.Run != null && Services.Run.RunActive;
        if (runActive) return "SKIP: 런 활성 상태 (이 단언은 비활성에서만)";
        foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            // 루프 + 재생 + 음소거 안 됨 = 잔류 소리(엔진/BGM 제외 — Music은 의도)
            if (s.isPlaying && !s.mute && s.loop && s.gameObject.name != "Music")
            {
                string clip = s.clip != null ? s.clip.name : "-";
                return $"FAIL: 런 비활성인데 '{s.gameObject.name}' 소리남 (clip={clip})";
            }
        }
        return "PASS: 런 비활성, 잔류 소리 없음";
    }

    /// <summary>fog가 기대값과 다르면 FAIL — 오버뷰 맵 흐릿 검출.</summary>
    public static string CheckFog(bool expected)
    {
        if (RenderSettings.fog != expected)
            return $"FAIL: fog={RenderSettings.fog} 기대={expected}";
        return $"PASS: fog={RenderSettings.fog}";
    }

    /// <summary>엔진 mute가 기대값과 다르면 FAIL.</summary>
    public static string CheckEngineMute(bool expectedMuted)
    {
        var e = Object.FindFirstObjectByType<EngineAudio>();
        if (e == null) return "SKIP: EngineAudio 없음";
        var s = e.GetComponent<AudioSource>();
        if (s == null) return "SKIP: AudioSource 없음";
        if (s.mute != expectedMuted)
            return $"FAIL: engineMute={s.mute} 기대={expectedMuted}";
        return $"PASS: engineMute={s.mute}";
    }

    /// <summary>모든 화물 프리팹의 피벗이 기하 중심에서 벗어나면 FAIL — 작업대 잘림/회전 궤도 검출.</summary>
    public static string CheckCargoPivots()
    {
        var cat = Resources.Load<CargoModelCatalog>("CargoModels/CargoModelCatalog");
        if (cat == null) return "SKIP: 카탈로그 없음";
        string fails = "";
        foreach (CargoType ct in System.Enum.GetValues(typeof(CargoType)))
        {
            if (!cat.TryGet(ct, out var prefab, out _)) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = Vector3.zero;
            var r = inst.GetComponentInChildren<MeshRenderer>();
            float off = r != null ? (r.bounds.center - inst.transform.position).magnitude : 99f;
            Object.DestroyImmediate(inst);
            if (off > 0.1f) fails += $"{ct}(off={off:F2}) ";
        }
        return fails == "" ? "PASS: 모든 화물 피벗 중심" : $"FAIL: 피벗 어긋남 {fails}";
    }
}
