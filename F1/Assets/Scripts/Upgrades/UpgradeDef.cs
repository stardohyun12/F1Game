using UnityEngine;

/// <summary>
/// 업그레이드 효과 종류 (Day 21).
/// "감각형 업그레이드" 원칙 — 단순 +n% 아닌 운전 감각/전략 변화.
///
/// Day 21 첫 단계 5개 (Phase 1 끝):
/// 1. SuspensionUpgrade — 서스펜션 강화 (Shock 임계 +0.1g)
/// 2. ChilledHold — 냉동 칸 (냉동 화물 주문 해제)
/// 3. ShortcutMap — 지름길 지도 (NodeGraph isShortcut 정보 공개)
/// 4. SmoothSteering — 부드러운 조향 (Spin 임계 +20 deg/s)
/// 5. ReinforcedTrunk — 튼튼한 적재함 (화물 maxHP +20)
///
/// 효과 적용은 UpgradeSystem.IsOwned(type) 체크 후 시스템에서 분기.
/// </summary>
public enum UpgradeEffect
{
    /// <summary>화물 Shock 임계값 +0.1g 여유 (longWarn/latWarn).</summary>
    SuspensionUpgrade,
    /// <summary>냉동 화물(시간 제약 강한 주문) 카탈로그 해제.</summary>
    ChilledHold,
    /// <summary>NodeGraph isShortcut 엣지 표시 (지름길 가시화).</summary>
    ShortcutMap,
    /// <summary>화물 Spin 임계값 +20 deg/s 여유.</summary>
    SmoothSteering,
    /// <summary>화물 maxHP +20.</summary>
    ReinforcedTrunk,
}

/// <summary>
/// 업그레이드 정의 SO. 도현이 인스펙터에서 5개 작성.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Upgrade", fileName = "Upgrade_")]
public class UpgradeDef : ScriptableObject
{
    [Header("Identity")]
    public UpgradeEffect effect;
    public string displayName = "업그레이드";
    [TextArea(2, 4)]
    public string description = "효과 설명";

    [Header("Cost")]
    [Tooltip("정산 후 자금에서 소모. 첫 단계 5개는 ₩500~1500 권장.")]
    public int cost = 1000;

    [Header("UI")]
    public Color tintColor = Color.white;
}
