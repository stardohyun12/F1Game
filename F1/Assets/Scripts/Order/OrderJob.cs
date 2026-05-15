using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 런의 작업 정의. ScriptableObject.
/// GDD: GDD/core-loop.md (결정 1: 주문 선택), D-030 K4 연계.
///
/// 채널 8 (2026-05-15): 잡이 진짜 다른 목적지를 가짐.
/// `destinationLabels` (씬 DeliveryPoint.Label과 일치)이 source of truth.
/// `itemCount`는 `destinationLabels.Count`에서 파생 — 물건 1개 = 목적지 1곳.
/// 옛 `pointCount` 필드는 제거 (D-070 잔재). spec: 2026-05-15-order-route-flow-design.md.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Order Job", fileName = "Order_New")]
public class OrderJob : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "주문";
    [Tooltip("UI 카드/맵 마커 색상.")]
    public Color color = Color.green;

    [Header("Items / Destinations (채널 8 — 물건 1개 = 목적지 1곳)")]
    [Tooltip("배달 목적지. 씬 DeliveryPoint.Label과 일치. 개수가 곧 물건 수.")]
    public List<string> destinationLabels = new List<string>();

    /// <summary>물건 수 = 목적지 수 (1~3 클램프). destinationLabels에서 파생.</summary>
    public int itemCount => destinationLabels != null
        ? Mathf.Clamp(destinationLabels.Count, 1, 3)
        : 1;

    [Header("Difficulty")]
    [Tooltip("시간제한(초). 0이면 무제한.")]
    public float timeLimitSec = 0f;

    [Header("Reward")]
    public int baseReward = 500;
    [Tooltip("시간 내 완료 시 추가 보상. timeLimit=0이면 무시.")]
    public int timeBonus = 0;

    [Header("Display Only (MVP)")]
    [Tooltip("0=가벼움, 1=보통, 2=무거움. MVP는 표시만.")]
    [Range(0, 2)] public int weightTier = 0;

    public string WeightLabel => weightTier switch
    {
        0 => "가벼움",
        1 => "보통",
        _ => "무거움"
    };

    [Header("Cargo (Day 11)")]
    [Tooltip("이 주문에 실리는 화물 타입. RunController.BeginRun에서 CargoBox에 자동 적용.")]
    public CargoType cargoType = CargoType.Box;
}
