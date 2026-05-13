using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오디오 cue 이벤트 허브 (Phase 4 — OCP 해소).
///
/// 발행자(시스템들)와 구독자(AudioManager)를 디커플링한다.
/// AudioManager가 더 이상 CargoBox/Delivery/Upgrade 등 시스템을 직접 알지 않는다.
///
/// 새 SFX 추가 흐름:
/// 1. AudioCueId 열거에 새 항목.
/// 2. 발행 시점에 한 줄: AudioCueBus.Publish(AudioCueId.X, value).
/// 3. AudioManager에서 한 줄 Subscribe로 매핑(새 clip 인스펙터 할당).
///
/// 시그니처는 (id, float) 통일. value는 cue별 의미를 가짐(예: CargoImpact = speedKmh).
/// 의미 없는 cue는 0 또는 1을 넘긴다.
/// </summary>
public enum AudioCueId
{
    CargoImpact,        // value = speedKmh (볼륨 스케일링용)
    CargoTotaled,
    DeliveryCompleted,
    UpgradePurchased,
    RunResultSuccess,
    RunResultFail,
    // Packing minigame cues (Task 16 — clips assigned in W5 polish)
    PackingMaterialSelect,  // value = channelIndex (1/2/3 키)
    PackingStrokeStart,     // 마우스 좌클릭 다운 (BeginStroke)
    PackingStrokeEnd,       // 마우스 좌클릭 업 (EndStroke)
    PackingConfirm,         // "출발하기" 버튼
}

public static class AudioCueBus
{
    static readonly Dictionary<AudioCueId, Action<float>> _handlers
        = new Dictionary<AudioCueId, Action<float>>();

    public static void Subscribe(AudioCueId id, Action<float> handler)
    {
        if (handler == null) return;
        _handlers.TryGetValue(id, out var existing);
        _handlers[id] = existing + handler;
    }

    public static void Unsubscribe(AudioCueId id, Action<float> handler)
    {
        if (handler == null) return;
        if (!_handlers.TryGetValue(id, out var existing)) return;
        var next = existing - handler;
        if (next == null) _handlers.Remove(id);
        else _handlers[id] = next;
    }

    public static void Publish(AudioCueId id, float value = 1f)
    {
        if (_handlers.TryGetValue(id, out var h)) h?.Invoke(value);
    }

    /// 씬 전환·도메인 리로드 시 stale 핸들러 정리용.
    public static void Clear() => _handlers.Clear();

    // "Enter Play Mode without Domain Reload" 옵션이 켜져 있으면 static dict가 이전 플레이의
    // 핸들러를 그대로 보존한다. 매 Play 진입 시 자동 리셋해 stale handler 누수를 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnEnterPlayMode() => _handlers.Clear();
}
