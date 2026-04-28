using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 업그레이드 카탈로그 (Day 24).
/// ResultPanel/UpgradeSelectionPanel에서 표시할 5개 후보 SO.
///
/// MVP는 정산마다 5개 모두 노출. Phase 2에 무작위 3개 픽 (로그라이트 패턴).
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Upgrade Catalog", fileName = "UpgradeCatalog")]
public class UpgradeCatalog : ScriptableObject
{
    [Tooltip("정산 화면에 표시할 업그레이드 후보. Phase 1 첫 단계 = 5개.")]
    public List<UpgradeDef> upgrades = new List<UpgradeDef>();
}
